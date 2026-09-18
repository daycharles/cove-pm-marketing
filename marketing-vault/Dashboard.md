# CovePM Marketing Dashboard

> Start here. This is the operating view for marketing work.

## Current priorities

- [ ] Confirm the highest-value initial segment and buyer.
- [ ] Maintain evidence-backed positioning around maintenance speed and the full property-management platform.
- [ ] Keep the website message, calls to action, and proof current.
- [ ] Turn agent research into measurable experiments.

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

## This week

| Workstream | Owner | Status | Next checkpoint |
| --- | --- | --- | --- |
| Market intelligence | Marketing agent | Not started | Add next validated finding |
| Website messaging | Marketing agent | In progress | Review against current positioning |
| Content experiments | Marketing agent | Not started | Choose first reversible test |
| Pipeline / pilots | Marketing agent | Not started | Define baseline and qualification |

## Next scheduled marketing work

- **7:30 AM weekdays:** CovePM Marketing Daily Brief — summarizes the previous day's agent sessions.
- **8:00 AM weekdays:** CovePM Marketing Agent morning session.
- **12:30 PM weekdays:** CovePM Marketing Agent midday session.

Both agent sessions now read and write this vault. The daily brief reads their dated session notes,
Notion work, and Slack updates, then saves its own dated note under `Sessions/`.

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
