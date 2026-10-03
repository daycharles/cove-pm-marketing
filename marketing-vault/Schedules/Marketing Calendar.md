# Averion Software Marketing Calendar

> **2026-10-02 transition:** The recurring runs below are historical planning records, not active Hermes commitments. The Windows two-hour monitor was deleted; there is no Hermes marketing cron. ChatGPT legacy tasks and its reported daily brief remain unverified/unremoved pending account access. Use [[Plans/Hermes Marketing Plan - 2026-10-02]] for current on-demand work. Dated checkpoints are internal review targets, not scheduler triggers.

The table below records the previous cadence for historical reference. No new work is driven by these times; only the planning windows remain useful as optional internal checkpoints.

## Former recurring marketing runs (historical)

| Run | Cadence | Current evidence | Notes |
| --- | --- | --- | --- |
| Averion Software Marketing Daily Brief | Weekdays at 7:30 AM America/New_York | Latest durable brief: 2026-09-30 | Summarizes previous-day work; see [[Marketing Schedule]] and [[Sessions/2026-09-30 - Averion Marketing Daily Brief]] |
| Averion Software Marketing OS Monitor | Configured weekdays every 2 hours, 8:00 AM–6:00 PM | Latest locally observed execution artifact: 2026-09-29; live task inventory unavailable | Scheduled workbench posts a run report to the shared Marketing OS API when its token is configured. Each cycle may be an idle queue watch rather than new research. |
| Legacy morning / midday agent tasks | Paused since 2026-09-20 | No new output expected | These are not the two-hour local monitor. |

## Current planning windows

Dates below translate the 30-day plan started 2026-09-30 into calendar dates. They are internal checkpoints, not RFP submission deadlines or promises. No live private software RFP or operator due date has been verified.

| Window | Checkpoint | Source |
| --- | --- | --- |
| Oct 1–5, 2026 | Confirm product truth and name evidence owners; prepare claim register and gap list | [[Plans/Private Multifamily RFP Exposure and Discovery Plan - 2026-09-30]] |
| Oct 1–7, 2026 | Refresh six operator account briefs; select two first-wave accounts | [[Plans/Private Multifamily RFP Exposure and Discovery Plan - 2026-09-30]] |
| Oct 6–12, 2026 | Map buyer roles and official intake routes; prepare discovery materials | [[Plans/Private Multifamily RFP Exposure and Discovery Plan - 2026-09-30]] |
| Oct 10–18, 2026 | Review RFP response library with product, security, legal, and delivery owners | [[Plans/Private Operator RFP Answer Library - Draft - 2026-09-30]] |
| Oct 15–24, 2026 | Conditional discovery outreach decision window; required approvals first | [[Plans/Private Multifamily RFP Exposure and Discovery Plan - 2026-09-30]] |
| Oct 20–30, 2026 | Review discovery and procurement evidence; recommend submit, nurture, or stop | [[Plans/Private Multifamily RFP Exposure and Discovery Plan - 2026-09-30]] |

## Dated marketing notes

```dataview
CALENDAR date
FROM "Sessions" OR "Research" OR "Experiments" OR "Leads" OR "Pilots"
WHERE date != null OR date_added != null OR date_created != null
```

## Open tasks with due dates

```tasks
not done
has due date
sort by due
sort by priority
limit 50
```

## Approval deadlines and gated work

```dataview
TABLE date, type, status, company, pricing_status, price_lock_status
FROM "Pilots"
WHERE approval_required = true
SORT date DESC
```

Any spend, agreement, pricing, discount, price lock, product commitment, timeline promise, or outcome promise must remain blocked until approved.
