# Creates a new, independent source archive. Does not stage, commit, push, delete or move anything.
[CmdletBinding()]
param([string]$TargetRepository, [switch]$CheckSource)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$sourceRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..')).TrimEnd('\')
$kit = Join-Path $sourceRoot 'Docs\Release106'
$gitExe = (Get-Command git -CommandType Application -ErrorAction Stop).Source

function Read-Git([string]$Directory, [string]$Arguments) {
    $psi = New-Object Diagnostics.ProcessStartInfo
    $psi.FileName = $gitExe
    $psi.Arguments = $Arguments
    $psi.WorkingDirectory = $Directory
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.StandardOutputEncoding = New-Object Text.UTF8Encoding($false)
    $psi.StandardErrorEncoding = New-Object Text.UTF8Encoding($false)
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $psi
    [void]$process.Start()
    $output = $process.StandardOutput.ReadToEnd()
    $errorText = $process.StandardError.ReadToEnd()
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "git $Arguments failed: $errorText" }
    $process.Dispose()
    return $output
}
function Normalized-Hash([string]$Path) {
    $text = [IO.File]::ReadAllText($Path).Replace("`r`n", "`n")
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($text)))).Replace('-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}
function Write-Utf8([string]$Path, [string]$Text) {
    [IO.File]::WriteAllText($Path, $Text, (New-Object Text.UTF8Encoding($false)))
}

foreach ($required in @('Assets','Packages','ProjectSettings','Docs','AGENTS.md','.gitattributes','.gitignore')) {
    if (!(Test-Path -LiteralPath (Join-Path $sourceRoot $required))) { throw "Source is missing $required" }
}
$raw = Read-Git $sourceRoot 'ls-files -z --cached --others --exclude-standard -- .'
$names = @($raw.Split([char]0, [StringSplitOptions]::RemoveEmptyEntries) | Sort-Object -Unique)
$files = @()
$bytes = [long]0
foreach ($relative in $names) {
    $full = [IO.Path]::GetFullPath((Join-Path $sourceRoot $relative))
    if (!$full.StartsWith($sourceRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Source path escaped project root' }
    if (!(Test-Path -LiteralPath $full -PathType Leaf)) { continue }
    if ($relative -match '^(Library|Temp|Logs|Obj|Build|Builds|UserSettings|\.git|\.vs|\.idea|\.vscode)[/\\]') { throw "Cache unexpectedly selected: $relative" }
    $stream = [IO.File]::OpenRead($full)
    try {
        $buffer = New-Object byte[] 120
        $read = $stream.Read($buffer, 0, $buffer.Length)
        if ([Text.Encoding]::ASCII.GetString($buffer,0,$read).StartsWith('version https://git-lfs.github.com/spec/v1')) { throw "Unresolved Git LFS pointer: $relative. Download source LFS files first." }
        $bytes += $stream.Length
    } finally { $stream.Dispose() }
    $files += $relative
}
Write-Host ('Source: {0}' -f $sourceRoot)
Write-Host ('Files: {0}; size: {1:N2} GiB (Unity caches excluded)' -f $files.Count, ($bytes / 1GB))
if ($CheckSource) { Write-Host 'Source check complete. No destination files written.'; return }
if ([string]::IsNullOrWhiteSpace($TargetRepository)) { throw 'Specify -TargetRepository with the freshly cloned lixiang repository root.' }
$repo = [IO.Path]::GetFullPath((Resolve-Path -LiteralPath $TargetRepository).Path).TrimEnd('\')
$gitRoot = (Read-Git $repo 'rev-parse --show-toplevel').Trim().Replace('/', '\').TrimEnd('\')
if (![string]::Equals($repo, $gitRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Target must be the Git repository root, not a Unity version folder.' }
if ((Read-Git $repo 'branch --show-current').Trim() -ne 'lixiang') { throw 'Select lixiang in GitHub Desktop first.' }
$remote = (Read-Git $repo 'remote get-url origin').Trim()
if ($remote -notmatch '^(https://github\.com/yao8324/project123(\.git)?/?|git@github\.com:yao8324/project123(\.git)?)$') { throw 'Target origin is not yao8324/project123.' }
if ((Read-Git $repo 'status --porcelain --untracked-files=all').Trim()) { throw 'Target repository is not clean. Use a fresh clone; existing work will not be changed.' }
if ((Read-Git $repo 'rev-parse HEAD').Trim() -ne (Read-Git $repo 'rev-parse origin/lixiang').Trim()) { throw 'Target differs from origin/lixiang. Fetch and update a clean clone first.' }
if (!(Test-Path -LiteralPath (Join-Path $repo '1.0.5\Assets'))) { throw 'Expected archived 1.0.5 is missing from target.' }
$target = [IO.Path]::GetFullPath((Join-Path $repo '1.0.6'))
if (Test-Path -LiteralPath $target) { throw '1.0.6 already exists. This script never overwrites a version.' }
$expected = Get-Content -LiteralPath (Join-Path $kit 'RootDocumentBase.json') -Raw | ConvertFrom-Json
foreach ($name in @('README.md','ARCHITECTURE.md')) {
    if ((Normalized-Hash (Join-Path $repo $name)) -ne $expected.$name) { throw "Upstream $name changed. Review and merge the release templates before preparing; no files have been written." }
}

[void][IO.Directory]::CreateDirectory($target)
$hashRows = New-Object 'Collections.Generic.List[object]'
$index = 0
foreach ($relative in $files) {
    $from = Join-Path $sourceRoot $relative
    $to = [IO.Path]::GetFullPath((Join-Path $target $relative))
    if (!$to.StartsWith($target + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Destination escaped 1.0.6' }
    [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($to))
    [IO.File]::Copy($from, $to, $false)
    $sourceHash = (Get-FileHash -LiteralPath $from -Algorithm SHA256).Hash
    $copyHash = (Get-FileHash -LiteralPath $to -Algorithm SHA256).Hash
    if ($sourceHash -ne $copyHash) { throw "Copy hash mismatch: $relative. No Git commit was made." }
    $hashRows.Add([pscustomobject]@{ path=$relative; sha256=$copyHash })
    $index++
    if ($index % 100 -eq 0) { Write-Progress -Activity 'Copying and verifying 1.0.6' -Status "$index / $($files.Count)" -PercentComplete (100*$index/$files.Count) }
}
Write-Progress -Activity 'Copying and verifying 1.0.6' -Completed
foreach ($name in @('README.md','CONTRIBUTING.md')) {
    [IO.File]::Copy((Join-Path $kit $name), (Join-Path $target $name), $true)
}
[IO.File]::Copy((Join-Path $kit 'ReleaseReadiness.md'), (Join-Path $target 'Docs\ReleaseReadiness.md'), $true)
$beforeDocs = Join-Path $target 'Docs\Release106\BeforeRootDocuments'
[void][IO.Directory]::CreateDirectory($beforeDocs)
foreach ($name in @('README.md','ARCHITECTURE.md')) {
    [IO.File]::Copy((Join-Path $repo $name), (Join-Path $beforeDocs $name), $false)
    [IO.File]::Copy((Join-Path $kit ('Repository-' + $name)), (Join-Path $repo $name), $true)
}
$report = [ordered]@{
    generatedAt=(Get-Date -Format 'yyyy-MM-ddTHH:mm:sszzz'); version='1.0.6'; source=$sourceRoot
    sourceFiles=$files.Count; sourceBytes=$bytes; verifiedCopies=$hashRows
    replacedWithReleaseTemplates=@('README.md','CONTRIBUTING.md','Docs/ReleaseReadiness.md')
    rootDocumentsUpdated=@('README.md','ARCHITECTURE.md')
    unityImportOfNewCopy='Not yet run'; committed=$false; pushed=$false
}
Write-Utf8 (Join-Path $target 'Docs\Release106\CopyVerification.json') ($report | ConvertTo-Json -Depth 5)
Write-Host "Ready for Unity import and GitHub Desktop review: $target"
Write-Host 'Only 1.0.6 and the two repository documents were written. Nothing was staged, committed or pushed.'
