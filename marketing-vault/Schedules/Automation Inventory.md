# Automation Inventory (Transition Record)

Last checked: 2026-10-02

## Legacy ChatGPT/Codex integration

The 8:00 AM and 12:30 PM Averion Software Marketing Agent prompts previously read from and wrote to this vault.
Both tasks were paused on 2026-09-20 to conserve usage and avoid duplicate work. Their saved prompts
were updated for Averion Software, Averion Compass, StellaAI, and the new website screenshots on
2026-09-24; their paused status and cadence were preserved.
Credentials, API keys, restricted personal data, and secrets must not be stored here.

## Scheduled tasks shown in ChatGPT

These are transitional. The replacement is the local workflow in `agent-workbench/`; do not add
new recurring ChatGPT/Codex runs unless there is a specific human-review need.

The Scheduled tasks view showed two marketing tasks; both remain paused:

- **Averion Software Marketing Agent — Morning** — weekdays at 8:00 AM — paused 2026-09-20
- **Averion Software Marketing Agent — Midday** — weekdays at 12:30 PM — paused 2026-09-20

The **Averion Software Marketing Daily Brief** is reported active as the current heartbeat
automation, weekdays at 7:30 AM America/New_York. Its stable internal ID remains unchanged and is
omitted here; it writes dated notes under `Sessions/`. The local review could not verify its live
status because access to the Windows task inventory was denied.

The local Marketing OS Monitor is configured for weekdays every two hours between 8:00 AM and
6:00 PM. Its live enabled state and next trigger could not be verified on 2026-09-30 because Windows
denied task-inventory access. The active checkout's `run-weekly.ps1` posts each cycle's summary,
work completed, output/QA, and next action to the hosted Marketing OS run-report API when the
authorization token is configured; the local token setting was present on 2026-09-30. The OS reads
that API and refreshes its report feed every 45 seconds. This is the live report surface; daily
briefs remain the durable narrative summary in `Sessions/`. An idle cycle is a queue watch, not
fresh research. The latest daily brief reports the Sept. 30 drafts explicitly ran no pre-draft
research, while the Sept. 30 RFP workstream contains new source-backed operator research and a
readiness assessment. A direct API readback could not be confirmed during the Sept. 30 review
because the HTTPS request failed authentication; confirm the latest live row in the Marketing OS
once the hosted connection is available.

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

On 2026-09-23, the approval bridge was corrected so QA-flagged artifacts return to the local retry
queue with the prior QA feedback. They are rerun up to three times and only `ready-for-human-review`
artifacts create visible mobile approval cards. Repeated failures become `blocked` for investigation.

## Notion alignment

Notion's current operating brief references weekday runs at 9:00 AM and 12:00 PM Eastern and a
connected Zoho Mail bridge whose outbound email enters an approval queue. A later Notion funnel
checkpoint renamed the reporting channel from `#notion-updates` to `#marketing-updates`. The local
runner has not connected to Zoho Mail or Slack and does not claim to mirror those schedules; see
[[Research/Notion Knowledge Sync - 2026-09-20]] for the reconciliation record.

## LinkedIn social workflow

Publora is connected to the Averion Software LinkedIn Company Page on its free plan, and Canva is connected for
visual content creation. The active operating design is [[LinkedIn Publishing and Engagement Workflow]].
It uses a prebuilt evergreen queue plus three timely posts per week and two weekday engagement
blocks. The local workbench prepares copy and interaction drafts; each post is shown as an
individual exact-preview approval, and Publora schedules or sends only after that specific post is
approved. The workflow does not auto-publish, auto-comment, auto-react, reshare, or send
unsolicited messages.

The scheduler also offers general-purpose recommended automations, but none was active as part of the
Averion Software marketing workflow.

## Maintenance

When a marketing task is created or changed, update both this note and [[Marketing Schedule]].
Record its exact name, cadence, status, task link or ID, last run, and next run.
