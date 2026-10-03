# Averion Software Marketing Dashboard

> Start here. This is the operating view for marketing work.

**Vault navigation:** [[Homepage|Open Homepage]] · **Daily operations:** [[Dashboard]]

**Company:** Averion Software LLC, a Virginia company. **Property-management product:** Averion Compass. **Separate product:** StellaAI. See [[Strategy/Company Identity]] for the verified wording and claims guardrails.

## Current priorities

- [ ] **Current operating plan:** [[Plans/Hermes Marketing Plan - 2026-10-02]]. On-demand private-operator research plus the new [[Plans/LinkedIn Daily Publishing Pilot - 2026-10-02]]. Owner chose Buffer Free and no per-post approval for the bounded LinkedIn lane. Social themes should span property operations—not just inspections and maintenance—with product claims kept to verified Compass capabilities. The Company Page has one daily 9:00 AM New York slot. The public image URL is live and byte-verified; Buffer shows four sent posts and zero queued. The first draft substantially overlaps the September 29 post and is blocked. The manual owner-restricted workflow is published, and its first authenticated read-only run succeeded but skipped because there is no eligible post in the manifest. The repository secret is configured; media preflight, post creation/readback, and recurring schedule remain unverified/disabled. The old Windows monitor/report/mobile sync remain retired. Legacy ChatGPT task removal still requires account access.
- [ ] Use the on-demand Research and Review specialists to verify Winn's corporate email is suitable for a business/technology routing inquiry, then verify Drucker + Falk's contact destination and buyer roles. The revised [[Research/Private Operator Route QA - 2026-10-02]] clears only internal research-slot selection; no qualified route or contact permission is established.
- [ ] Keep the Compass product-claim and RFP answer gaps with product/engineering, security, implementation, and legal/commercial owners. Prepare an exact outreach artifact only after route and buyer evidence warrant it; obtain human approval before any external action.
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
| Private-operator routes | Hermes Research + Review, on demand | Internal conditional shortlist | Winn business-route suitability; Drucker + Falk contact and role evidence |
| RFP/product truth | Hermes RFP + named human owners | Gaps open | Confirm current release and owner-approved answer evidence |
| Outreach | Human-gated | No send-ready draft | Verify route, draft exact artifact, QA, then human review |
| Social | Hermes Content + Review; Buffer Free | Daily slot/image verified; authenticated read-only run passed; draft blocked as near-duplicate; no queue/recurrence | Draft a distinct post on a broader theme, QA it, then run read-only media preflight |

## Local pipeline lane

The company-first qualification workflow is available at `agent-workbench/lead_engine.py`. It
accepts supplied company evidence, scores fit locally with Ollama, records qualified/review/
nurture/disqualify decisions in SQLite, and stops before contact discovery or sending.

## Current automation status

- **2026-10-02 transition:** The enabled Windows `\CovePM Marketing OS Monitor` was deleted. No Hermes cron jobs exist. The scheduled checkout's `run-weekly.ps1` is no longer scheduled and its hosted report/mobile approval bridge was removed; manual use still creates/runs a local social brief, so do not run it as part of this plan. See [[Plans/Hermes Marketing Plan - 2026-10-02]] and [[Schedules/Automation Inventory]].
- The old ChatGPT morning/midday tasks were last documented as paused; the ChatGPT daily brief was reported active. Their current account status and removal are **unverified** because ChatGPT requires login. Do not treat those historical automations as this plan.
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
