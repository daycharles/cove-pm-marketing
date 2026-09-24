# Marketing Schedule

Use this page as the human-readable index of recurring work. The existing ChatGPT/Codex schedules
are transitional; the local workbench will become the primary automation path after evaluation.

Vault root: `C:\Users\cd104535\Documents\Codex\cove-pm-marketing\marketing-vault`

| Schedule / session | Cadence | Purpose | Status | Last run | Next run | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| CovePM Marketing Agent — morning session | Weekdays at 8:00 AM | Transitional ChatGPT/Codex session | Paused 2026-09-20 | — | None while paused | Preserved for rollback; local Ollama workbench is replacement path |
| CovePM Marketing Agent — midday session | Weekdays at 12:30 PM | Transitional ChatGPT/Codex session | Paused 2026-09-20 | — | None while paused | Preserved for rollback; local Ollama workbench is replacement path |
| CovePM Marketing Daily Brief | Weekdays at 7:30 AM America/New_York | Summarize the previous day's marketing-agent sessions | Active | 2026-09-23 | Next weekday run | Automation ID: `covepm-marketing-daily-brief`; output in `Sessions/` |
| Local marketing workbench | Every 2 hours on weekdays, 8:00 AM–6:00 PM | Scan queue -> local model -> QA -> revision loop -> human approval -> control-room snapshot -> mobile run report | Active local MVP | 2026-09-23 | Next Task Scheduler run | `agent-workbench/run-weekly.ps1`; retries QA-flagged drafts with feedback up to three times, then exposes only QA-passed artifacts for human approval |
| LinkedIn publishing and engagement workflow | Weekdays at 8:30 AM and 2:30 PM; evergreen queue at 4:00 PM; engagement at 10:30 AM and 4:00 PM | Public social listening -> prepare timely content, evergreen queue, and engagement batches for Publora | Active, approval-gated | 2026-09-20 | Next weekday run | Each evergreen post is a separate mobile approval; only approved Draft text is placed on the Publora calendar; see [[LinkedIn Publishing and Engagement Workflow]] |

## Where to see the next work

Open either **CovePM Marketing Agent** entry in the Scheduled tasks screen. Its detail view is the
source of truth for the saved prompt, next run, recent results, and schedule controls. The vault
tracks the cadence here, while each session's output belongs in `Sessions/`.

The legacy scheduled tasks are paused and write no new output. The local workbench writes drafts and
`CONTROL-ROOM.md` to `agent-workbench/outputs/` and technical run, queue, action, and approval metadata
to its local SQLite database.

## Editing a schedule

Open the corresponding scheduled task in ChatGPT/Codex and edit its cadence, prompt, or pause
state. After changing it, update the row above and add a note in the related session log.
