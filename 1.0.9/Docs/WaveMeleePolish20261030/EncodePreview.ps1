param([string]$FrameDirectory,[string]$OutputPath)
Add-Type -AssemblyName PresentationCore
$encoder = New-Object System.Windows.Media.Imaging.GifBitmapEncoder
$frames = Get-ChildItem -LiteralPath $FrameDirectory -Filter '*.png' | Sort-Object Name
foreach ($file in $frames) {
    $bitmap = [System.Windows.Media.Imaging.BitmapFrame]::Create([Uri]$file.FullName, [System.Windows.Media.Imaging.BitmapCreateOptions]::PreservePixelFormat, [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
    $metadata = New-Object System.Windows.Media.Imaging.BitmapMetadata 'gif'
    $metadata.SetQuery('/grctlext/Delay', [UInt16]5)
    $metadata.SetQuery('/grctlext/Disposal', [Byte]2)
    if ($encoder.Frames.Count -eq 0) {
        $metadata.SetQuery('/appext/Application', [Text.Encoding]::ASCII.GetBytes('NETSCAPE2.0'))
        $metadata.SetQuery('/appext/Data', [byte[]](3,1,0,0,0))
    }
    $frame = [System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap, $null, $metadata, $null)
    $encoder.Frames.Add($frame)
}
$stream = [System.IO.File]::Create($OutputPath)
try { $encoder.Save($stream) } finally { $stream.Dispose() }
Write-Output ("Encoded {0} frames; {1} bytes" -f $frames.Count, (Get-Item -LiteralPath $OutputPath).Length)
