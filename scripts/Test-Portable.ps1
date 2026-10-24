[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$ExePath)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$exe = Get-Item -LiteralPath $ExePath
$smokeDir = Join-Path $projectRoot ('artifacts/portable smoke Ж/' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $smokeDir -Force | Out-Null
$smokeExe = Join-Path $smokeDir 'ACZeroPCremap.exe'
Copy-Item -LiteralPath $exe.FullName -Destination $smokeExe
$emptyRuntime = Join-Path $smokeDir 'empty-runtime'
New-Item -ItemType Directory -Path $emptyRuntime | Out-Null
$tracePath = Join-Path $smokeDir 'host-trace.txt'
$environmentChanges = @{
    'DOTNET_ROOT' = $emptyRuntime
    'DOTNET_ROOT_X64' = $emptyRuntime
    'DOTNET_MULTILEVEL_LOOKUP' = '0'
    'DOTNET_HOST_TRACE' = '1'
    'DOTNET_HOST_TRACEFILE' = $tracePath
    'PATH' = "$env:SystemRoot\System32;$env:SystemRoot"
}
$previous = @{}
$process = $null
try {
    foreach ($name in $environmentChanges.Keys) {
        $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        [Environment]::SetEnvironmentVariable($name, $environmentChanges[$name], 'Process')
    }
    try { $process = Start-Process -FilePath $smokeExe -WorkingDirectory $smokeDir -WindowStyle Hidden -PassThru }
    finally {
        foreach ($name in $previous.Keys) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
    }
    Add-Type @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class PortableWindowCheck {
    private delegate bool EnumCallback(IntPtr window, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumCallback callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    public static IntPtr Find(int processId) {
        IntPtr result = IntPtr.Zero;
        EnumWindows((window, unused) => {
            uint owner; GetWindowThreadProcessId(window, out owner);
            if (owner != processId) return true;
            var text = new StringBuilder(512); GetWindowText(window, text, text.Capacity);
            if (text.ToString() != "ACZeroPCremap") return true;
            result = window; return false;
        }, IntPtr.Zero);
        return result;
    }
    public static void Close(IntPtr window) { PostMessage(window, 0x0010, IntPtr.Zero, IntPtr.Zero); }
}
'@
    $deadline = [DateTime]::UtcNow.AddSeconds(45)
    $window = [IntPtr]::Zero
    while ([DateTime]::UtcNow -lt $deadline -and -not $process.HasExited) {
        $window = [PortableWindowCheck]::Find($process.Id)
        if ($window -ne [IntPtr]::Zero) { break }
        Start-Sleep -Milliseconds 200
        $process.Refresh()
    }
    if ($window -eq [IntPtr]::Zero) { throw 'Published application did not initialize its main window.' }
    $process.Refresh()
    $modules = @((Get-Process -Id $process.Id).Modules)
    if (@($modules | Where-Object { $_.FileName -match '\\shared\\Microsoft\.(NETCore|WindowsDesktop)\.App\\' }).Count -gt 0) {
        throw 'The application used an installed shared runtime.'
    }
    # Newer single-file hosts may link CoreCLR internally, so requiring a
    # separate coreclr.dll module incorrectly rejects a self-contained build.
    $nativeWpf = @($modules | Where-Object { $_.ModuleName -eq 'PresentationNative_cor3.dll' })
    if ($nativeWpf.Count -ne 1 -or $nativeWpf[0].FileName -notlike '*\.net\ACZeroPCremap\*') {
        throw 'The application did not load the bundled native WPF component.'
    }
    $trace = Get-Content -LiteralPath $tracePath -Raw
    if ($trace -notmatch 'Executing as a self-contained app' -or $trace -notmatch 'is_framework_dependent=0') {
        throw 'The host trace did not confirm a self-contained runtime.'
    }
    [PortableWindowCheck]::Close($window)
    if (-not $process.WaitForExit(10000)) { throw 'The published application did not close cleanly.' }
    if ($process.ExitCode -ne 0) { throw "The published application exited with $($process.ExitCode)." }
    $settingsPath = Join-Path $smokeDir 'ACZeroPCremap.settings.json'
    if (-not (Test-Path -LiteralPath $settingsPath)) { throw 'Window placement was not saved beside the portable EXE on close.' }
    $settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
    if ($settings.Window.Width -le 0 -or $settings.Window.Height -le 0) { throw 'Invalid saved window placement.' }
    Write-Host 'PASS: single EXE started from a path with spaces/Unicode, with empty DOTNET_ROOT and no dotnet on PATH.'
    Write-Host "PASS: host trace confirms self-contained startup; bundled WPF loaded from $($nativeWpf[0].FileName)"
    Write-Host 'PASS: main window initialized, closed normally, and saved window placement beside the EXE.'
    Write-Host "Host trace: $tracePath"
    Write-Host 'This is a deployment smoke check on the current host, not a clean-Windows VM test.'
}
finally {
    if ($process -and -not $process.HasExited) { $process.Kill() }
}
