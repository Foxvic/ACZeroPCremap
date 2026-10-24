[CmdletBinding()]
param(
    [string]$DotnetPath,
    [string]$OutputRoot,
    [switch]$SkipChecks,
    [switch]$NoRestore
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$projectRoot = Split-Path $PSScriptRoot -Parent
if (-not $OutputRoot) { $OutputRoot = Join-Path $projectRoot 'release' }
$OutputRoot = [IO.Path]::GetFullPath($OutputRoot)
if (-not $DotnetPath) {
    $localSdk = Join-Path $projectRoot '.tools/dotnet/dotnet.exe'
    if (Test-Path -LiteralPath $localSdk) { $DotnetPath = $localSdk }
    else { $DotnetPath = (Get-Command dotnet -ErrorAction Stop).Source }
}
$appProject = Join-Path $projectRoot 'src/ACZeroPCremap.App/ACZeroPCremap.App.csproj'
[xml]$projectXml = Get-Content -LiteralPath $appProject -Raw
$version = [string]$projectXml.Project.PropertyGroup.Version
$packageName = "ACZeroPCremap-$version-win-x64"
$packageDir = Join-Path $OutputRoot $packageName
$binaryZip = Join-Path $OutputRoot ($packageName + '.zip')
$sourceZip = Join-Path $OutputRoot ("ACZeroPCremap-$version-source.zip")
foreach ($target in @($packageDir, $binaryZip, $sourceZip)) {
    if (Test-Path -LiteralPath $target) { throw "Output already exists: $target. Choose another -OutputRoot or update the version; existing files are never deleted." }
}

function Invoke-Dotnet([string[]]$Arguments) {
    & $DotnetPath @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed ($LASTEXITCODE): $Arguments" }
}

Push-Location $projectRoot
try {
    $restoreOptions = @()
    if ($NoRestore) { $restoreOptions = @('--no-restore') }
    Invoke-Dotnet (@('build', 'ACZeroPCremap.sln', '-c', 'Release', '-m:1') + $restoreOptions)
    if (-not $SkipChecks) {
        Invoke-Dotnet @('tests/ACZeroPCremap.Tests/bin/Release/net10.0/ACZeroPCremap.Tests.dll')
        Invoke-Dotnet @('tests/ACZeroPCremap.UiChecks/bin/Release/net10.0-windows/ACZeroPCremap.UiChecks.dll', $projectRoot)
    }
    $staging = Join-Path $projectRoot ('artifacts/publish/' + [Guid]::NewGuid().ToString('N'))
    Invoke-Dotnet (@('publish', $appProject, '-c', 'Release', '-p:PublishProfile=Portable-win-x64', '-o', $staging, '-m:1') + $restoreOptions)
    $stagedExe = Join-Path $staging 'ACZeroPCremap.exe'
    if (-not (Test-Path -LiteralPath $stagedExe)) { throw 'The portable EXE was not published.' }
    # Unexpected sidecar binaries mean single-file packaging did not succeed.
    $sidecars = @(Get-ChildItem -LiteralPath $staging -Recurse -File | Where-Object { $_.Extension -in @('.dll', '.json', '.pdb') })
    if ($sidecars.Count -gt 0) { throw 'Unexpected sidecar DLL/JSON/PDB files in the single-file publish output.' }

    New-Item -ItemType Directory -Path $packageDir -Force | Out-Null
    Copy-Item -LiteralPath $stagedExe -Destination (Join-Path $packageDir 'ACZeroPCremap.exe')
    Copy-Item -LiteralPath 'release-assets/README.txt' -Destination (Join-Path $packageDir 'README.txt')
    Copy-Item -LiteralPath 'LICENSE' -Destination (Join-Path $packageDir 'LICENSE.txt')
    Copy-Item -LiteralPath 'THIRD-PARTY-NOTICES.txt' -Destination $packageDir

    # Copy notices from the actual restored runtime packs, including .NET's
    # third-party attribution. Do not hardcode a particular servicing version.
    $assets = Get-Content 'src/ACZeroPCremap.App/obj/project.assets.json' -Raw | ConvertFrom-Json
    $runtimeConfig = Get-Content 'src/ACZeroPCremap.App/bin/Release/net10.0-windows/win-x64/ACZeroPCremap.runtimeconfig.json' -Raw | ConvertFrom-Json
    $licenseDir = Join-Path $packageDir 'licenses'
    New-Item -ItemType Directory -Path $licenseDir -Force | Out-Null
    foreach ($pack in @('Microsoft.NETCore.App.Runtime.win-x64', 'Microsoft.WindowsDesktop.App.Runtime.win-x64')) {
        $frameworkName = $pack.Replace('.Runtime.win-x64', '')
        $dependency = @($runtimeConfig.runtimeOptions.includedFrameworks | Where-Object { $_.name -eq $frameworkName }) | Select-Object -First 1
        if (-not $dependency) { throw "Runtime dependency metadata not found: $pack" }
        $packVersion = $dependency.version
        $packDir = $null
        foreach ($folder in $assets.packageFolders.PSObject.Properties.Name) {
            $candidate = Join-Path $folder ($pack.ToLowerInvariant() + '/' + $packVersion)
            if (Test-Path -LiteralPath $candidate) { $packDir = $candidate; break }
        }
        if (-not $packDir) { throw "Runtime pack not found: $pack $packVersion" }
        $notices = @(Get-ChildItem -LiteralPath $packDir -File | Where-Object { $_.Name -match '^(LICENSE(\.TXT)?|THIRD-PARTY-NOTICES\.TXT)$' })
        if ($notices.Count -eq 0) { throw "No runtime license found: $pack" }
        foreach ($notice in $notices) {
            Copy-Item -LiteralPath $notice.FullName -Destination (Join-Path $licenseDir ($pack + '-' + [IO.Path]::GetFileNameWithoutExtension($notice.Name) + '.txt'))
        }
    }

    $releaseInfo = @("ACZeroPCremap $version", 'Platform: win-x64', 'Deployment: self-contained, compressed single-file',
        "Built: $([DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ'))", "SDK: $(& $DotnetPath --version)")
    if ($SkipChecks) { $releaseInfo += 'Checks: skipped by explicit build option' } else { $releaseInfo += 'Checks: core and WPF passed' }
    $releaseInfo | Set-Content -LiteralPath (Join-Path $packageDir 'BUILD-INFO.txt') -Encoding UTF8
    $exeHash = (Get-FileHash -LiteralPath (Join-Path $packageDir 'ACZeroPCremap.exe') -Algorithm SHA256).Hash.ToLowerInvariant()
    "$exeHash  ACZeroPCremap.exe" | Set-Content -LiteralPath (Join-Path $packageDir 'SHA256SUMS.txt') -Encoding ASCII

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::CreateFromDirectory($packageDir, $binaryZip, [IO.Compression.CompressionLevel]::Optimal, $false)
    # An explicit source allowlist keeps SDKs, cached builds, personal settings,
    # reference game screenshots and generated packages out of the source ZIP.
    $sourceFiles = @('ACZeroPCremap.sln', 'Directory.Build.props', 'global.json', '.gitignore', '.gitattributes',
        'README.md', 'LICENSE', 'CONTRIBUTING.md', 'CHANGELOG.md', 'SPEC-v1.md', 'VALIDATION.md',
        'THIRD-PARTY-NOTICES.txt', 'dinput.ini', 'dinputSample.ini') | ForEach-Object { Get-Item -LiteralPath $_ }
    foreach ($sourceDir in @('src', 'tests', 'docs', 'scripts', '.github', 'release-assets')) {
        $sourceFiles += Get-ChildItem -LiteralPath $sourceDir -Recurse -File | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
    }
    $sourceArchive = [IO.Compression.ZipFile]::Open($sourceZip, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in $sourceFiles) {
            $relative = $file.FullName.Substring($projectRoot.Length + 1).Replace('\', '/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($sourceArchive, $file.FullName, $relative, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally { $sourceArchive.Dispose() }
    @($binaryZip, $sourceZip) | ForEach-Object {
        $hash = Get-FileHash -LiteralPath $_ -Algorithm SHA256
        "$($hash.Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($_))"
    } | Set-Content -LiteralPath (Join-Path $OutputRoot 'SHA256SUMS.txt') -Encoding ASCII
    Get-Item -LiteralPath $binaryZip, $sourceZip | Select-Object Name, @{Name='MiB';Expression={[math]::Round($_.Length / 1MB, 2)}}
    Write-Host "Ready: $packageDir"
}
finally { Pop-Location }
