# File: generate-icon.ps1
# Purpose: Generates the application icon assets used by the Clipboard Keeper executable.

param(
    [string]$OutputPath = (Join-Path $PSScriptRoot "assets\ClipboardKeeper.ico")
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

function New-RoundedRectPath {
    param(
        [float]$X,
        [float]$Y,
        [float]$Width,
        [float]$Height,
        [float]$Radius
    )

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = $Radius * 2
    $path.AddArc($X, $Y, $diameter, $diameter, 180, 90)
    $path.AddArc($X + $Width - $diameter, $Y, $diameter, $diameter, 270, 90)
    $path.AddArc($X + $Width - $diameter, $Y + $Height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($X, $Y + $Height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function Fill-RoundedRect {
    param(
        [System.Drawing.Graphics]$Graphics,
        [System.Drawing.Brush]$Brush,
        [float]$X,
        [float]$Y,
        [float]$Width,
        [float]$Height,
        [float]$Radius
    )

    $path = New-RoundedRectPath $X $Y $Width $Height $Radius
    try {
        $Graphics.FillPath($Brush, $path)
    }
    finally {
        $path.Dispose()
    }
}

function Stroke-RoundedRect {
    param(
        [System.Drawing.Graphics]$Graphics,
        [System.Drawing.Pen]$Pen,
        [float]$X,
        [float]$Y,
        [float]$Width,
        [float]$Height,
        [float]$Radius
    )

    $path = New-RoundedRectPath $X $Y $Width $Height $Radius
    try {
        $Graphics.DrawPath($Pen, $path)
    }
    finally {
        $path.Dispose()
    }
}

function New-SignalStackBadgePath {
    param([float]$Scale)

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddPolygon(@(
        [System.Drawing.PointF]::new(72 * $Scale, 14 * $Scale),
        [System.Drawing.PointF]::new(184 * $Scale, 14 * $Scale),
        [System.Drawing.PointF]::new(236 * $Scale, 70 * $Scale),
        [System.Drawing.PointF]::new(230 * $Scale, 188 * $Scale),
        [System.Drawing.PointF]::new(172 * $Scale, 238 * $Scale),
        [System.Drawing.PointF]::new(62 * $Scale, 228 * $Scale),
        [System.Drawing.PointF]::new(20 * $Scale, 166 * $Scale),
        [System.Drawing.PointF]::new(24 * $Scale, 66 * $Scale)
    ))
    $path.CloseFigure()
    return $path
}

function New-ClipboardIconBitmap {
    param([int]$Size)

    $bitmap = New-Object System.Drawing.Bitmap $Size, $Size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $scale = $Size / 256.0

    if ($Size -le 512) {
        $smallBounds = [System.Drawing.RectangleF]::new(6 * $scale, 6 * $scale, 244 * $scale, 244 * $scale)
        $smallBgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush `
            ($smallBounds),
            ([System.Drawing.Color]::FromArgb(255, 36, 236, 205)),
            ([System.Drawing.Color]::FromArgb(255, 76, 92, 255)),
            ([System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal)
        $smallBorderPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(248, 255, 255, 255)), ([single](12 * $scale))
        $smallCardBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(252, 255, 255, 255))
        $smallCardStrokePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(118, 24, 36, 112)), ([single](5 * $scale))
        $smallCardShadowBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(46, 18, 28, 88))
        $smallClipBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 198, 255, 72))
        $smallClipHoleBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 24, 30, 118))
        $smallHaloPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(214, 255, 255, 255)), ([single](48 * $scale))
        $smallKPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 23, 30, 132)), ([single](32 * $scale))

        try {
            Fill-RoundedRect $graphics $smallBgBrush (6 * $scale) (6 * $scale) (244 * $scale) (244 * $scale) (58 * $scale)
            Stroke-RoundedRect $graphics $smallBorderPen (6 * $scale) (6 * $scale) (244 * $scale) (244 * $scale) (58 * $scale)

            Fill-RoundedRect $graphics $smallClipBrush (88 * $scale) (10 * $scale) (80 * $scale) (44 * $scale) (20 * $scale)
            Fill-RoundedRect $graphics $smallClipHoleBrush (112 * $scale) (26 * $scale) (32 * $scale) (10 * $scale) (5 * $scale)

            $smallHaloPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
            $smallHaloPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
            $smallKPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
            $smallKPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

            $graphics.DrawLine($smallHaloPen, 82 * $scale, 66 * $scale, 82 * $scale, 202 * $scale)
            $graphics.DrawLine($smallHaloPen, 96 * $scale, 138 * $scale, 176 * $scale, 66 * $scale)
            $graphics.DrawLine($smallHaloPen, 96 * $scale, 138 * $scale, 186 * $scale, 206 * $scale)

            $graphics.DrawLine($smallKPen, 82 * $scale, 66 * $scale, 82 * $scale, 202 * $scale)
            $graphics.DrawLine($smallKPen, 96 * $scale, 138 * $scale, 176 * $scale, 66 * $scale)
            $graphics.DrawLine($smallKPen, 96 * $scale, 138 * $scale, 186 * $scale, 206 * $scale)
        }
        finally {
            $smallKPen.Dispose()
            $smallHaloPen.Dispose()
            $smallClipHoleBrush.Dispose()
            $smallClipBrush.Dispose()
            $smallCardShadowBrush.Dispose()
            $smallCardStrokePen.Dispose()
            $smallCardBrush.Dispose()
            $smallBorderPen.Dispose()
            $smallBgBrush.Dispose()
            $graphics.Dispose()
        }

        return $bitmap
    }

    $badgePath = New-SignalStackBadgePath $scale
    $shadowPath = New-SignalStackBadgePath $scale
    $shadowMatrix = New-Object System.Drawing.Drawing2D.Matrix
    $shadowMatrix.Translate(0, 8 * $scale)
    $shadowPath.Transform($shadowMatrix)

    $bounds = [System.Drawing.RectangleF]::new(16 * $scale, 12 * $scale, 224 * $scale, 230 * $scale)
    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush `
        ($bounds),
        ([System.Drawing.Color]::FromArgb(255, 62, 232, 214)),
        ([System.Drawing.Color]::FromArgb(255, 112, 64, 255)),
        ([System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
    $shadowBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(62, 9, 21, 68))
    $rimPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(220, 255, 255, 255)), ([single](5.5 * $scale))
    $bottomAccentPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(145, 255, 255, 255)), ([single](5 * $scale))

    $backCardBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(210, 255, 255, 255))
    $frontCardBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(250, 255, 255, 255))
    $frontStrokePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(110, 15, 23, 84)), ([single](2.5 * $scale))
    $clipBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 185, 255, 75))
    $clipHoleBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 22, 28, 86))
    $navyBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 22, 28, 86))
    $tealBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 16, 178, 170))
    $limeBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 185, 255, 75))

    try {
        $graphics.FillPath($shadowBrush, $shadowPath)
        $graphics.FillPath($bgBrush, $badgePath)
        $graphics.DrawPath($rimPen, $badgePath)

        $bottomAccentPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $bottomAccentPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $graphics.DrawLine($bottomAccentPen, 56 * $scale, 210 * $scale, 132 * $scale, 210 * $scale)

        Fill-RoundedRect $graphics $backCardBrush (58 * $scale) (72 * $scale) (112 * $scale) (124 * $scale) (18 * $scale)
        Fill-RoundedRect $graphics $frontCardBrush (82 * $scale) (54 * $scale) (118 * $scale) (138 * $scale) (20 * $scale)
        Stroke-RoundedRect $graphics $frontStrokePen (82 * $scale) (54 * $scale) (118 * $scale) (138 * $scale) (20 * $scale)

        Fill-RoundedRect $graphics $clipBrush (104 * $scale) (36 * $scale) (74 * $scale) (32 * $scale) (16 * $scale)
        Fill-RoundedRect $graphics $clipHoleBrush (124 * $scale) (46 * $scale) (34 * $scale) (8 * $scale) (4 * $scale)

        if ($Size -ge 32) {
            $kPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 22, 28, 86)), ([single](15 * $scale))
            $kPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
            $kPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
            $graphics.DrawLine($kPen, 111 * $scale, 94 * $scale, 111 * $scale, 158 * $scale)
            $graphics.DrawLine($kPen, 116 * $scale, 128 * $scale, 148 * $scale, 94 * $scale)
            $graphics.DrawLine($kPen, 116 * $scale, 128 * $scale, 154 * $scale, 160 * $scale)
            $kPen.Dispose()

            Fill-RoundedRect $graphics $tealBrush (150 * $scale) (108 * $scale) (34 * $scale) (8 * $scale) (4 * $scale)
            Fill-RoundedRect $graphics $tealBrush (150 * $scale) (132 * $scale) (42 * $scale) (8 * $scale) (4 * $scale)
            Fill-RoundedRect $graphics $limeBrush (150 * $scale) (156 * $scale) (28 * $scale) (8 * $scale) (4 * $scale)
        }
        else {
            $smallFont = New-Object System.Drawing.Font -ArgumentList "Segoe UI Black", ([single](72 * $scale)), ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
            $smallFormat = New-Object System.Drawing.StringFormat
            try {
                $smallFormat.Alignment = [System.Drawing.StringAlignment]::Center
                $smallFormat.LineAlignment = [System.Drawing.StringAlignment]::Center
                $smallFormat.FormatFlags = [System.Drawing.StringFormatFlags]::NoWrap
                $graphics.DrawString("K", $smallFont, $navyBrush, [System.Drawing.RectangleF]::new(90 * $scale, 82 * $scale, 90 * $scale, 82 * $scale), $smallFormat)
                Fill-RoundedRect $graphics $tealBrush (146 * $scale) (122 * $scale) (34 * $scale) (8 * $scale) (4 * $scale)
                Fill-RoundedRect $graphics $limeBrush (146 * $scale) (144 * $scale) (24 * $scale) (8 * $scale) (4 * $scale)
            }
            finally {
                $smallFormat.Dispose()
                $smallFont.Dispose()
            }
        }
    }
    finally {
        $limeBrush.Dispose()
        $tealBrush.Dispose()
        $navyBrush.Dispose()
        $clipHoleBrush.Dispose()
        $clipBrush.Dispose()
        $frontStrokePen.Dispose()
        $frontCardBrush.Dispose()
        $backCardBrush.Dispose()
        $bottomAccentPen.Dispose()
        $rimPen.Dispose()
        $shadowBrush.Dispose()
        $bgBrush.Dispose()
        $shadowMatrix.Dispose()
        $shadowPath.Dispose()
        $badgePath.Dispose()
        $graphics.Dispose()
    }

    return $bitmap
}

function ConvertTo-PngBytes {
    param([System.Drawing.Bitmap]$Bitmap)

    $stream = New-Object System.IO.MemoryStream
    try {
        $Bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        return $stream.ToArray()
    }
    finally {
        $stream.Dispose()
    }
}

function Save-BitmapAsPng {
    param(
        [System.Drawing.Bitmap]$Bitmap,
        [string]$Path
    )

    $directory = Split-Path -Parent $Path
    New-Item -ItemType Directory -Force $directory | Out-Null
    $Bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
}

function Write-IcoFile {
    param(
        [string]$Path,
        [int[]]$Sizes,
        [byte[][]]$ImageBytes
    )

    $directory = Split-Path -Parent $Path
    New-Item -ItemType Directory -Force $directory | Out-Null

    $stream = [System.IO.File]::Create($Path)
    $writer = New-Object System.IO.BinaryWriter $stream
    try {
        $writer.Write([UInt16]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]$Sizes.Count)

        $offset = 6 + (16 * $Sizes.Count)
        for ($i = 0; $i -lt $Sizes.Count; $i++) {
            $widthByte = if ($Sizes[$i] -eq 256) { 0 } else { $Sizes[$i] }
            $writer.Write([byte]$widthByte)
            $writer.Write([byte]$widthByte)
            $writer.Write([byte]0)
            $writer.Write([byte]0)
            $writer.Write([UInt16]1)
            $writer.Write([UInt16]32)
            $writer.Write([UInt32]$ImageBytes[$i].Length)
            $writer.Write([UInt32]$offset)
            $offset += $ImageBytes[$i].Length
        }

        foreach ($bytes in $ImageBytes) {
            $writer.Write($bytes)
        }
    }
    finally {
        $writer.Dispose()
        $stream.Dispose()
    }
}

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$images = New-Object 'System.Collections.Generic.List[byte[]]'

foreach ($size in $sizes) {
    $bitmap = New-ClipboardIconBitmap $size
    try {
        [void]$images.Add((ConvertTo-PngBytes $bitmap))
    }
    finally {
        $bitmap.Dispose()
    }
}

Write-IcoFile -Path $OutputPath -Sizes $sizes -ImageBytes $images.ToArray()

$assetRoot = Split-Path -Parent $OutputPath
$previewMap = @{
    "ClipboardKeeper-exe-small.png" = 16
    "ClipboardKeeper-exe-large.png" = 32
    "ClipboardKeeper-exe-icon-preview.png" = 32
    "ClipboardKeeper-preview.png" = 512
}

foreach ($previewName in $previewMap.Keys) {
    $previewBitmap = New-ClipboardIconBitmap $previewMap[$previewName]
    try {
        Save-BitmapAsPng -Bitmap $previewBitmap -Path (Join-Path $assetRoot $previewName)
    }
    finally {
        $previewBitmap.Dispose()
    }
}

Write-Host "Generated $OutputPath"
