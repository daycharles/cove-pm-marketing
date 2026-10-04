---
type: marketing-session
date: 2026-10-04
status: week-batch-scheduled-and-verified
segment: LinkedIn daily pilot
objective: Build a varied source-backed pool and schedule a weekly Buffer batch
---

# 2026-10-04 — LinkedIn Daily Post Pool

## Outcome

Four research/drafting bots returned eight candidate captions across resident communication, multifamily energy engagement, indoor-air-quality communication, commercial-building waste routines, and staff coordination. I reviewed the exact drafts against official SAHF, ENERGY STAR, and EPA sources, kept seven distinct candidates, and removed one duplicate employee-onboarding angle. The seven-item pool is [[Research/LinkedIn Daily Post Pool - 2026-10-04]]. All seven were manually scheduled through Buffer after public-byte, same-day history/queue, target-day, attachment, and exact-item readback gates passed. They are future queue items, not published posts, and are general property-operations education rather than Compass capability claims.

## Verified weekly Buffer batch

The seven owner-dispatched runs each returned `scheduled_verified` with the exact caption, channel ID, requested due time, Buffer status `scheduled`, and one attached `image/png`. The runner checked the public image against the cataloged local SHA-256 before each mutation. This was a one-time weekly batch; no recurring workflow was enabled.

| Date / slot (EDT) | Theme | Asset | Buffer post ID | Workflow run |
|---|---|---|---|---|
| 2026-10-05 9:00 AM | Ongoing resident communication | AC-G006 | `6ac28e1fd467abe8cfc05859` | 37221007496 |
| 2026-10-06 9:00 AM | Energy priorities in staff onboarding | AC-G012 | `6ac28e696ae9cbb01a53a54f` | 37221088335 |
| 2026-10-07 9:00 AM | Indoor-air-quality communication | AC-G009 | `6ac28e946ae9cbb01a53ae76` | 37221135633 |
| 2026-10-08 9:00 AM | Late-rent notice next steps | AC-G007 | `6ac28ecad467abe8cfc08307` | 37221186239 |
| 2026-10-09 9:00 AM | Resident energy engagement | AC-G008 | `6ac28ef1ca1380ada8307a95` | 37221235042 |
| 2026-10-10 9:00 AM | Cross-functional waste routines | AC-G010 | `6ac28f1bfe1389e4133a7d18` | 37221279416 |
| 2026-10-11 9:00 AM | Team contribution recognition | AC-G011 | `6ac28f43d467abe8cfc096e1` | 37221319024 |

Public Pages deployment: commit `bba3832`, run 37220252929 (`success`). Read-only exact-date preflight: run 37220942740 (`dry_run_ready`). The existing Buffer daily 9:00 AM America/New_York channel slot remains configured; these manual exact-date schedule entries do not enable an autonomous daily runner.

## Evidence and QA

- SAHF, *Resident Communication Touchpoints Brief*: https://sahfnet.org/sites/default/files/documents/sahf-resident-communication-touchpoints-brief.pdf (accessed 2026-10-04). Supports accessible communication principles, ongoing outreach examples, and late-rent notice contact/support options with repayment plans qualified as available only.
- ENERGY STAR, *Improving Multifamily Energy Management Through Stakeholder Engagement*: https://www.energystar.gov/sites/default/files/tools/ENERGYSTARMultifamilyStakeholderEngagement_10_2021.pdf (accessed 2026-10-04). Supports onboarding energy education, stakeholder engagement, collaborative activities, and staff recognition suggestions.
- ENERGY STAR, *8 Great Strategies to Engage Tenants on Energy Efficiency*: https://www.energystar.gov/sites/default/files/buildings/tools/8-Great-Strategies-to-Engage-Tenants.pdf (accessed 2026-10-04). Supports sharing goals/information, audience-tailored messages, practical actions, and tenant suggestions.
- U.S. EPA, *Building Air Quality*, “Effective Communication” section: https://www.epa.gov/sites/default/files/2014-08/documents/sec_3.pdf (accessed 2026-10-04). Supports accurate information, role clarity, and systems to log/respond to complaints.
- U.S. EPA, *Managing and Reducing Wastes: A Guide for Commercial Buildings*: https://www.epa.gov/smm/managing-and-reducing-wastes-guide-commercial-buildings (accessed 2026-10-04). Supports cross-functional team participation, goal-setting, and waste tracking; its commercial/institutional scope is retained.

## Measurement / state

- Source-backed captions retained: 7; duplicate draft removed: 1.
- Final visual QA for the seven caption-paired pool images: passed; earlier resident-communication version rejected for exit signage, two late-rent versions rejected for paper gibberish/unreadable badge, and AC-G001 held for whiteboard gibberish.
- Exact caption/source/claim/history QA: 7 passed; each live Buffer run repeated history and target-day checks.
- Images publicly hosted and byte-verified for this pool: 7.
- Buffer items scheduled and verified by readback: 7; published from this batch so far: 0.
- Read-only Buffer workflow run 37132677493 (earlier, before the pool was eligible): `skipped_no_eligible_post`.
- Recurring workflow: not enabled. This batch was scheduled only through seven owner-dispatched manual workflow runs.

## Next actions

1. After each 9:00 AM slot, verify actual publication status and record any public LinkedIn URL; a scheduled queue item is not a published post.
2. After October 11, review available impressions, comments, and clicks. No engagement result is claimed yet.
3. Build a new distinct pool and fresh image set before scheduling the next week; honor the catalog’s 14-day image-reuse preference.
4. Keep the workflow manual and recurrence disabled. If a future preflight, mutation, or readback is uncertain, stop and inspect Buffer before any retry.

## Approvals and boundaries

The owner’s standing bounded LinkedIn exception removes per-post human approval only when all channel, source, claim, uniqueness, media, audit, and readback gates pass. It does not authorize auto-engagement, DMs, other channels, spend, CRM changes, or unsupported product claims. Failed or uncertain gates mean skip and notify.
