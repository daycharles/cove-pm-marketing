# CovePM Marketing Dashboard

> Start here. This is the operating view for marketing work.

## Current priorities

- [ ] Confirm the highest-value initial segment and buyer; current product evidence favors housing authorities and affordable-housing operators.
- [ ] Maintain evidence-backed positioning around maintenance speed and the full property-management platform.
- [ ] Keep the website message, calls to action, and proof current.
- [ ] Turn agent research into measurable experiments.
- [ ] Run and evaluate ten local workbench tasks before adding live integrations.
- [ ] Review the local control room queue and pending approvals each weekday.
- [ ] Supply company-level candidates to the local qualification workflow and review dispositions.
- [ ] Scope content production, lead qualification, and approved outreach workflows.

## Quick links

- Vault root: `C:\Users\cd104535\Documents\Codex\cove-pm-marketing\marketing-vault`
- Daily brief output: `Sessions/`

- [[Strategy/Positioning]]
- [[Strategy/ICP and Personas]]
- [[Schedules/Marketing Schedule]]
- [[Schedules/Marketing Calendar]]
- [[Kanban/CovePM Marketing Board]]
- [[Research/Research Index]]
- [[Sessions/Session Index]]
- [[Experiments/Experiment Log]]
- [[Website/Website Notes]]
- [[Templates/Research Note]]
- [[Templates/Session Note]]
- [[Templates/Lead Note]]
- [[Templates/Pilot Note]]
- [[Schedules/Obsidian Setup Checklist]]
- [[Plans/AI Marketing Team Implementation Plan]]
- [[Plans/AI Marketing Team Independent Red-Team Review]]
- [[Research/Notion Knowledge Sync - 2026-09-20]]

## This week

| Workstream | Owner | Status | Next checkpoint |
| --- | --- | --- | --- |
| Market intelligence | Marketing agent | Not started | Add next validated finding |
| Website messaging | Marketing agent | In progress | Review against current positioning |
| Local workbench | Local runner | In progress | Complete ten evaluated runs |
| Content factory | Marketing agent | Planned | Define first source-update package |
| Pipeline / pilots | Marketing agent | In progress | Review `agent-workbench/outputs/lead-qualification-primary.md` and choose one public-sector and one affordable pilot candidate |
| Approved outreach | Human-gated | Planned | Confirm mailbox and suppression process |

## Local pipeline lane

The company-first qualification workflow is available at `agent-workbench/lead_engine.py`. It
accepts supplied company evidence, scores fit locally with Ollama, records qualified/review/
nurture/disqualify decisions in SQLite, and stops before contact discovery or sending.

## Current automation status

- The existing 7:30 AM, 8:00 AM, and 12:30 PM ChatGPT/Codex tasks are transitional and remain documented in `Schedules/`.
- The local workbench is the replacement path: `agent-workbench/`.
- Ollama is installed locally with `qwen3:4b`; the real-model workflow has completed a verified run.
- The local control room is available at `agent-workbench/outputs/CONTROL-ROOM.md` after `python agent-workbench/control_room.py dashboard`.
- The weekly operating checkpoint is available at `agent-workbench/outputs/WEEKLY-REVIEW.md` after `python agent-workbench/control_room.py weekly-review`.
- Notion has been reviewed read-only; the six-account Notion priority set is mirrored in `agent-workbench/outputs/lead-qualification-notion.md`.

The scheduled tasks may continue producing session notes until the local workflow has completed its evaluation milestone.

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
