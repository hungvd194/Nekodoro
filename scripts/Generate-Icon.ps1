# Rebuild Nekodoro's original cat icon; keep it readable down to 16 pixels.
Add-Type -AssemblyName System.Drawing
$assetDirectory = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../src/PomodoroTaskbar.Windows/Assets'))
$canvas = [Drawing.Bitmap]::new(256, 256)
$graphics = [Drawing.Graphics]::FromImage($canvas)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([Drawing.Color]::Transparent)
$body = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#FFF4DF'))
$leaf = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#7F9276'))
$ink = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#554B41'))
$patch = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#A89A87'))
$outline = [Drawing.Pen]::new($ink, 9)
$outline.LineJoin = [Drawing.Drawing2D.LineJoin]::Round
$smile = [Drawing.Pen]::new($ink, 6)
$smile.StartCap = $smile.EndCap = [Drawing.Drawing2D.LineCap]::Round
$leftEar = [Drawing.Point[]]@([Drawing.Point]::new(26,105),[Drawing.Point]::new(40,20),[Drawing.Point]::new(108,68))
$rightEar = [Drawing.Point[]]@([Drawing.Point]::new(151,68),[Drawing.Point]::new(215,20),[Drawing.Point]::new(232,108))
$graphics.FillPolygon($patch, $leftEar); $graphics.DrawPolygon($outline, $leftEar)
$graphics.FillPolygon($patch, $rightEar); $graphics.DrawPolygon($outline, $rightEar)
$graphics.FillEllipse($body, 15, 62, 226, 181); $graphics.DrawEllipse($outline, 15, 62, 226, 181)
$graphics.FillPie($patch, 20, 66, 216, 172, 183, 60)
$graphics.FillEllipse($ink, 78, 137, 15, 18); $graphics.FillEllipse($ink, 166, 137, 15, 18)
$graphics.FillEllipse($ink, 121, 163, 14, 10)
$graphics.DrawArc($smile, 113, 171, 30, 19, 15, 150)
$graphics.DrawLine($smile, 28, 153, 50, 159); $graphics.DrawLine($smile, 27, 181, 49, 177)
$graphics.DrawLine($smile, 207, 159, 228, 153); $graphics.DrawLine($smile, 207, 177, 230, 181)
$tomato = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#BF6559'))
$graphics.FillEllipse($tomato, 100, 31, 57, 45)
$leaves = [Drawing.Point[]]@([Drawing.Point]::new(128,43),[Drawing.Point]::new(111,29),[Drawing.Point]::new(126,31),[Drawing.Point]::new(139,16),[Drawing.Point]::new(138,35),[Drawing.Point]::new(153,32),[Drawing.Point]::new(141,46))
$graphics.FillPolygon($leaf, $leaves)
$tomato.Dispose(); $patch.Dispose(); $outline.Dispose()
$canvas.Save((Join-Path $assetDirectory 'App.png'), [Drawing.Imaging.ImageFormat]::Png)
$sizes = @(16,20,24,32,48,64,128,256)
$frames = foreach ($size in $sizes) {
    $bitmap = [Drawing.Bitmap]::new($size, $size)
    $render = [Drawing.Graphics]::FromImage($bitmap)
    $render.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $render.DrawImage($canvas, 0, 0, $size, $size)
    $buffer = [IO.MemoryStream]::new()
    $bitmap.Save($buffer, [Drawing.Imaging.ImageFormat]::Png)
    $bytes = $buffer.ToArray()
    $buffer.Dispose(); $render.Dispose(); $bitmap.Dispose()
    [pscustomobject]@{ Size = $size; Bytes = $bytes }
}
$stream = [IO.File]::Create((Join-Path $assetDirectory 'App.ico'))
$writer = [IO.BinaryWriter]::new($stream)
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$frames.Count)
$offset = 6 + 16 * $frames.Count
foreach ($frame in $frames) {
    $dimension = if ($frame.Size -eq 256) { 0 } else { $frame.Size }
    $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
    $writer.Write([byte]0); $writer.Write([byte]0)
    $writer.Write([uint16]1); $writer.Write([uint16]32)
    $writer.Write([uint32]$frame.Bytes.Length); $writer.Write([uint32]$offset)
    $offset += $frame.Bytes.Length
}
foreach ($frame in $frames) { $writer.Write([byte[]]$frame.Bytes) }
$writer.Dispose(); $stream.Dispose()
$smile.Dispose(); $ink.Dispose(); $leaf.Dispose(); $body.Dispose(); $graphics.Dispose(); $canvas.Dispose()

