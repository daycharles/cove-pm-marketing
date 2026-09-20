# CovePM Marketing Dashboard

> Start here. This is the operating view for marketing work.

## Current priorities

- [ ] Confirm the highest-value initial segment and buyer.
- [ ] Maintain evidence-backed positioning around maintenance speed and the full property-management platform.
- [ ] Keep the website message, calls to action, and proof current.
- [ ] Turn agent research into measurable experiments.
- [ ] Run and evaluate ten local workbench tasks before adding live integrations.
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

## This week

| Workstream | Owner | Status | Next checkpoint |
| --- | --- | --- | --- |
| Market intelligence | Marketing agent | Not started | Add next validated finding |
| Website messaging | Marketing agent | In progress | Review against current positioning |
| Local workbench | Local runner | In progress | Complete ten evaluated runs |
| Content factory | Marketing agent | Planned | Define first source-update package |
| Pipeline / pilots | Marketing agent | Planned | Define qualification and baseline |
| Approved outreach | Human-gated | Planned | Confirm mailbox and suppression process |

## Current automation status

- The existing 7:30 AM, 8:00 AM, and 12:30 PM ChatGPT/Codex tasks are transitional and remain documented in `Schedules/`.
- The local workbench is the replacement path: `agent-workbench/`.
- The local runner is currently verified in mock mode; Ollama setup and real-model benchmarking are still pending.

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
