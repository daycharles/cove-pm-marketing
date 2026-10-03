# Marketing Schedule

> **2026-10-02: superseded by [[Plans/Hermes Marketing Plan - 2026-10-02]].** No recurring marketing job is part of the new Hermes plan. The Windows `\CovePM Marketing OS Monitor` task was deleted and verified absent; the active checkout's hosted run-report and mobile approval bridge were removed. Hermes cron has no jobs. The ChatGPT Morning/Midday tasks were last documented paused, and the ChatGPT Daily Brief was reported active; their live status and removal cannot be verified without account access. The historical table below is retained for audit, **not a current schedule or instruction to run**.

Historical schedule inventory follows. No entries below are part of the current operating plan.

Vault root: `C:\Users\cd104535\Documents\Codex\cove-pm-marketing\marketing-vault`

| Schedule / session | Cadence | Purpose | Status | Last run | Next run | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| Averion Software Marketing Agent — Morning | Weekdays at 8:00 AM | Transitional ChatGPT/Codex session | Paused 2026-09-20 | — | None while paused | Branding prompt updated 2026-09-24; local Ollama workbench remains the replacement path |
| Averion Software Marketing Agent — Midday | Weekdays at 12:30 PM | Transitional ChatGPT/Codex session | Paused 2026-09-20 | — | None while paused | Branding prompt updated 2026-09-24; local Ollama workbench remains the replacement path |
| Averion Software Marketing Daily Brief | Weekdays at 7:30 AM America/New_York | Summarize previous-day workbench and marketing-agent sessions | Active | 2026-10-02 | Next weekday run | Durable output in `Sessions/`; Windows task currently runs the renamed `averion-software\products\cove-pm` checkout and needs reconciliation |
| Averion Software Marketing OS Monitor | Configured every 2 hours on weekdays, 8:00 AM–6:00 PM | Scan queue -> local model -> QA -> revision loop -> human approval -> control-room snapshot -> shared Marketing OS run report | Enabled; last task run 2026-10-01 6:00 PM | 2026-10-01 | 2026-10-02 8:00 AM | Scheduler points to `C:\Users\cd104535\Documents\Codex\averion-software\products\cove-pm\marketing-vault\agent-workbench\run-weekly.ps1`; durable vault remains the reporting copy. See [[Marketing Calendar]]. |
| Averion Software LinkedIn publishing and engagement workflow | Weekdays at 8:30 AM and 2:30 PM; evergreen queue at 4:00 PM; engagement at 10:30 AM and 4:00 PM | Public social listening -> prepare distinct timely content and uncovered evergreen slots; check the 90-day post ledger before approval | Active, approval-gated | 2026-09-24 | Next weekday run | Use Averion Software page; use Averion Compass in property-management posts and keep StellaAI separate; see [[LinkedIn Publishing and Engagement Workflow]] |

## Historical schedule notes (not active instructions)

Open either **Averion Software Marketing Agent** entry in the Scheduled tasks screen. Its detail view is the
source of truth for the saved prompt, next run, recent results, and schedule controls. The vault
tracks the cadence here, while each session's output belongs in `Sessions/`.

The legacy scheduled tasks are paused and write no new output. The local workbench writes drafts and
`CONTROL-ROOM.md` to `agent-workbench/outputs/` and technical run, queue, action, and approval metadata
to its local SQLite database.

## Editing a schedule

Open the corresponding scheduled task in ChatGPT/Codex and edit its cadence, prompt, or pause
state. After changing it, update the row above and add a note in the related session log.

## October 1 queue reset

The monitor remains enabled. The active scheduled checkout now uses the fresh Averion Compass template outside its scanned inbox, and dated briefs use `linkedin-compass-content-YYYYMMDD.md`. Legacy inbox briefs and approval rows are retired. Marketing OS rejects old local approval IDs through 27 and CovePM titles/public copy. See `Sessions/2026-10-01 - Fresh Compass Queue Reset.md` for the verified source repair and hosted cleanup.
One-time override, October 1, 2026: next monitor run moved from 10:00 AM to 9:00 AM ET. Today's 10:00 AM run is skipped; noon and later runs remain on schedule. The recurring 10:00 AM trigger resumes October 2. Task Scheduler verified NextRunTime = 2026-10-01 09:00:00.
