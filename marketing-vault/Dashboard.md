# Averion Software Marketing Dashboard

> Start here. This is the operating view for marketing work.

**Vault navigation:** [[Homepage|Open Homepage]] · **Daily operations:** [[Dashboard]]

**Company:** Averion Software LLC, a Virginia company. **Property-management product:** Averion Compass. **Separate product:** StellaAI. See [[Strategy/Company Identity]] for the verified wording and claims guardrails.

## Current priorities

- [ ] Confirm the highest-value initial segment and buyer; current product evidence favors housing authorities and affordable-housing operators.
- [ ] Maintain evidence-backed Averion Compass positioning around inspections and unit turns.
- [ ] Keep StellaAI visible as a separate product without letting it overshadow Compass in property-management messaging.
- [ ] Keep the website message, calls to action, and proof current.
- [ ] Turn agent research into measurable experiments.
- [ ] Run and evaluate ten local workbench tasks before adding live integrations.
- [ ] Review the local control room queue and pending approvals each weekday.
- [ ] Supply company-level candidates to the local qualification workflow and review dispositions.
- [ ] Scope content production, lead qualification, and approved outreach workflows.
- [ ] Review the approval-gated discovery drafts for WinnResidential/WinnCompanies and Drucker + Falk.
- [ ] Use the pilot-readiness scorecard after any approved discovery conversation.
- [ ] Execute the Marketing Team Productization Roadmap: instrument the pilot before adding connectors.
- [ ] Complete the first five evaluated workbench tasks and ten reviewed company candidates.
- [ ] Current checkpoint: review 3 awaiting-approval items, 4 blocked items, and 6 queued items; lead queue is 3 approved / 14 pending.

## Quick links

- Vault root: `C:\Users\cd104535\Documents\Codex\cove-pm-marketing\marketing-vault`
- Daily brief output: `Sessions/`

- [[Strategy/Positioning]]
- [[Brand/Brand Guide]]
- [[Assets/website-screenshots/README]]
- [[Strategy/ICP and Personas]]
- [[Schedules/Marketing Schedule]]
- [[Schedules/Marketing Calendar]]
- [[Kanban/CovePM Marketing Board]]
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
| Market intelligence | Marketing agent | Not started | Add next validated finding |
| Website messaging | Marketing agent | In progress | Review against current positioning |
| Local workbench | Local runner | In progress | Complete ten evaluated runs |
| Content factory | Marketing agent | In progress | Review approval-gated discovery drafts for two priority accounts |
| Pipeline / pilots | Marketing agent | In progress | Review discovery drafts, then score the first approved conversation with the pilot-readiness scorecard |
| Approved outreach | Human-gated | Planned | Confirm mailbox and suppression process |
| Productization pilot | Averion Software marketing OS | In progress | Establish baselines and complete Phase 0 exit criteria |

## Local pipeline lane

The company-first qualification workflow is available at `agent-workbench/lead_engine.py`. It
accepts supplied company evidence, scores fit locally with Ollama, records qualified/review/
nurture/disqualify decisions in SQLite, and stops before contact discovery or sending.

## Current automation status

- The 8:00 AM and 12:30 PM ChatGPT marketing tasks are paused and now carry Averion Software / Averion Compass branding. The local workbench remains the active routine path.
- The local workbench is the replacement path: `agent-workbench/`.
- Ollama is installed locally with `qwen3:4b`; the real-model workflow has completed a verified run.
- The local control room is available at `agent-workbench/outputs/CONTROL-ROOM.md` after `python agent-workbench/control_room.py dashboard`.
- The weekly operating checkpoint is available at `agent-workbench/outputs/WEEKLY-REVIEW.md` after `python agent-workbench/control_room.py weekly-review`.
- Pending local approvals are synced to the mobile approval inbox with artifact excerpts; repeated runs deduplicate by local approval ID and run ID.
- Notion has been reviewed read-only; the six-account Notion priority set is mirrored in `agent-workbench/outputs/lead-qualification-notion.md`.

The local Ollama workbench is now the active routine path; legacy Codex tasks are paused and will not produce new session notes unless manually resumed.

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
