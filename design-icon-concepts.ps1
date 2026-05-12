# File: design-icon-concepts.ps1
# Purpose: Generates visual icon concept previews used to explore Clipboard Keeper app branding directions.

param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "assets\icon-concepts")
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

function New-SoftDiamondPath {
    param([float]$X, [float]$Y, [float]$Size)

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.StartFigure()
    $path.AddBezier(
        [System.Drawing.PointF]::new($X + $Size * 0.50, $Y),
        [System.Drawing.PointF]::new($X + $Size * 0.69, $Y),
        [System.Drawing.PointF]::new($X + $Size, $Y + $Size * 0.31),
        [System.Drawing.PointF]::new($X + $Size, $Y + $Size * 0.50)
    )
    $path.AddBezier(
        [System.Drawing.PointF]::new($X + $Size, $Y + $Size * 0.50),
        [System.Drawing.PointF]::new($X + $Size, $Y + $Size * 0.69),
        [System.Drawing.PointF]::new($X + $Size * 0.69, $Y + $Size),
        [System.Drawing.PointF]::new($X + $Size * 0.50, $Y + $Size)
    )
    $path.AddBezier(
        [System.Drawing.PointF]::new($X + $Size * 0.50, $Y + $Size),
        [System.Drawing.PointF]::new($X + $Size * 0.31, $Y + $Size),
        [System.Drawing.PointF]::new($X, $Y + $Size * 0.69),
        [System.Drawing.PointF]::new($X, $Y + $Size * 0.50)
    )
    $path.AddBezier(
        [System.Drawing.PointF]::new($X, $Y + $Size * 0.50),
        [System.Drawing.PointF]::new($X, $Y + $Size * 0.31),
        [System.Drawing.PointF]::new($X + $Size * 0.31, $Y),
        [System.Drawing.PointF]::new($X + $Size * 0.50, $Y)
    )
    $path.CloseFigure()
    return $path
}

function New-IconBitmap {
    param(
        [scriptblock]$Draw,
        [int]$Size = 512
    )

    $bitmap = New-Object System.Drawing.Bitmap $Size, $Size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
        $graphics.Clear([System.Drawing.Color]::Transparent)
        & $Draw $graphics 0 0 $Size
    }
    finally {
        $graphics.Dispose()
    }

    return $bitmap
}

function Draw-ConceptA {
    param([System.Drawing.Graphics]$Graphics, [float]$X, [float]$Y, [float]$Size)

    $s = $Size / 256.0
    $shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(55, 0, 18, 55))
    $bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush `
        ([System.Drawing.RectangleF]::new($X + 18 * $s, $Y + 18 * $s, 220 * $s, 220 * $s)),
        ([System.Drawing.Color]::FromArgb(255, 15, 188, 212)),
        ([System.Drawing.Color]::FromArgb(255, 73, 64, 255)),
        ([System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal)
    $paper = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(245, 255, 255, 255))
    $clip = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 232, 83))
    $ink = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 20, 28, 96))
    $line = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(120, 20, 28, 96))
    $rim = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(230, 255, 255, 255)), ([single](6 * $s))
    $font = $null
    try {
        Fill-RoundedRect $Graphics $shadow ($X + 22 * $s) ($Y + 28 * $s) (212 * $s) (212 * $s) (50 * $s)
        Fill-RoundedRect $Graphics $bg ($X + 18 * $s) ($Y + 16 * $s) (220 * $s) (220 * $s) (50 * $s)
        Stroke-RoundedRect $Graphics $rim ($X + 18 * $s) ($Y + 16 * $s) (220 * $s) (220 * $s) (50 * $s)

        Fill-RoundedRect $Graphics $paper ($X + 58 * $s) ($Y + 52 * $s) (140 * $s) (166 * $s) (18 * $s)
        Fill-RoundedRect $Graphics $clip ($X + 88 * $s) ($Y + 34 * $s) (80 * $s) (34 * $s) (16 * $s)
        Fill-RoundedRect $Graphics $ink ($X + 111 * $s) ($Y + 44 * $s) (34 * $s) (10 * $s) (5 * $s)
        Fill-RoundedRect $Graphics $line ($X + 132 * $s) ($Y + 168 * $s) (40 * $s) (8 * $s) (4 * $s)
        Fill-RoundedRect $Graphics $line ($X + 132 * $s) ($Y + 186 * $s) (28 * $s) (8 * $s) (4 * $s)

        $font = New-Object System.Drawing.Font -ArgumentList "Segoe UI Black", ([single](92 * $s)), ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
        $format = New-Object System.Drawing.StringFormat
        $format.Alignment = [System.Drawing.StringAlignment]::Center
        $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        $Graphics.DrawString("K", $font, $ink, [System.Drawing.RectangleF]::new($X + 54 * $s, $Y + 72 * $s, 148 * $s, 108 * $s), $format)
        $format.Dispose()
    }
    finally {
        if ($font -ne $null) { $font.Dispose() }
        $rim.Dispose()
        $line.Dispose()
        $ink.Dispose()
        $clip.Dispose()
        $paper.Dispose()
        $bg.Dispose()
        $shadow.Dispose()
    }
}

function Draw-ConceptB {
    param([System.Drawing.Graphics]$Graphics, [float]$X, [float]$Y, [float]$Size)

    $s = $Size / 256.0
    $diamond = New-SoftDiamondPath ($X + 16 * $s) ($Y + 16 * $s) (224 * $s)
    $shadow = New-SoftDiamondPath ($X + 20 * $s) ($Y + 26 * $s) (216 * $s)
    $shadowBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(52, 22, 12, 76))
    $bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush `
        ([System.Drawing.RectangleF]::new($X + 16 * $s, $Y + 16 * $s, 224 * $s, 224 * $s)),
        ([System.Drawing.Color]::FromArgb(255, 255, 77, 109)),
        ([System.Drawing.Color]::FromArgb(255, 255, 184, 77)),
        ([System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal)
    $card1 = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(230, 255, 255, 255))
    $card2 = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 255, 255))
    $teal = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 18, 178, 170))
    $dark = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 54, 35, 100))
    $rim = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(230, 255, 255, 255)), ([single](6 * $s))
    try {
        $Graphics.FillPath($shadowBrush, $shadow)
        $Graphics.FillPath($bg, $diamond)
        $Graphics.DrawPath($rim, $diamond)

        Fill-RoundedRect $Graphics $card1 ($X + 58 * $s) ($Y + 72 * $s) (112 * $s) (124 * $s) (18 * $s)
        Fill-RoundedRect $Graphics $card2 ($X + 82 * $s) ($Y + 56 * $s) (118 * $s) (134 * $s) (20 * $s)
        Fill-RoundedRect $Graphics $teal ($X + 104 * $s) ($Y + 42 * $s) (72 * $s) (30 * $s) (15 * $s)
        Fill-RoundedRect $Graphics $dark ($X + 124 * $s) ($Y + 51 * $s) (32 * $s) (8 * $s) (4 * $s)

        Fill-RoundedRect $Graphics $teal ($X + 110 * $s) ($Y + 104 * $s) (56 * $s) (10 * $s) (5 * $s)
        Fill-RoundedRect $Graphics $teal ($X + 110 * $s) ($Y + 128 * $s) (72 * $s) (10 * $s) (5 * $s)
        Fill-RoundedRect $Graphics $teal ($X + 110 * $s) ($Y + 152 * $s) (46 * $s) (10 * $s) (5 * $s)

        $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 54, 35, 100)), ([single](13 * $s))
        $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $Graphics.DrawArc($pen, $X + 58 * $s, $Y + 86 * $s, 64 * $s, 64 * $s, 130, 210)
        $pen.Dispose()
    }
    finally {
        $rim.Dispose()
        $dark.Dispose()
        $teal.Dispose()
        $card2.Dispose()
        $card1.Dispose()
        $bg.Dispose()
        $shadowBrush.Dispose()
        $shadow.Dispose()
        $diamond.Dispose()
    }
}

function Draw-ConceptC {
    param([System.Drawing.Graphics]$Graphics, [float]$X, [float]$Y, [float]$Size)

    $s = $Size / 256.0
    $bgPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $bgPath.AddPolygon(@(
        [System.Drawing.PointF]::new($X + 74 * $s, $Y + 18 * $s),
        [System.Drawing.PointF]::new($X + 182 * $s, $Y + 18 * $s),
        [System.Drawing.PointF]::new($X + 232 * $s, $Y + 74 * $s),
        [System.Drawing.PointF]::new($X + 226 * $s, $Y + 190 * $s),
        [System.Drawing.PointF]::new($X + 170 * $s, $Y + 236 * $s),
        [System.Drawing.PointF]::new($X + 60 * $s, $Y + 226 * $s),
        [System.Drawing.PointF]::new($X + 22 * $s, $Y + 164 * $s),
        [System.Drawing.PointF]::new($X + 24 * $s, $Y + 66 * $s)
    ))
    $bgPath.CloseFigure()
    $bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush `
        ([System.Drawing.RectangleF]::new($X + 20 * $s, $Y + 18 * $s, 216 * $s, 218 * $s)),
        ([System.Drawing.Color]::FromArgb(255, 94, 234, 212)),
        ([System.Drawing.Color]::FromArgb(255, 120, 64, 255)),
        ([System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
    $shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(55, 9, 21, 68))
    $white = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 255, 255))
    $navy = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 21, 31, 88))
    $lime = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 176, 255, 77))
    $rim = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(225, 255, 255, 255)), ([single](6 * $s))
    try {
        $shadowMatrix = New-Object System.Drawing.Drawing2D.Matrix
        $shadowMatrix.Translate(0, 8 * $s)
        $shadowPath = $bgPath.Clone()
        $shadowPath.Transform($shadowMatrix)
        $Graphics.FillPath($shadow, $shadowPath)
        $shadowPath.Dispose()
        $shadowMatrix.Dispose()

        $Graphics.FillPath($bg, $bgPath)
        $Graphics.DrawPath($rim, $bgPath)

        Fill-RoundedRect $Graphics $white ($X + 66 * $s) ($Y + 54 * $s) (82 * $s) (132 * $s) (16 * $s)
        Fill-RoundedRect $Graphics $lime ($X + 92 * $s) ($Y + 38 * $s) (74 * $s) (30 * $s) (15 * $s)
        Fill-RoundedRect $Graphics $navy ($X + 112 * $s) ($Y + 47 * $s) (34 * $s) (8 * $s) (4 * $s)

        $kPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 21, 31, 88)), ([single](19 * $s))
        $kPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $kPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $Graphics.DrawLine($kPen, $X + 101 * $s, $Y + 92 * $s, $X + 101 * $s, $Y + 158 * $s)
        $Graphics.DrawLine($kPen, $X + 105 * $s, $Y + 128 * $s, $X + 142 * $s, $Y + 92 * $s)
        $Graphics.DrawLine($kPen, $X + 105 * $s, $Y + 128 * $s, $X + 150 * $s, $Y + 164 * $s)
        $kPen.Dispose()

        Fill-RoundedRect $Graphics $lime ($X + 154 * $s) ($Y + 150 * $s) (38 * $s) (10 * $s) (5 * $s)
        Fill-RoundedRect $Graphics $lime ($X + 154 * $s) ($Y + 170 * $s) (52 * $s) (10 * $s) (5 * $s)
    }
    finally {
        $rim.Dispose()
        $lime.Dispose()
        $navy.Dispose()
        $white.Dispose()
        $shadow.Dispose()
        $bg.Dispose()
        $bgPath.Dispose()
    }
}

function Save-Concept {
    param([string]$Name, [scriptblock]$Draw)

    $bitmap = New-IconBitmap -Draw $Draw -Size 512
    try {
        $bitmap.Save((Join-Path $OutputDirectory ($Name + ".png")), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $bitmap.Dispose()
    }
}

function Draw-ScaledIcon {
    param(
        [System.Drawing.Graphics]$Graphics,
        [scriptblock]$Draw,
        [float]$X,
        [float]$Y,
        [float]$Size
    )

    & $Draw $Graphics $X $Y $Size
}

New-Item -ItemType Directory -Force $OutputDirectory | Out-Null

Save-Concept -Name "concept-a-keeper-k" -Draw ${function:Draw-ConceptA}
Save-Concept -Name "concept-b-clip-stack" -Draw ${function:Draw-ConceptB}
Save-Concept -Name "concept-c-signal-board" -Draw ${function:Draw-ConceptC}

$board = New-Object System.Drawing.Bitmap 1320, 620, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($board)
try {
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

    $graphics.Clear([System.Drawing.Color]::FromArgb(255, 246, 248, 252))
    $titleFont = New-Object System.Drawing.Font "Segoe UI", 28, ([System.Drawing.FontStyle]::Bold)
    $bodyFont = New-Object System.Drawing.Font "Segoe UI", 13, ([System.Drawing.FontStyle]::Regular)
    $labelFont = New-Object System.Drawing.Font "Segoe UI", 15, ([System.Drawing.FontStyle]::Bold)
    $muted = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 83, 94, 112))
    $ink = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 17, 24, 39))
    $card = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
    $cardPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 218, 226, 236)), 1

    $graphics.DrawString("Clipboard Keeper icon directions", $titleFont, $ink, 42, 34)
    $graphics.DrawString("Local Windows clipboard manager by Ryan Chen - fast reuse for text/images, private, tray-first utility.", $bodyFont, $muted, 44, 80)

    $items = @(
        @{ X = 44; Name = "A. Keeper K"; Tag = "Letter-led + clipboard tab"; Note = "Most direct brand mark. Strong at 16px." ; Draw = ${function:Draw-ConceptA} },
        @{ X = 474; Name = "B. Clip Stack"; Tag = "Object-led, no letters"; Note = "Most functional and friendly. Less brand-like." ; Draw = ${function:Draw-ConceptB} },
        @{ X = 904; Name = "C. Signal Board"; Tag = "Modern utility badge"; Note = "Most new-wave, sharper personality." ; Draw = ${function:Draw-ConceptC} }
    )

    foreach ($item in $items) {
        Fill-RoundedRect $graphics $card $item.X 126 372 438 22
        Stroke-RoundedRect $graphics $cardPen $item.X 126 372 438 22
        Draw-ScaledIcon $graphics $item.Draw ($item.X + 90) 158 192
        Draw-ScaledIcon $graphics $item.Draw ($item.X + 255) 316 48
        Draw-ScaledIcon $graphics $item.Draw ($item.X + 316) 328 24
        Draw-ScaledIcon $graphics $item.Draw ($item.X + 350) 332 16
        $graphics.DrawString($item.Name, $labelFont, $ink, ($item.X + 28), 382)
        $graphics.DrawString($item.Tag, $bodyFont, $muted, ($item.X + 28), 414)
        $graphics.DrawString($item.Note, $bodyFont, $muted, ($item.X + 28), 444)
    }

    $board.Save((Join-Path $OutputDirectory "clipboard-keeper-icon-concepts.png"), [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    if ($cardPen -ne $null) { $cardPen.Dispose() }
    if ($card -ne $null) { $card.Dispose() }
    if ($ink -ne $null) { $ink.Dispose() }
    if ($muted -ne $null) { $muted.Dispose() }
    if ($labelFont -ne $null) { $labelFont.Dispose() }
    if ($bodyFont -ne $null) { $bodyFont.Dispose() }
    if ($titleFont -ne $null) { $titleFont.Dispose() }
    $graphics.Dispose()
    $board.Dispose()
}

Write-Host "Generated icon concepts in $OutputDirectory"
