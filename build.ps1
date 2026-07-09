param(
    [switch]$NoShortcut
)

$ErrorActionPreference = 'Stop'
$dir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $dir
Add-Type -AssemblyName System.Drawing

# ---------- 1) Generate clock.ico ----------
function New-ClockIco($path) {
    $sz = 256
    $bmp = New-Object System.Drawing.Bitmap $sz, $sz
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)

    $bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.Point 0,0),
        (New-Object System.Drawing.Point $sz,$sz),
        [System.Drawing.Color]::FromArgb(255,137,180,250),
        [System.Drawing.Color]::FromArgb(255,180,140,250))
    $g.FillEllipse($bg, 8, 8, ($sz-16), ($sz-16))

    $face = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255,24,24,37))
    $g.FillEllipse($face, 28, 28, ($sz-56), ($sz-56))

    $cx = $sz/2; $cy = $sz/2
    $tick = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255,205,214,244)), 6
    for ($i=0; $i -lt 12; $i++) {
        $a = [Math]::PI * 2 * $i / 12
        $r1 = 92; $r2 = 104
        $g.DrawLine($tick,
            [single]($cx + $r1*[Math]::Sin($a)), [single]($cy - $r1*[Math]::Cos($a)),
            [single]($cx + $r2*[Math]::Sin($a)), [single]($cy - $r2*[Math]::Cos($a)))
    }
    $hHour = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255,205,214,244)), 12
    $hHour.StartCap = 'Round'; $hHour.EndCap='Round'
    $g.DrawLine($hHour, [single]$cx, [single]$cy, [single]($cx+40), [single]($cy-48))
    $hMin = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255,137,180,250)), 8
    $hMin.StartCap='Round'; $hMin.EndCap='Round'
    $g.DrawLine($hMin, [single]$cx, [single]$cy, [single]($cx-14), [single]($cy-78))
    $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255,137,180,250))), ($cx-9), ($cy-9), 18, 18)
    $g.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $png = $ms.ToArray()
    $ms.Dispose(); $bmp.Dispose()

    $fs = [System.IO.File]::Create($path)
    $bw = New-Object System.IO.BinaryWriter $fs
    $bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]1)   # ICONDIR
    $bw.Write([Byte]0); $bw.Write([Byte]0)                             # 256x256 -> 0,0
    $bw.Write([Byte]0); $bw.Write([Byte]0)
    $bw.Write([UInt16]1); $bw.Write([UInt16]32)
    $bw.Write([UInt32]$png.Length)
    $bw.Write([UInt32]22)                                              # offset = 6+16
    $bw.Write($png)
    $bw.Flush(); $bw.Close(); $fs.Close()
}
New-ClockIco (Join-Path $dir 'clock.ico')
Write-Host "[1/3] Generated clock.ico" -ForegroundColor Green

# ---------- 2) Build exe ----------
$csc = Join-Path ([Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory()) 'csc.exe'
if (-not (Test-Path $csc)) {
    $framework64 = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    $framework32 = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
    if (Test-Path $framework64) { $csc = $framework64 }
    elseif (Test-Path $framework32) { $csc = $framework32 }
    else { throw "csc.exe not found" }
}
$args = @(
    '/nologo','/target:winexe','/codepage:65001','/optimize+',
    '/out:WorldClock.exe','/win32icon:clock.ico','/win32manifest:app.manifest',
    '/reference:System.Windows.Forms.dll',
    '/reference:System.Drawing.dll',
    'WorldClock.cs'
)
& $csc $args
if ($LASTEXITCODE -ne 0) { throw "Build failed" }
Write-Host "[2/3] Built WorldClock.exe" -ForegroundColor Green

# ---------- 3) Create Start Menu shortcut ----------
$exe = Join-Path $dir 'WorldClock.exe'
if ($NoShortcut) {
    Write-Host "[3/3] Skipped Start Menu shortcut" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Done. Executable: $exe" -ForegroundColor Cyan
    return
}

$startMenu = [Environment]::GetFolderPath('Programs')
$lnk = Join-Path $startMenu 'World Clock.lnk'
$ws = New-Object -ComObject WScript.Shell
$sc = $ws.CreateShortcut($lnk)
$sc.TargetPath = $exe
$sc.WorkingDirectory = $dir
$sc.IconLocation = (Join-Path $dir 'clock.ico')
$sc.Description = 'Desktop world clock widget'
$sc.Save()
Write-Host "[3/3] Created Start Menu shortcut" -ForegroundColor Green
Write-Host ""
Write-Host "Done. Executable: $exe" -ForegroundColor Cyan
