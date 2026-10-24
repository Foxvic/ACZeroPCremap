[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$assetRoot = Join-Path $projectRoot 'src/ACZeroPCremap.App/Assets'
Add-Type -AssemblyName PresentationCore, WindowsBase
$source = [Windows.Media.Imaging.BitmapFrame]::Create([Uri](Join-Path $assetRoot 'winged-key-source.png'), [Windows.Media.Imaging.BitmapCreateOptions]::None, [Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
$source = [Windows.Media.Imaging.FormatConvertedBitmap]::new($source, [Windows.Media.PixelFormats]::Bgra32, $null, 0)
# Crop transparent padding for ICO packaging while preserving the generated artwork.
$pixels = [byte[]]::new($source.PixelWidth * $source.PixelHeight * 4)
$source.CopyPixels($pixels, $source.PixelWidth * 4, 0)
$left = $source.PixelWidth; $top = $source.PixelHeight; $right = 0; $bottom = 0
for ($y = 0; $y -lt $source.PixelHeight; $y++) {
    for ($x = 0; $x -lt $source.PixelWidth; $x++) {
        if ($pixels[($y * $source.PixelWidth + $x) * 4 + 3] -gt 8) {
            $left = [Math]::Min($left, $x); $top = [Math]::Min($top, $y)
            $right = [Math]::Max($right, $x); $bottom = [Math]::Max($bottom, $y)
        }
    }
}
if ($right -le $left -or $bottom -le $top) { throw 'Icon source has no visible artwork.' }
$cropped = [Windows.Media.Imaging.CroppedBitmap]::new($source, [Windows.Int32Rect]::new($left, $top, $right - $left + 1, $bottom - $top + 1))
$sizes = @(16,20,24,32,40,48,64,128,256)
$frames = @()
foreach ($size in $sizes) {
    $visual = [Windows.Media.DrawingVisual]::new()
    [Windows.Media.RenderOptions]::SetBitmapScalingMode($visual, [Windows.Media.BitmapScalingMode]::HighQuality)
    $drawing = $visual.RenderOpen()
    $scale = ($size * 0.94) / [Math]::Max($cropped.PixelWidth, $cropped.PixelHeight)
    $width = $cropped.PixelWidth * $scale; $height = $cropped.PixelHeight * $scale
    $drawing.DrawImage($cropped, [Windows.Rect]::new(($size - $width) / 2, ($size - $height) / 2, $width, $height))
    $drawing.Close()
    $bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new($size, $size, 96, 96, [Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $memory = [IO.MemoryStream]::new()
    $encoder.Save($memory)
    $frames += ,$memory.ToArray()
    $memory.Dispose()
    if ($size -eq 256) { [IO.File]::WriteAllBytes((Join-Path $assetRoot 'winged-key.png'), $frames[-1]) }
}
$output = [IO.BinaryWriter]::new([IO.File]::Create((Join-Path $assetRoot 'ACZeroPCremap.ico')))
try {
    $output.Write([uint16]0); $output.Write([uint16]1); $output.Write([uint16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $dimension = if ($sizes[$i] -eq 256) { 0 } else { $sizes[$i] }
        $output.Write([byte]$dimension); $output.Write([byte]$dimension)
        $output.Write([byte]0); $output.Write([byte]0)
        $output.Write([uint16]1); $output.Write([uint16]32)
        $output.Write([uint32]$frames[$i].Length); $output.Write([uint32]$offset)
        $offset += $frames[$i].Length
    }
    foreach ($frame in $frames) { $output.Write([byte[]]$frame) }
} finally { $output.Dispose() }
Write-Host ('Created ACZeroPCremap.ico: ' + ($sizes -join ', ') + ' px with alpha transparency.')
