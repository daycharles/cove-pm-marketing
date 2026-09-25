# Marketing Schedule

Use this page as the human-readable index of recurring work. The existing ChatGPT/Codex schedules
are transitional; the local workbench will become the primary automation path after evaluation.

Vault root: `C:\Users\cd104535\Documents\Codex\cove-pm-marketing\marketing-vault`

| Schedule / session | Cadence | Purpose | Status | Last run | Next run | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| Averion Software Marketing Agent — Morning | Weekdays at 8:00 AM | Transitional ChatGPT/Codex session | Paused 2026-09-20 | — | None while paused | Branding prompt updated 2026-09-24; local Ollama workbench remains the replacement path |
| Averion Software Marketing Agent — Midday | Weekdays at 12:30 PM | Transitional ChatGPT/Codex session | Paused 2026-09-20 | — | None while paused | Branding prompt updated 2026-09-24; local Ollama workbench remains the replacement path |
| Averion Software Marketing Daily Brief | Weekdays at 7:30 AM America/New_York | Summarize previous-day workbench and marketing-agent sessions | Active | 2026-09-23 | Next weekday run | Stable internal ID: `covepm-marketing-daily-brief`; output in `Sessions/` |
| Averion Software Marketing OS Monitor | Every 2 hours on weekdays, 8:00 AM–6:00 PM | Scan queue -> local model -> QA -> revision loop -> human approval -> control-room snapshot -> mobile run report | Active local MVP | 2026-09-24 | Next Task Scheduler run | `agent-workbench/run-weekly.ps1`; marketing prompts now use Averion Compass and current screenshot references |
| Averion Software LinkedIn publishing and engagement workflow | Weekdays at 8:30 AM and 2:30 PM; evergreen queue at 4:00 PM; engagement at 10:30 AM and 4:00 PM | Public social listening -> prepare distinct timely content and uncovered evergreen slots; check the 90-day post ledger before approval | Active, approval-gated | 2026-09-24 | Next weekday run | Use Averion Software page; use Averion Compass in property-management posts and keep StellaAI separate; see [[LinkedIn Publishing and Engagement Workflow]] |

## Where to see the next work

Open either **Averion Software Marketing Agent** entry in the Scheduled tasks screen. Its detail view is the
source of truth for the saved prompt, next run, recent results, and schedule controls. The vault
tracks the cadence here, while each session's output belongs in `Sessions/`.

The legacy scheduled tasks are paused and write no new output. The local workbench writes drafts and
`CONTROL-ROOM.md` to `agent-workbench/outputs/` and technical run, queue, action, and approval metadata
to its local SQLite database.

## Editing a schedule

Open the corresponding scheduled task in ChatGPT/Codex and edit its cadence, prompt, or pause
state. After changing it, update the row above and add a note in the related session log.
