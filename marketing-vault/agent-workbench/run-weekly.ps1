$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
$python = ".\.venv\Scripts\python.exe"
& $python .\control_room.py dashboard
& $python .\control_room.py weekly-review
& $python .\control_room.py run-next
& $python .\control_room.py dashboard

$reportConfig = Join-Path $env:LOCALAPPDATA 'CovePM\marketing-os-sites.env'
if (Test-Path -LiteralPath $reportConfig) {
    $tokenLine = Get-Content -LiteralPath $reportConfig | Where-Object { $_ -like 'OAI_SITES_AUTHORIZATION_TOKEN=*' } | Select-Object -First 1
    if ($tokenLine) {
        $token = $tokenLine.Substring('OAI_SITES_AUTHORIZATION_TOKEN='.Length).Trim()
        $now = Get-Date
        $payload = @{
            run_key = "scheduled-$($now.ToString('yyyyMMdd-HHmm'))"
            title = "Scheduled marketing run · $($now.ToString('h:mm tt'))"
            summary = 'The local marketing workbench completed its scheduled operating cycle.'
            work_done = 'Scanned the task queue, refreshed the control-room snapshot, processed the next queued task, and refreshed the weekly review checkpoint.'
            outputs = 'Updated the local control-room and weekly-review artifacts; approval-gated work remains queued for review.'
            next_action = 'Review the mobile approval inbox and Research feed during the next check window.'
            run_date = $now.ToString('yyyy-MM-dd')
        } | ConvertTo-Json -Compress
        try {
            Invoke-RestMethod -Uri 'https://cove-pm-marketing.daycharles.chatgpt.site/api/run-reports' -Method Post -Headers @{ 'OAI-Sites-Authorization' = "Bearer $token" } -ContentType 'application/json' -Body $payload | Out-Null
        } catch {
            Write-Warning "Could not sync the run report to the mobile feed: $($_.Exception.Message)"
        }
    }
}
