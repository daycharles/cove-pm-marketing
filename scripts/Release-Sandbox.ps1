param([Parameter(Mandatory=$true)][string]$SandboxId, [string]$TaskId)
$ErrorActionPreference = 'Stop'
$path = Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..')).Path '.sandbox/registry.json'
$lock = [IO.File]::Open($path, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
try {
    $lock.Position = 0; $bytes = New-Object byte[] $lock.Length; [void]$lock.Read($bytes, 0, $bytes.Length)
    $items = @((ConvertFrom-Json ([Text.Encoding]::UTF8.GetString($bytes))))
    $slot = $items | Where-Object id -eq $SandboxId
    if (-not $slot) { throw "Unknown sandbox: $SandboxId" }
    if ($TaskId -and $slot.taskId -ne $TaskId) { throw "Sandbox $SandboxId is claimed by another task." }
    $slot.status = 'available'; $slot.PSObject.Properties.Remove('taskId'); $slot.PSObject.Properties.Remove('agent'); $slot.PSObject.Properties.Remove('claimedAt')
    $lock.SetLength(0); $lock.Position = 0
    $writer = New-Object IO.StreamWriter($lock); $writer.Write((ConvertTo-Json $items -Depth 5)); $writer.Flush()
    $slot | ConvertTo-Json -Depth 5
} finally { $lock.Dispose() }
