# Automation Inventory (Transition Record)

Last checked: 2026-09-20

## Legacy ChatGPT/Codex integration

The 8:00 AM and 12:30 PM CovePM Marketing Agent prompts previously read from and wrote to this vault.
Both tasks were paused on 2026-09-20 to conserve Codex usage and avoid duplicate work. Their history
and prompts remain preserved for rollback. No active 7:30 AM brief was present in the scheduler view.
Credentials, API keys, restricted personal data, and secrets must not be stored here.

## Scheduled tasks shown in ChatGPT

These are transitional. The replacement is the local workflow in `agent-workbench/`; do not add
new recurring ChatGPT/Codex runs unless there is a specific human-review need.

The Scheduled tasks view showed two CovePM marketing tasks; both are now paused:

- **CovePM Marketing Agent** — weekdays at 8:00 AM — paused 2026-09-20
- **CovePM Marketing Agent** — weekdays at 12:30 PM — paused 2026-09-20

No active CovePM Marketing Daily Brief was present in the scheduler view on 2026-09-20.

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

## LinkedIn social workflow

Publora is connected to the CovePM LinkedIn Company Page on its free plan, and Canva is connected for
visual content creation. The active operating design is [[LinkedIn Publishing and Engagement Workflow]].
It targets two weekday posts per day plus two weekday engagement blocks. The local workbench prepares
copy and interaction drafts; Publora schedules or sends only after human approval. The workflow does
not auto-publish, auto-comment, auto-react, reshare, or send unsolicited messages.

The scheduler also offers general-purpose recommended automations, but none was active as part of the
CovePM marketing workflow.

## Maintenance

When a marketing task is created or changed, update both this note and [[Marketing Schedule]].
Record its exact name, cadence, status, task link or ID, last run, and next run.
