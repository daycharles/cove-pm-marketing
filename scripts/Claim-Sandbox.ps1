param([Parameter(Mandatory=$true)][string]$TaskId, [string]$Agent = $env:USERNAME)
$ErrorActionPreference = 'Stop'
$path = Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..')).Path '.sandbox/registry.json'
$lock = [IO.File]::Open($path, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
try {
    $lock.Position = 0; $bytes = New-Object byte[] $lock.Length; [void]$lock.Read($bytes, 0, $bytes.Length)
    $items = @((ConvertFrom-Json ([Text.Encoding]::UTF8.GetString($bytes))))
    $slot = $items | Where-Object status -eq 'available' | Select-Object -First 1
    if (-not $slot) { throw 'No sandbox is available. Wait and retry; do not share or steal a sandbox.' }
    $slot.status = 'in-use'
    $slot | Add-Member -NotePropertyName taskId -NotePropertyValue $TaskId
    $slot | Add-Member -NotePropertyName agent -NotePropertyValue $Agent
    $slot | Add-Member -NotePropertyName claimedAt -NotePropertyValue ([DateTime]::UtcNow.ToString('o'))
    $lock.SetLength(0); $lock.Position = 0
    $writer = New-Object IO.StreamWriter($lock); $writer.Write((ConvertTo-Json $items -Depth 5)); $writer.Flush()
    $slot | ConvertTo-Json -Depth 5
} finally { $lock.Dispose() }
