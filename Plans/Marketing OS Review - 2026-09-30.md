# Marketing OS review — September 30, 2026

## Objective and review scope

Make the desktop Marketing OS more useful for choosing, executing, and measuring marketing work. Preserve the Averion navy, gold, and white design language. The commercial priority and agent autonomy preference are awaiting user answers. This is a review and implementation brief, not a claim that the redesign has shipped.

Reviewed the live browser interface, version 57 client source, packaged production worker, live hosted database, September 30 daily brief, automation inventory, experiment log, and dated local control-room snapshot. Database reads were read-only. Approval and run-report table reads were bounded samples; lead and research table reads were complete within the returned pages. No publishing, outreach, or business-record changes were performed during the review.

## Findings

| Priority | Observed issue | Why it matters | Proposed correction |
| --- | --- | --- | --- |
| P0 | The live sidebar says Approvals 0 while the overview displays pending social approvals. The count uses approved queued/running actions. | A reviewer cannot trust the inbox badge. | Count items awaiting a human decision separately from work approved for execution. |
| P0 | The overview displays 52 attention items. The client treats pending items without an Approved decision as mismatches. | Routine review work is mixed with failures, hiding urgent issues. | Explicit review, execution, failure, changes-requested, and legacy-state categories; validate transitions and show the reason for each exception. Do not overwrite historical records to make counts look clean. |
| P0 | Existing approval cards are informational, with no card-level review handler in the inspected desktop client. The available action drawer starts one of four canned workflows. | The user can see pending work without opening the exact item to decide on it. | Open the existing approval by ID, show the exact artifact and QA evidence, and persist a decision against that record. Verify existing backend semantics before changing execution behavior. |
| P0 | A single failed endpoint clears approvals, leads, research, and reports in the client. | Partial outages appear to erase the workspace. | Independent source loading, per-source errors, last successful refresh timestamps, and retained data clearly marked stale. |
| P1 | The hosted lead table returned 3 companies last updated September 20; the September 25 local snapshot lists 17. | Hosted and local views represent different coverage. | Reconcile record identity and approved ownership of synchronization; display coverage and source timestamps. Do not infer missing records were deleted. |
| P1 | The hosted research table returned 7 entries dated September 20, while later research exists in the vault. | Recent research is hard to discover from the live feed. | Index dated research consistently; retain source URLs, observation dates, confidence, counterevidence, and recommended next actions. |
| P1 | September 30's daily brief reports repeated QA failures on September 29 and differing execution/reporting paths. | Automated runs consume time without producing usable output. | Group repeated failures by task and cause, expose retry counts, stop repeating unchanged attempts, and provide a clear repair owner and checkpoint. Reconcile the active runner and durable reporting source. |
| P1 | Run reports contain work_done, outputs, and next_action, but the UI largely shows a short summary. | Users cannot inspect what the agent produced or why it matters. | Expandable, searchable run details with artifact links, QA findings, outcome, and next action. Distinguish a completed run from a successful marketing result. |
| P1 | The overview emphasizes queue counts and document counts. | Activity does not establish qualified conversations, demos, pilots, or revenue. | Choose a primary growth goal with the user, then connect campaigns and observed outcomes. Show Unknown / Not connected where measurement is absent. |
| P1 | Content, experiments, and schedule are largely document indexes. | Users must reconstruct a workflow from separate notes. | Content workflow board, experiment scorecards, and an operating calendar connected to the same records and evidence. |
| P2 | Search only filters rendered items in the current view. | Users cannot quickly find a company, report, decision, or draft across the OS. | Global search and a keyboard command menu, with category filters and direct navigation. |
| P2 | Older CovePM naming persists in live historical records and test items appear beside current work. | Historical context distracts from today's decisions. | Preserve historical titles and audit trails; offer archived/test filters and use current Averion Compass naming in new work. |
| P2 | Externally hosted Markdown depends on cross-origin browser access. | Some externally hosted reports cannot render in the current reader. | Show explicit load errors and source fallback; use approved stored report copies where required. Do not imply every third-party host supports embedding. |

## Proposed interactive experience

1. **Today:** a short brief showing what changed, the most valuable next actions, the evidence behind each recommendation, overdue decisions, and measured progress toward the selected goal.
2. **Decision inbox:** open an exact draft, inspect claims and sources, compare revisions, comment, request changes, or approve. Show expected consequences before execution and record the actual outcome afterward.
3. **Campaign workspace:** connect audience, pain, hypothesis, offer, message, channel, assets, owner, checkpoint, and outcome. Let the user navigate from a result back to its campaign and source evidence.
4. **Content studio:** searchable drafts with stages, exact channel previews, reusable approved claims, version history, and a publishing calendar. Provider delivery confirmation is separate from approval.
5. **Account workspace:** company detail, qualification evidence, missing information, last contact, owner, next step, and progression to discovery/demo/pilot. Fit scores remain separate from expressed interest.
6. **Research and experiments:** inspect findings and confidence, compare hypotheses, convert a finding into a linked experiment, set a success measure, and record learning after the checkpoint.
7. **Agent operations:** display last successful run, useful output, QA failures, retries, dependencies, and integration health. Group idle checks separately from productive work.
8. **Ask the workspace:** answer questions from accessible records with dates and source links. Proposed actions must use existing records and supported executors; conversational fluency is not proof of execution.

## Build order and acceptance criteria

### First: trustworthy state and useful decisions

- Correct inbox counts and status classification.
- Open existing approvals and full reports directly.
- Preserve partial data during source failures and show source freshness.
- Reconcile the source checkout and packaged worker before implementation; the root worker inspected during review differs from the production package.
- Acceptance: pending decisions appear in the inbox; approving an existing item updates that exact record; report details expose actual output; a failed source does not blank unrelated sections; all counts can be explained from records.

### Second: connected daily work

- Add the goal-oriented daily brief, global search, campaign records, content stages, and account detail.
- Connect existing research and artifacts before adding speculative integrations.
- Acceptance: the user can move from a finding to a draft or experiment to a decision and inspect the resulting state without manually assembling information from multiple documents.

### Third: measurable automation and learning

- Connect agreed systems, ingest real outcome signals, and apply the user's chosen autonomy boundaries.
- Show workload, failure recurrence, and the effect of completed experiments.
- Acceptance: qualified conversations, demos, pilots, and content results have identifiable sources; unknown metrics are clearly labeled; repeated failed work escalates with an actionable reason.

## User decisions confirmed

- Optimize for qualified leads and pilot customers.
- Agents research and prepare automatically; publishing and outreach require approval.
- The user is new to marketing and needs explanations and access to the supporting information.
- Due dates and deadlines need a shared calendar.
- The experience should feel like an interactive operating workspace.
- Research should recommend the best initial segment.
- The numerical 90-day target is not yet known; keep it unset rather than inventing a target.

## Initial discovery questions

- Primary near-term outcome: qualified leads and pilots, content and audience growth, or market research and product opportunities. A numeric 30- or 90-day target would make prioritization sharper.
- Autonomy: automatically research and prepare work with approval for outreach/publishing; automatically execute routine work under agreed rules; or require approval for every external action.
- Biggest current frustration and required integrations / systems of record.

## Implementation constraints

Terminal work must remain in the background. Use a claimed sandbox if the applicable repository provides a pool; never take another agent's slot. UI changes require a short feature/fix recording before being called Done. No UI code was changed in this review. Michael's existing viewer access and the site's custom audience must be preserved unless the user requests a change.

## Evidence

- Live Marketing OS: https://cove-pm-marketing.daycharles.chatgpt.site/
- Version 57 source commit: 27adb842de25fbb06332e0c207d3a65d15fbb2f6, inspected at .mdviewer/dist/client/index.html.
- Version 57 packaged worker: dist/server/index.js from the saved production archive.
- Live database: DB / approval_actions, lead_records, research_articles, run_reports; read September 30, 2026.
- marketing-vault/Sessions/2026-09-30 - Averion Marketing Daily Brief.md.
- marketing-vault/Schedules/Automation Inventory.md.
- marketing-vault/Experiments/Experiment Log.md.
- marketing-vault/agent-workbench/outputs/CONTROL-ROOM.md, timestamp September 25, 2026.
