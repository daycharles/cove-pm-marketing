---
date: 2026-10-17
type: marketing-session
owner: Averion Software Marketing Agent
status: in_progress
segment: LinkedIn publishing
objective: Build a varied, source-backed 30-day LinkedIn calendar and prepare eligible posts for the first manual weekly Buffer batch.
source_urls: []
approvals_needed: []
---

# LinkedIn 30-Day Draft Build — 2026-10-17

## Outcome

Built [[Research/LinkedIn 30-Day Content Pool - 2026-10-17]] for 2026-10-18 through 2026-11-16: 30 distinct daily themes, audience notes, central takeaways, exact public copy, per-post source IDs, direct source URLs, and an evidence block. The topic mix extends beyond inspections and maintenance across resident service, staff coordination, IPM, waste, energy/water, procurement, IAQ, continuity, and preparedness. Captions do not assert unsupported Compass capabilities, customer outcomes, savings, health outcomes, or legal compliance.

## Live state and media gates

- Read-only Buffer audit run `39048316379`, observed 2026-10-17 12:11 PM EDT: queue 0/10, no scheduled local days, sent-day history through 2026-10-11. This records Buffer status, not a public-LinkedIn URL check for every item.
- Five first-week image/caption pairs passed exact-copy/source/uniqueness, visual, public-byte, and topic-fit review: Oct 18 (AC-G003), Oct 20 (AC-G012), Oct 21 (AC-G009), Oct 23 (AC-G008), and Oct 24 (AC-G010).
- Oct 19 (AC-G006) is held: image quality is acceptable, but the generic conversation shows no visible transport, route, vehicle, or mobility-aid cue for the accessible-evacuation caption.
- Oct 22 (AC-G007) is held: image quality is acceptable, but the scene reads as resident counseling and does not fit a waste-service contract-capacity post.
- Both held captions remain `needs_media`, their asset assignments and usage reservations were removed, and the posts are not eligible for Buffer. The remaining 23 dates have no media assigned.
- AC-G001 and AC-G011 remain held for their prior artifact findings.

## Audit and verification

- The content pool includes attached source evidence; `sources.py verify --evidence` passed. The full task ledger includes additional unused sources not cited in this pool.
- Focused LinkedIn runner tests passed (24 tests). Full workbench suite passed (65 tests) with `COVE_HUBSPOT_PRIVATE_APP_TOKEN` unset in the test process; no credential value was read or printed.
- Local manifest and usage records reflect five ready posts, two media holds, and five image reservations. No Buffer mutation, commit, or push is recorded in this session note.
- The test/publication workflow remains manual; no recurring trigger was enabled.

## Decision

Schedule only posts whose exact copy, source, uniqueness, channel, slot, and actual image all pass. Do not fill Oct 19 or Oct 22 with unrelated images. A queued item is not a published post; verify each item after its slot and record public URLs separately.

## Next checkpoint

Refresh live Buffer state immediately before the weekly batch. If the queue and idempotency gates still pass, schedule Oct 18, 20, 21, 23, and 24 at 9:00 AM `America/New_York`, read back each scheduled item (status, due time, caption, channel, image/MIME), and update the manifest, usage ledger, and content ledger after each verified schedule. Leave Oct 19 and Oct 22 unscheduled until topic-matched media passes visual and public-hosting QA. Prepare media for the remaining 23 dates in later week-sized batches.
