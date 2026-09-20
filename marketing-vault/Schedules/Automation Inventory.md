# Automation Inventory (Transition Record)

Last checked: 2026-09-18

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

The Scheduled tasks view also shows a separate general-purpose automation:

- **Weekday Morning Brief** — weekdays at 7:30 AM America/New_York

That brief is not part of the CovePM marketing workflow.

## Maintenance

When a marketing task is created or changed, update both this note and [[Marketing Schedule]].
Record its exact name, cadence, status, task link or ID, last run, and next run.
