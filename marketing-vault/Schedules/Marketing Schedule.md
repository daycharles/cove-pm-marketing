# Marketing Schedule

Use this page as the human-readable index of recurring work. The existing ChatGPT/Codex schedules
are transitional; the local workbench will become the primary automation path after evaluation.

Vault root: `C:\Users\cd104535\Documents\Codex\cove-pm-marketing\marketing-vault`

| Schedule / session | Cadence | Purpose | Status | Last run | Next run | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| CovePM Marketing Agent — morning session | Weekdays at 8:00 AM | Transitional ChatGPT/Codex session | Active / transition | — | Next run shown in scheduler | Retire after local workbench evaluation |
| CovePM Marketing Agent — midday session | Weekdays at 12:30 PM | Transitional ChatGPT/Codex session | Active / transition | — | Next run shown in scheduler | Retire after local workbench evaluation |
| CovePM Marketing Daily Brief | Weekdays at 7:30 AM | Transitional session summary | Active / transition | — | Next run shown in scheduler | Keep only if it remains useful after local reporting |
| Local marketing workbench | Weekly or on demand | Scan queue -> local model -> QA -> approval -> control-room snapshot | Active local MVP | 2026-09-20 | Next Task Scheduler run | `agent-workbench/run-weekly.ps1`; processes one queued task per run |

## Where to see the next work

Open either **CovePM Marketing Agent** entry in the Scheduled tasks screen. Its detail view is the
source of truth for the saved prompt, next run, recent results, and schedule controls. The vault
tracks the cadence here, while each session's output belongs in `Sessions/`.

The existing scheduled tasks write dated output to `Sessions/`. The local workbench writes drafts and
`CONTROL-ROOM.md` to `agent-workbench/outputs/` and technical run, queue, action, and approval metadata
to its local SQLite database.

## Editing a schedule

Open the corresponding scheduled task in ChatGPT/Codex and edit its cadence, prompt, or pause
state. After changing it, update the row above and add a note in the related session log.
