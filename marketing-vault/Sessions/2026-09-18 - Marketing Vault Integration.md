# Marketing Vault Integration — 2026-09-18

## Objective

Connect the CovePM marketing vault to the active weekday marketing schedule and daily brief.

## Completed

- Updated the 8:00 AM CovePM Marketing Agent task to read the vault and save a dated run note under `Sessions/`.
- Updated the 12:30 PM CovePM Marketing Agent task to read the vault, including the morning note when available, and save a dated run note under `Sessions/`.
- Updated the 7:30 AM `CovePM Marketing Daily Brief` automation to read both agent-session notes plus Notion and Slack context, then save a dated brief under `Sessions/`.
- Updated `Dashboard.md`, `Schedules/Marketing Schedule.md`, and `Schedules/Automation Inventory.md` to document the integration.
- Posted the operational update to Slack `#notion-updates`.

## Schedule

| Run | Cadence | Durable output |
| --- | --- | --- |
| CovePM Marketing Daily Brief | Weekdays, 7:30 AM America/New_York | Dated brief in `Sessions/` |
| CovePM Marketing Agent — morning | Weekdays, 8:00 AM America/New_York | Dated session note in `Sessions/` |
| CovePM Marketing Agent — midday | Weekdays, 12:30 PM America/New_York | Dated session note in `Sessions/` |

## Guardrails

The existing approval gates remain active. The agents must not spend money, send marketing email,
make commitments, promise discounts or price locks, or store credentials, API keys, restricted
personal data, or secrets in the vault without the required approval.

## Evidence

- Scheduled Tasks showed both CovePM tasks active at 8:00 AM and 12:30 PM on weekdays.
- Automation `covepm-marketing-daily-brief` is active at 7:30 AM on weekdays.
- Slack update link: https://covepm.slack.com/archives/C0C29GPPCLV/p1789748886414439
