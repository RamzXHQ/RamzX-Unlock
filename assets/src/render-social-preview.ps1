# Renders assets/social-preview.png (1280x640) - the raster twin of assets/banner.svg.
# GitHub's social preview needs PNG/JPG; this uses only .NET System.Drawing (no browser, no downloads).
# ASCII-only on purpose (Windows PowerShell 5.1 reads scripts as ANSI).
# Usage: powershell -NoProfile -File assets/src/render-social-preview.ps1

Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'

$W = 1280; $H = 640
$out = Join-Path (Split-Path $PSScriptRoot -Parent) 'social-preview.png'

function C([string]$hex, [int]$a = 255) {
    $h = $hex.TrimStart('#')
    [Drawing.Color]::FromArgb($a, [Convert]::ToInt32($h.Substring(0,2),16),
        [Convert]::ToInt32($h.Substring(2,2),16), [Convert]::ToInt32($h.Substring(4,2),16))
}

$bmp = New-Object Drawing.Bitmap $W, $H
$g = [Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'
$g.TextRenderingHint = 'AntiAliasGridFit'
$g.PixelOffsetMode = 'HighQuality'

# Background: diagonal gradient + blue glow top right
$bg = New-Object Drawing.Drawing2D.LinearGradientBrush (New-Object Drawing.Point 0,0), (New-Object Drawing.Point $W,$H), (C '#0a1020'), (C '#0b1326')
$blend = New-Object Drawing.Drawing2D.ColorBlend 3
$blend.Colors = @((C '#0a1020'), (C '#101a33'), (C '#0b1326'))
$blend.Positions = @(0.0, 0.55, 1.0)
$bg.InterpolationColors = $blend
$g.FillRectangle($bg, 0, 0, $W, $H)

$glowPath = New-Object Drawing.Drawing2D.GraphicsPath
$glowPath.AddEllipse(1088 - 760, 192 - 560, 1520, 1120)
$glow = New-Object Drawing.Drawing2D.PathGradientBrush $glowPath
$glow.CenterPoint = New-Object Drawing.PointF 1088, 192
$glow.CenterColor = C '#1d4ed8' 210
$glow.SurroundColors = @((C '#1d4ed8' 0))
$g.FillPath($glow, $glowPath)

# Text helpers (SVG-style baseline positioning)
$fmt = [Drawing.StringFormat]::GenericTypographic.Clone()
$fmt.FormatFlags = $fmt.FormatFlags -bor [Drawing.StringFormatFlags]::MeasureTrailingSpaces
function Font([string]$family, [float]$px, [string]$style = 'Regular') {
    New-Object Drawing.Font $family, $px, ([Drawing.FontStyle]$style), ([Drawing.GraphicsUnit]::Pixel)
}
function Ascent($f) { $f.Size * $f.FontFamily.GetCellAscent($f.Style) / $f.FontFamily.GetEmHeight($f.Style) }
function Text([string]$s, $f, [string]$hex, [float]$x, [float]$baseline) {
    $b = New-Object Drawing.SolidBrush (C $hex)
    $g.DrawString($s, $f, $b, $x, $baseline - (Ascent $f), $fmt)
    $g.MeasureString($s, $f, 2000, $fmt).Width
}
function Runs([object[]]$runs, [float]$x, [float]$baseline) {
    foreach ($r in $runs) { $x += Text $r[0] $r[1] $r[2] $x $baseline }
}

$fKicker = Font 'Segoe UI Semibold' 22
$x = 80
foreach ($ch in "KEEPASS 2 PLUGIN $([char]0xB7) WINDOWS HELLO".ToCharArray()) { $x += (Text ([string]$ch) $fKicker '#7dd3fc' $x 170) + 3 }

[void](Text 'Miftawerk Unlock' (Font 'Segoe UI' 84 'Bold') '#f1f5ff' 76 262)

$fSub = Font 'Segoe UI' 33; $fSubB = Font 'Segoe UI Semibold' 33
Runs @(@('Unlock KeePass with your ', $fSub, '#c7d2fe'), @('face', $fSubB, '#ffffff'), @(', ', $fSub, '#c7d2fe'), @('fingerprint', $fSubB, '#ffffff')) 80 326
Runs @(@('or ', $fSub, '#c7d2fe'), @('Windows Hello PIN', $fSubB, '#ffffff'), @('.', $fSub, '#c7d2fe')) 80 370

function RoundRect([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $p = New-Object Drawing.Drawing2D.GraphicsPath
    $d = 2 * $r
    $p.AddArc($x, $y, $d, $d, 180, 90); $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90); $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure(); $p
}
$fChip = Font 'Segoe UI' 21
$chipFill = New-Object Drawing.SolidBrush (C '#ffffff' 20)
$chipPen = New-Object Drawing.Pen (C '#ffffff' 41), 1
foreach ($c in @(@(80,410,212,'TPM-backed key'), @(304,410,166,'Auto-expiry'), @(482,410,236,'Locks with Windows'),
                 @(80,466,182,'No telemetry'), @(274,466,246,'Free & open source'))) {
    $p = RoundRect $c[0] $c[1] $c[2] 44 22
    $g.FillPath($chipFill, $p); $g.DrawPath($chipPen, $p)
    $tw = $g.MeasureString($c[3], $fChip, 2000, $fmt).Width
    [void](Text $c[3] $fChip '#e8eefc' ($c[0] + ($c[2] - $tw) / 2) ($c[1] + 29))
}

Runs @(@('by ', (Font 'Segoe UI' 22), '#94a3b8'), @('SeeColors iT', (Font 'Segoe UI Semibold' 22), '#e2e8f0'),
       @(" $([char]0xB7) seecolors.de $([char]0xB7) MIT license", (Font 'Segoe UI' 22), '#94a3b8')) 80 590

# Illustration (same geometry as banner.svg, translated by 790,110)
$g.TranslateTransform(790, 110)
$grad = New-Object Drawing.Drawing2D.LinearGradientBrush (New-Object Drawing.Point 0,0), (New-Object Drawing.Point 420,420), (C '#38bdf8'), (C '#6366f1')
function GradPen([float]$w) { $p = New-Object Drawing.Pen $grad, $w; $p.StartCap = 'Round'; $p.EndCap = 'Round'; $p }

$g.FillEllipse((New-Object Drawing.SolidBrush (C '#ffffff' 10)), 14, 14, 392, 392)
$g.DrawEllipse((New-Object Drawing.Pen (C '#ffffff' 31), 2), 14, 14, 392, 392)

$shackle = New-Object Drawing.Drawing2D.GraphicsPath
$shackle.AddLine(140, 170, 140, 130); $shackle.AddArc(140, 60, 140, 140, 180, 180); $shackle.AddLine(280, 130, 280, 170)
$g.DrawPath((GradPen 22), $shackle)

$body = RoundRect 100 165 220 175 34
$g.FillPath((New-Object Drawing.SolidBrush (C '#0f172a')), $body)
$g.DrawPath((GradPen 6), $body)

$fp = GradPen 7
foreach ($curve in @(
    @(168,300, 154,282,148,262,152,240, 158,210,184,192,210,192, 240,192,264,214,268,242),
    @(186,312, 174,296,170,278,174,260, 178,240,194,228,212,228, 232,228,248,244,248,266, 248,280,244,292,246,306),
    @(210,318, 200,296,198,278,202,262, 204,254,208,250,212,250, 218,250,222,256,222,266, 222,288,228,302,238,314),
    @(232,320, 226,310,222,298,222,284))) {
    $pts = for ($i = 0; $i -lt $curve.Count; $i += 2) { New-Object Drawing.PointF $curve[$i], $curve[$i + 1] }
    $g.DrawBeziers($fp, [Drawing.PointF[]]$pts)
}

$g.FillEllipse((New-Object Drawing.SolidBrush (C '#0f172a')), 284, 64, 92, 92)
$g.DrawEllipse((GradPen 5), 284, 64, 92, 92)
$face = New-Object Drawing.Pen (C '#7dd3fc'), 6; $face.StartCap = 'Round'; $face.EndCap = 'Round'
$g.DrawLine($face, 312, 100, 312, 106); $g.DrawLine($face, 348, 100, 348, 106)
$g.DrawBezier($face, 312, 124, 324, 134.67, 336, 134.67, 348, 124)

$g.Dispose()
$bmp.Save($out, [Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
"Wrote $out"
