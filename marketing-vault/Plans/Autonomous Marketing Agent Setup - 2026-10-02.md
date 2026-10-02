# Autonomous Marketing Agent Setup — 2026-10-02

**Status:** Initial operating setup; local research and drafting authorized.  
**Owner:** Averion Software marketing agent.  
**Commercial goal:** Create qualified Compass discovery opportunities without making unsupported product or outcome claims.

## Decision and scope

Use the existing single-agent local workbench and Obsidian vault. The immediate focus is private multifamily operator discovery and RFP readiness, as set in [[Private Multifamily RFP Workstream - 2026-09-30]]. Treat the target segment and inspection-to-unit-turn message as hypotheses to validate. Social publishing remains deferred. StellaAI is outside this campaign.

Current evidence supports the public description that Averion Compass helps teams record inspection findings, assign follow-up work, and track a unit through review and sign-off. The current release and RFP evidence gaps prevent broader capability, security, integration, implementation, or outcome claims. Use the approved current calls to action, **Talk with us** and **How it works**.

## Implementation phases

| Phase | Work | Exit evidence |
| --- | --- | --- |
| 0. Establish control | Use this plan, `AGENTS.md`, current brand/positioning, latest daily brief, and existing approval boundaries. Inventory the active scheduled checkout before changing automation. | A run can name its source of truth, output path, reviewer, and approval state. |
| 1. Produce one decision-ready packet | Reconcile the differing first-wave operator rankings; verify official operator or technology-evaluation routes; update the product-claim and RFP gap list. | One dated research artifact with source URLs, access dates, recommendation, gaps, and no contact represented as made. |
| 2. Prepare the experiment | Draft a buyer-specific discovery question and exact outreach artifact for a selected, evidence-qualified company. Define the qualified-conversation metric, baseline, and review date. | Draft and source rationale in the approval queue; no message sent. |
| 3. Measure and expand | Record human review time, QA defects, approved/sent status, replies, qualified conversations, demos, and pilot readiness when observed. | Weekly report separates activity from outcomes; next channel or connector is chosen from the observed bottleneck. |

## First autonomous run

1. Read [[Sessions/2026-10-02 - Averion Marketing Daily Brief]], [[Research/Private Multifamily RFP and Exposure Landscape - 2026-09-30]], [[Private Multifamily RFP Exposure and Discovery Plan - 2026-09-30]], and [[Research/Private Operator RFP Readiness Audit - 2026-09-30]].
2. Recheck the candidate ranking and official routes against current primary sources. Record access dates and label any unverified route as a gap. Do not infer an open software RFP from a general vendor form.
3. Produce a dated note in `Research/` with target, buyer, problem, evidence, proposed message, channel, next action, metric, owner, checkpoint, and risks.
4. If the evidence supports a specific company, create an exact outreach draft with personalization rationale and opt-out path for review. Stop before contact or form submission.
5. Log the run in `Sessions/`, update [[Dashboard]] and [[Experiments/Experiment Log]] when a hypothesis or result changes, and surface only concrete approvals.

## Cadence and state

- **Weekdays:** inspect queue, approval state, stale evidence, and due work without requiring an LLM call. Work one bounded research or draft item when the evidence packet is ready.
- **Weekly:** render the local review and compare output quality, human effort, qualified conversations, and recorded outcomes. Missing denominators stay missing.
- **System of record:** vault notes for decisions and evidence; workbench SQLite for local run metadata. The Windows schedule currently targets a separate checkout. Reconcile paths and data before changing its task or copying state.
- **External action:** use the existing exact-artifact approval, suppression/opt-out, idempotency, and audit controls. Do not use a local draft or approval-card submission as authority to send or publish.

## Setup verification

- Confirm the target checkout and current working tree before edits or scheduled execution.
- Read `agent-workbench/README.md`; its archived example task path is stale. Do not reactivate archived briefs.
- Use read-only `python control_room.py status` to inspect this checkout. `dashboard` and `weekly-review` write reports; `run-next` executes a queued task.
- Validate new work with source and claim review, then check the relevant workbench tests. Do not run live provider adapters as smoke tests.
- If a UI changes, capture a short video from a claimed sandbox and retain it as evidence before Done. This setup changes no UI.

## Open dependencies

- Product and engineering must confirm release behavior before expanding public or RFP claims.
- Security, implementation, legal, and leadership owners must validate their RFP sections and commercial terms.
- The scheduled checkout and durable vault need reconciliation before this plan can own automated execution. No scheduler change is part of this initial setup.
