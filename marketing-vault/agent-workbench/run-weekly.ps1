$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
$python = ".\.venv\Scripts\python.exe"
& $python .\social_workflow.py ensure | Out-Null
& $python .\control_room.py dashboard
& $python .\control_room.py weekly-review
$runResult = & $python .\control_room.py run-next | ConvertFrom-Json
& $python .\control_room.py dashboard

$reportConfig = Join-Path $env:LOCALAPPDATA 'CovePM\marketing-os-sites.env'
if (Test-Path -LiteralPath $reportConfig) {
    $tokenLine = Get-Content -LiteralPath $reportConfig | Where-Object { $_ -like 'OAI_SITES_AUTHORIZATION_TOKEN=*' } | Select-Object -First 1
    if ($tokenLine) {
        $token = $tokenLine.Substring('OAI_SITES_AUTHORIZATION_TOKEN='.Length).Trim()
        $now = Get-Date
        $pendingJson = @'
import json, sqlite3
db = r"C:\Users\cd104535\Documents\Codex\cove-pm-marketing\marketing-vault\agent-workbench\data\runs.sqlite3"
with sqlite3.connect(db) as con:
    con.row_factory = sqlite3.Row
    rows = con.execute("""
        SELECT a.approval_id, a.run_id, a.requested_action, a.task_id,
               w.objective, w.last_output_path
        FROM approval_queue a
        LEFT JOIN work_queue w ON w.task_id = a.task_id
        WHERE a.status = 'pending'
        ORDER BY a.requested_at
    """).fetchall()
print(json.dumps([dict(row) for row in rows]))
'@ | & $python -
        $pendingApprovals = @($pendingJson | ConvertFrom-Json)
        try {
            $remoteApprovals = @(Invoke-RestMethod -Uri 'https://cove-pm-marketing.daycharles.chatgpt.site/api/approvals' -Headers @{ 'OAI-Sites-Authorization' = "Bearer $token" }).approvals
            foreach ($approval in $pendingApprovals) {
                $marker = "Local approval id: $($approval.approval_id); run: $($approval.run_id)"
                if ($remoteApprovals | Where-Object {
                    $_.note -like "*$marker*" -and $_.status -in @('pending', 'queued', 'approved')
                }) { continue }
                $artifactText = ''
                if ($approval.last_output_path -and (Test-Path -LiteralPath $approval.last_output_path)) {
                    $artifactText = Get-Content -Raw -LiteralPath $approval.last_output_path
                }
                $approvalAction = if ($approval.task_id -like '*linkedin-*') { 'social-publish' } else { 'workbench' }
                $approvalArtifacts = @($artifactText)
                if ($approval.task_id -like '*linkedin-evergreen-queue*') {
                    $matches = [regex]::Matches($artifactText, '(?ms)^### Post \d{4}-\d{2}-\d{2}.*?(?=^### Post |\z)')
                    if ($matches.Count -gt 0) { $approvalArtifacts = @($matches | ForEach-Object { $_.Value.Trim() }) }
                }
                foreach ($approvalArtifact in $approvalArtifacts) {
                    if ($approvalArtifact.Length -gt 3800) {
                        $approvalArtifact = $approvalArtifact.Substring(0, 3800) + "`n`n[Artifact excerpt truncated; open the vault output for the full result.]"
                    }
                    $approvalTask = if ($approvalAction -eq 'social-publish') { "LinkedIn post · $($approval.objective ?? $approval.task_id)" } else { "Approval #$($approval.approval_id) · $($approval.objective ?? $approval.task_id)" }
                    if ($approvalArtifact -match '(?m)^### Post (?<postDate>\d{4}-\d{2}-\d{2})') { $approvalTask = "LinkedIn post · $($Matches.postDate) · evergreen" }
                    $approvalPayload = @{
                        task = $approvalTask
                        action = $approvalAction
                        decision = 'Pending review'
                        note = "$marker`n$($approval.requested_action)`nExact post preview shown; approve each post separately before Publora scheduling."
                        recipient = ''
                        subject = ''
                        content = $approvalArtifact
                    } | ConvertTo-Json -Compress
                    Invoke-RestMethod -Uri 'https://cove-pm-marketing.daycharles.chatgpt.site/api/approvals' -Method Post -Headers @{ 'OAI-Sites-Authorization' = "Bearer $token" } -ContentType 'application/json' -Body $approvalPayload | Out-Null
                }
            }
        } catch {
            Write-Warning "Could not sync pending approvals to the mobile inbox: $($_.Exception.Message)"
        }
        $controlRoomPath = Join-Path $PSScriptRoot 'outputs\CONTROL-ROOM.md'
        $weeklyReviewPath = Join-Path $PSScriptRoot 'outputs\WEEKLY-REVIEW.md'
        $artifactPath = $null
        $artifact = ''
        if ($runResult.stdout) {
            try {
                $runDetails = $runResult.stdout | ConvertFrom-Json
                $artifactPath = $runDetails.output
            } catch {
                $artifactPath = $null
            }
        }
        if ($artifactPath -and (Test-Path -LiteralPath $artifactPath)) {
            $artifact = Get-Content -Raw -LiteralPath $artifactPath
        }
        if (-not $artifact -and $runResult.status -eq 'idle') {
            $artifact = 'No queued task was ready during this run.'
        }
        $artifactExcerpt = $artifact
        if ($artifactExcerpt.Length -gt 3600) {
            $artifactExcerpt = $artifactExcerpt.Substring(0, 3600) + "`n`n[Artifact excerpt truncated; open the vault output for the full result.]"
        }
        $queueSnapshot = if (Test-Path -LiteralPath $controlRoomPath) { Get-Content -Raw -LiteralPath $controlRoomPath } else { '' }
        $reviewSnapshot = if (Test-Path -LiteralPath $weeklyReviewPath) { Get-Content -Raw -LiteralPath $weeklyReviewPath } else { '' }
        $queueLine = ($queueSnapshot -split "`n" | Where-Object { $_ -like '> Queue:*' } | Select-Object -First 1)
        $approvalLines = ($queueSnapshot -split "`n" | Where-Object { $_ -match '^\- \*\*#' } | Select-Object -First 5) -join "`n"
        $reviewHeadline = ($reviewSnapshot -split "`n" | Where-Object { $_ -like '| Work queue |*' -or $_ -like '| Lead queue |*' -or $_ -like '| Pending approvals |*' } | Select-Object -First 3) -join "`n"
        $runTimeLabel = $now.ToString('h:mm tt')
        $taskTitle = if ($runResult.task) { $runResult.task } else { "Queue watch · $runTimeLabel" }
        $runStatus = if ($runResult.status) { $runResult.status } else { 'unknown' }
        $qaText = if ($runDetails -and $runDetails.qa -and $runDetails.qa.Count) { ($runDetails.qa -join '; ') } else { 'No deterministic QA warnings reported.' }
        $summary = if ($runStatus -eq 'idle') { "No queued task was ready at $runTimeLabel. The team monitored the current queue and approval state." } else { "Processed: $taskTitle. Run status: $runStatus." }
        $workDone = if ($runStatus -eq 'idle') { "Queue watch at $runTimeLabel.`nNo new task was processed; the runner checked queue health, pending approvals, and lead state." } else { "Task result:`n$artifactExcerpt" }
        $outputs = "Artifact path: $artifactPath`nQA: $qaText`n$queueLine`n$reviewHeadline`nPending approvals:`n$approvalLines"
        $nextAction = if ($runStatus -eq 'idle') { 'Review the current queue and pending approvals; no new task result was produced.' } elseif ($qaText -ne 'No deterministic QA warnings reported.') { 'Review the generated artifact and resolve the listed QA warnings before publishing, outreach, pricing, or commitments.' } else { 'Review the generated artifact and approval queue before any external action.' }
        $payload = @{
            run_key = "scheduled-$($now.ToString('yyyyMMdd-HHmm'))"
            title = "Marketing result · $taskTitle"
            summary = $summary
            work_done = $workDone
            outputs = $outputs
            next_action = $nextAction
            run_date = $now.ToString('yyyy-MM-dd')
        } | ConvertTo-Json -Compress
        try {
            Invoke-RestMethod -Uri 'https://cove-pm-marketing.daycharles.chatgpt.site/api/run-reports' -Method Post -Headers @{ 'OAI-Sites-Authorization' = "Bearer $token" } -ContentType 'application/json' -Body $payload | Out-Null
        } catch {
            Write-Warning "Could not sync the run report to the mobile feed: $($_.Exception.Message)"
        }
    }
}
