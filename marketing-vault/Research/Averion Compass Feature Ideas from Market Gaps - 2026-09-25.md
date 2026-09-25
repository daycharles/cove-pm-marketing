# Averion Compass Feature Ideas from Market Gaps

**Prepared:** September 25, 2026  
**Purpose:** Convert the competitor-review and market-gap research into product ideas for Averion Compass.

> Status labels matter: **Documented gap** means the current product knowledge base explicitly says the capability is not evidenced. **Needs product audit** means the reviewed materials do not confirm it; check the current app before calling it absent. These are candidate ideas, not commitments or a roadmap.

## Current product baseline

The product documentation describes reusable, versioned inspection templates; move-in, move-out, and routine inspections; checklists; findings with severity and resolution status; optional photo/file evidence; inspection approval; unit-turn task generation from approved move-out inspections; blocker-aware task status and ready-state transition; and links from make-ready tasks to work items.

Compass also has broader work orchestration, including queues, status, priority, assignment, scheduling, notes, bulk actions, timelines, attachments, and messaging. The product has reporting and CSV export. Those existing capabilities are not listed below as new ideas.

Sources: [Capabilities and limitations](../../../averion-software/docs/marketing/products/cove-pm/capabilities.md), [Inspection-to-make-ready workflow](../../../averion-software/docs/marketing/products/cove-pm/inspection-to-make-ready.md), and the [market-gap report](Averion%20Compass%20Market%20Gaps%20and%20Competitive%20Review%20-%202026-09-25.md).

## Highest-value ideas to investigate first

### 1. Finding-to-work conversion with owner and due date

- **Gap status:** Needs product audit. Findings can generate make-ready tasks, and those tasks can link to work items where a work record exists. A complete, direct finding-to-work-order workflow is not confirmed.
- **Idea:** From a finding, create or link a work item in one step; carry over property, unit, area, description, severity, photos, and target date; assign an employee or vendor; return the work status to the finding and turn.
- **Why it could matter:** This is the core handoff customers describe between inspection and repair. It is a sharper position than a generic inspection or turn board.
- **Questions to validate:** Do teams create one work order per finding, combine findings by trade, or review them first? Which fields must transfer? Who approves the work?
- **Possible measure:** Median time from approved finding to accepted work assignment; share of actionable findings with an owner and due date.

### 2. Finding closeout with verification and evidence

- **Gap status:** Needs product audit. Findings have resolution status and optional evidence; overall durable closeout and verification rules are not confirmed.
- **Idea:** Define a clear closeout sequence: work reported complete, evidence attached, supervisor or inspector verifies, and the finding is closed or reopened with a reason.
- **Why it could matter:** “Done” can mean a technician updated a task, a supervisor confirmed the fix, or an inspector passed the item. Making that distinction visible could reduce repeated checking.
- **Questions to validate:** Which role is allowed to verify? Is a photo required for certain items? What happens when the repair fails reinspection?
- **Possible measure:** Share of findings closed with the required evidence; reopened finding rate; time from reported completion to verification.

### 3. Configurable follow-up rules and escalation

- **Gap status:** Documented gap. Current notes do not evidence automatic resident/vendor notifications for every transition, SLA measurement, or automated escalation.
- **Idea:** Let an operator set optional due-date rules and reminders by finding severity or work type; notify the responsible person when work is assigned, due soon, overdue, completed, or returned for correction; escalate to a selected supervisor.
- **Why it could matter:** Reviewers mention workflow friction and field follow-up. Operators may value fewer manual status checks if rules stay clear and configurable.
- **Questions to validate:** Which events actually need a notification? Which channels are acceptable? What response time is required, and who owns escalation?
- **Possible measure:** Overdue work age, manual follow-ups per finding, and notification acknowledgment time.
- **Boundary:** Basic messaging exists; this idea is about configurable event-based automation, not merely sending messages.

### 4. Customer-owned inspection templates and checklist governance

- **Gap status:** Partial. Reusable, versioned templates are documented; customer-specific checklist behavior and regulatory mappings are explicitly not evidenced.
- **Idea:** Support customer-managed checklist versions, required fields, scoring/priority rules, and change history. Make it easy to copy a template and see which checklist version was used on a completed inspection.
- **Why it could matter:** Review signals include requests for flexible inspection types and reusable move-in/move-out checklists. The product already has a template foundation, so the question is where customers need more control.
- **Questions to validate:** What varies by property, program, and inspection type? Who may edit a template? Should changes affect an inspection already in progress?
- **Possible measure:** Setup time for a new property or inspection type; percentage of inspections using the intended template version.
- **Boundary:** Do not market this as NSPIRE/HUD compliance without expert review, maintained mappings, and validated product behavior.

### 5. Inspection comparison and change history

- **Gap status:** Needs product audit. Inspection history and audit history exist in parts of the product; a side-by-side comparison of repeated inspections is not confirmed.
- **Idea:** Compare two inspections for the same unit or area, highlight newly found, resolved, recurring, and unchanged conditions, and link each change to follow-up work.
- **Why it could matter:** Helps distinguish a new issue from a repeated repair and gives an operator a faster review before sign-off.
- **Questions to validate:** Which inspection pairs are most useful—move-in vs. move-out, prior routine vs. current routine, or pre-repair vs. reinspection? How should changed checklists be handled?
- **Possible measure:** Time to review repeat inspections; recurring-finding rate; number of repeat issues linked to prior work.

## Additional ideas to validate

| Idea | Status | Potential value | Key validation question |
| --- | --- | --- | --- |
| Offline inspection capture and later sync | **Documented gap** | Reliable field work in low-connectivity buildings | How often do inspectors lose connectivity, and what data/media must be available offline? |
| Inspector route planning and travel-aware dispatch | **Documented gap** | Reduce avoidable travel for multi-property inspection days | Do customers need route optimization, or simply a shared schedule and assignment queue? |
| Automated inspector scheduling | **Documented gap** | Reduce coordination for recurring inspections | Is scheduling owned by one coordinator, a property manager, or external inspectors? |
| Resident-led inspection intake | **Needs product audit** | Let residents document conditions before move-in or report a concern | Would residents use it, and what moderation, privacy, and accessibility controls are needed? |
| Vendor work status portal | **Needs product audit** | Let external vendors accept, update, and document assigned work without staff re-entry | Will vendors use a separate portal, or do operators prefer email/SMS links? |
| Required evidence by finding/checklist rule | **Needs product audit** | Improve consistency when photos or documents are essential | Which issue types need evidence, and who decides that it is sufficient? |
| Turn dependency board and critical path | **Needs product audit** | Show which tasks block the ready date and what can happen in parallel | Do teams need dependencies and schedule risk, or is the existing blocker-aware status enough? |
| Cross-property repeat-issue view | **Needs product audit** | Surface recurring building, equipment, or contractor problems | Which assets and categories are consistent enough to compare across properties? |
| Operational aging and closeout analytics | **Partial** | Help leaders see aging findings, repeat work, and readiness delays | Which measures can the product capture reliably, and what are the agreed definitions? |
| Lock, archive, and correction trail for approved inspections | **Needs product audit** | Preserve confidence in completed records while allowing corrections | What should be immutable, and what correction process meets customer policy? |

## Do not add these as claims yet

- NSPIRE or HUD compliance, official filing, or automatic standards updates.
- Guaranteed reduction in turn time, overdue work, vacancy loss, or staff workload.
- Offline support, route optimization, or automatic dispatch.
- Production integrations with specific PMS, accounting, inspection, or vendor platforms.
- Automated resident/vendor alerts across all workflow events.

The product capability notes identify offline behavior as unknown, integrations as mock/sandbox only, and production deployment and customer outcomes as unverified. Verify each before turning an idea into a public promise.

## Suggested order

1. **Audit the current app** for finding-to-work conversion, closeout verification, comparison views, template governance, reminder rules, and external-user workflow. Update the capability notes with screenshots and exact behavior.
2. **Interview 8–12 operators** about the inspection-to-repair handoff. Ask for a walkthrough of a recent example and identify current systems, roles, delays, and evidence needs.
3. **Prototype the strongest missing step** with real operators before building broad automation. The first prototype should likely focus on creating, assigning, and verifying work from an inspection finding.
4. **Run a scoped pilot** and baseline assignment time, open finding age, verified completion time, reinspection result, days to ready, staff follow-ups, and evidence completeness.
5. **Choose roadmap work from repeated pain and buyer commitment**, not competitor feature parity alone.

## Recommendation

Start with **finding-to-work conversion plus verified closeout** as the product discovery theme. It extends the workflow Compass already supports, addresses the market research’s central handoff question, and can be evaluated with observable measures. Audit the current release first; some portions may already exist beyond what the product documentation confirms.
