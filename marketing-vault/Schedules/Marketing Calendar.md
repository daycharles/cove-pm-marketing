# Averion Software Marketing Calendar

This calendar is driven by dated session notes and tasks. The recurring marketing runs are:

- **7:30 AM weekdays:** Averion Software Marketing Daily Brief
- **Every 2 hours, weekdays 8:00 AM–6:00 PM:** local Averion Software Marketing OS Monitor
- The ChatGPT morning and midday agent schedules remain paused.

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
