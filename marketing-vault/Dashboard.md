# Averion Software Marketing Dashboard

> Start here. This is the operating view for marketing work.

**Vault navigation:** [[Homepage|Open Homepage]] · **Daily operations:** [[Dashboard]]

**Company:** Averion Software LLC, a Virginia company. **Property-management product:** Averion Compass. **Separate product:** StellaAI. See [[Strategy/Company Identity]] for the verified wording and claims guardrails.

## Current priorities

- [ ] **Current operating plan:** [[Plans/Hermes Marketing Plan - 2026-10-02]] and [[Plans/LinkedIn Daily Publishing Pilot - 2026-10-02]]. LinkedIn remains Company Page only, one 9:00 AM America/New_York slot, manual weekly batches, no recurrence. The Oct 3 post was publicly verified; the Oct 5–11 batch was scheduled/read back, but individual public URLs were not re-verified in this run. The source-backed Oct 18–Nov 16 pool spans resident service, staff coordination, IPM, waste, utilities, procurement, IAQ, continuity, and preparedness. First-week state: 1/5 eligible posts scheduled (2026-10-18 Buffer ID `6ad3b053ae173d6e00c8bd22`); 4 eligible remain; 2 held for media/topic fit; 23 later dates need media. Latest exact-date run `39053921811` pre-mutation state: queue 0/10, scheduled local days [], sent history through 2026-10-11. These items are scheduled, not published. Keep Compass claims within approved evidence; AC-G001 and AC-G011 remain held.
- [ ] The October 5 [[Research/Private Operator Route and Role Recheck - 2026-10-05]] confirmed Winn's general corporate mailbox and clarified Drucker + Falk's general contact and operations/IT role hypotheses. Neither page establishes a software-evaluation destination: 0/2 appropriate routes documented in this packet. [[Plans/Private Operator Outreach Activation - 2026-10-05]] now proposes one unsent routing-only Winn inquiry; owner must approve exact sender, recipient, copy, suppression/opt-out and audit before any send. No discovery pitch or contact yet.
- [ ] Use the October 5 [[Research/Private Operator Product RFP Evidence Request and Gate Matrix - 2026-10-05]] to assign named product/release, engineering, security/privacy, implementation/support, legal/commercial and leadership approvers and collect current, release-bound evidence. No live software RFP, approved technical answer or bid decision is established. Prepare exact outreach only after route and buyer evidence warrant it, with human approval before external action.
- [ ] Resolve the older Compass caption package separately; do not schedule an overlapping post. The first new generated-image draft [[Sessions/2026-10-02 - LinkedIn Sign-off Draft]] is held as a near-duplicate of the September 29 post.

## Quick links

- Vault root: `C:\Users\cd104535\Documents\Codex\cove-pm-marketing\marketing-vault`
- Daily brief output: `Sessions/`

- [[Strategy/Positioning]]
- [[Brand/Brand Guide]]
- [[Assets/website-screenshots/README]]
- [[Strategy/ICP and Personas]]
- [[Schedules/Marketing Schedule]]
- [[Schedules/Marketing Calendar]]
- [[Kanban/Averion Compass Marketing Board]]
- [[Research/Research Index]]
- [[Research/Marketing Material Harvest - here-x20 Docs - 2026-09-25]]
- [[Plans/Averion Compass Data Migration and Pilot Readiness]]
- [[Sessions/Session Index]]
- [[Experiments/Experiment Log]]
- [[Website/Website Notes]]
- [[Templates/Research Note]]
- [[Templates/Session Note]]
- [[Templates/Lead Note]]
- [[Templates/Pilot Note]]
- [[Schedules/Obsidian Setup Checklist]]
- [[Plans/AI Marketing Team Implementation Plan]]
- [[Plans/Marketing Team Productization Roadmap]]
- [[Plans/AI Marketing Team Independent Red-Team Review]]
- [[Research/Notion Knowledge Sync - 2026-09-20]]

## This week

| Workstream | Owner | Status | Next checkpoint |
| --- | --- | --- | --- |
| Private-operator routes | Hermes Research + human account owner | Oct 5 source recheck complete; 0/2 appropriate software routes documented; one routing-only candidate unsent | Approve/reject one exact Winn routing inquiry after company email and suppression preflight; discovery pitch remains blocked |
| RFP/product truth | Hermes RFP + named human owners | Oct 5 evidence-request matrix drafted; gates remain open | Name approvers; supply current release and section evidence; identify actual solicitation before bid decision |
| Outreach | Human-gated | No send-ready draft | Verify route, draft exact artifact, QA, then human review |
|| Social | Hermes Content + Review; Buffer Free | Oct 3 move-in/orientation post published; seven distinct follow-ups scheduled and verified for Oct 5–11 at 9:00 AM ET with unique AI-disclosed images; themes include resident access, energy, IAQ, late-rent communication, waste, and staff coordination; no Compass feature claims; no recurring workflow | Verify actual publication and public URLs after each slot; review engagement metrics after Oct 11 |

## Local pipeline lane

The company-first qualification workflow is available at `agent-workbench/lead_engine.py`. It
accepts supplied company evidence, scores fit locally with Ollama, records qualified/review/
nurture/disqualify decisions in SQLite, and stops before contact discovery or sending.

## Current automation status

- **2026-10-02 transition:** The enabled Windows `\CovePM Marketing OS Monitor` was deleted. No Hermes cron jobs exist. The scheduled checkout's `run-weekly.ps1` is no longer scheduled and its hosted report/mobile approval bridge was removed; manual use still creates/runs a local social brief, so do not run it as part of this plan. See [[Plans/Hermes Marketing Plan - 2026-10-02]] and [[Schedules/Automation Inventory]].
- The old ChatGPT morning/midday tasks were last documented as paused; the ChatGPT daily brief was reported active. Their current account status and removal are **unverified** because ChatGPT requires login. Do not treat those historical automations as this plan.
- The October 5 [[Sessions/2026-10-05 - Legacy Automation Inventory Reconciliation]] rechecked the exact Windows task (absent), default-profile Hermes cron (no jobs), and the separate local-only launcher (retirement still uncommitted there). ChatGPT task state remains inaccessible and removal unverified; use an authorized interactive account session to identify and retire only the three exact legacy entries, then read back the list.
- Local Ollama/workbench outputs remain historical evidence or optional manual tools, not the current recurring execution path. The hosted Marketing OS endpoints and existing hosted records were not deleted.

## Evidence rules

Separate observed results from assumptions. Do not invent customer names, logos, benchmarks,
certifications, integrations, guarantees, or case-study results. Record source URLs and dates for
research claims.

## Live views

### Open tasks

```tasks
not done
sort by priority
sort by due
limit 50
```

### Recent sessions

```dataview
TABLE date, status, segment, objective
FROM "Sessions"
WHERE type = "marketing-session"
SORT date DESC
LIMIT 10
```

### Recent research

```dataview
TABLE date, status, topic, segment, confidence
FROM "Research"
WHERE type = "research"
SORT date DESC
LIMIT 10
```

### Approval queue

```dataview
TABLE date, type, status, approvals_needed
FROM "Sessions" OR "Experiments"
WHERE approval_required = true OR (approvals_needed != null AND length(approvals_needed) > 0)
SORT date DESC
```
