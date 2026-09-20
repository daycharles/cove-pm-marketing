# Automation Inventory (Transition Record)

Last checked: 2026-09-20

## Legacy ChatGPT/Codex integration

The 8:00 AM and 12:30 PM CovePM Marketing Agent prompts currently read from and write to this vault.
Each run saves a dated note under `Sessions/` and updates the dashboard/index when priorities or
artifacts change. The 7:30 AM brief reads those notes and saves its own dated summary under
`Sessions/`. Credentials, API keys, restricted personal data, and secrets must not be stored here.

## Scheduled tasks shown in ChatGPT

These are transitional. The replacement is the local workflow in `agent-workbench/`; do not add
new recurring ChatGPT/Codex runs unless there is a specific human-review need.

The marketing schedule currently includes two active tasks, based on the Scheduled tasks view:

- **CovePM Marketing Agent** — weekdays at 8:00 AM
- **CovePM Marketing Agent** — weekdays at 12:30 PM

The following brief automation is now active:

- **CovePM Marketing Daily Brief** — weekdays at 7:30 AM America/New_York
- Automation ID: `covepm-marketing-daily-brief`
- Scope: summarize the previous day's 8:00 AM and 12:30 PM marketing-agent sessions

The exact work planned for each session is defined by that task's saved prompt. Open the task entry
to review or modify the prompt, cadence, pause state, and recent runs.

## Local workbench loop

The local workbench is now the first automation path for routine marketing work. Its scheduled
entry point is `agent-workbench/run-weekly.ps1`, which:

1. scans the Markdown inbox and refreshes the local control-room snapshot;
2. processes at most one queued task through local Ollama;
3. records the run and approval request in SQLite; and
4. refreshes `agent-workbench/outputs/CONTROL-ROOM.md`; and
5. writes `agent-workbench/outputs/WEEKLY-REVIEW.md` with the current lead and approval checkpoint.

It remains approval-gated: it does not send outreach, publish social content, edit the website, or
make pricing, legal, product, or outcome commitments.

## Notion alignment

Notion's current operating brief references weekday runs at 9:00 AM and 12:00 PM Eastern and a
connected Zoho Mail bridge whose outbound email enters an approval queue. A later Notion funnel
checkpoint renamed the reporting channel from `#notion-updates` to `#marketing-updates`. The local
runner has not connected to Zoho Mail or Slack and does not claim to mirror those schedules; see
[[Research/Notion Knowledge Sync - 2026-09-20]] for the reconciliation record.

The Scheduled tasks view also shows a separate general-purpose automation:

- **Weekday Morning Brief** — weekdays at 7:30 AM America/New_York

That brief is not part of the CovePM marketing workflow.

## Maintenance

When a marketing task is created or changed, update both this note and [[Marketing Schedule]].
Record its exact name, cadence, status, task link or ID, last run, and next run.
