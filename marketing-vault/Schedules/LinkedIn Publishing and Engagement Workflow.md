# Averion Software LinkedIn Publishing & Engagement Workflow

Status: **fully superseded** by [[Plans/LinkedIn Daily Publishing Pilot - 2026-10-02]]; all instructions below are historical, including individual approval and Publora scheduling
Channel (historical): Averion Software LinkedIn Company Page via Publora
Content creation: Hermes on-demand drafting and image generation; existing ready catalog assets may be reused
Publishing mode (historical): per-post approval; now replaced only for the bounded LinkedIn Buffer lane by an independent exact-payload QA and audit gate

## Publishing model

**Current override (2026-10-02):** Aim for one distinct LinkedIn post per calendar day, including weekends, with no per-post approval under the owner's standing instruction. Buffer Free is connected but currently has no verified posting slots or queued posts; no runner is installed. The live image, QA, media-hosting and scheduling rules are in the pilot plan. The older Publora, two-post weekday, approval and engagement details below are historical only.

- One distinct reviewed post per calendar day is the pilot target. Prepare the next two days of drafts ahead of time; only individually approved posts can be added to the Publora calendar by the human owner.

## Daily output target

### Required image for every post

Every post must include a relevant image selected from the [social asset catalog](../Assets/social-catalog/CATALOG.md), or a newly created and reviewed asset added to that catalog. Agents should use [catalog.json](../Assets/social-catalog/catalog.json) to match topics and select only `ready` assets. Follow the [asset selection guide](../Assets/social-catalog/README.md).

Each draft must record `asset_id`, `asset_path`, `alt_text`, and `visual_type`; include any required AI illustration disclosure in the caption. The human approval preview must show the exact attached image. Check [usage.csv](../Assets/social-catalog/usage.csv) and queued drafts to rotate imagery, then record the reservation and subsequent schedule/publication. Upload the local file through the media workflow and confirm attachment success before scheduling. A missing image blocks scheduling. Screenshot entries marked `hold` or `reference` cannot be selected as ready feed media.

The daily post may be operator education, a narrow product explanation or a useful question. Engagement is optional and human-approved, not tied to fixed-time blocks. Weekends use the same exact-artifact review gate as weekdays.

## Former weekday lane examples (not a posting timetable)

| Day | Morning lane | Afternoon lane |
|---|---|---|
| Monday | Property operations insight | Averion Compass workflow walkthrough |
| Tuesday | Resident-experience lesson | Short field note or checklist |
| Wednesday | Myth, bottleneck, or handoff failure | Product education with a visual |
| Thursday | Regional/portfolio operations question | Build-in-public or research note |
| Friday | Community question or operator prompt | Weekly recap and next-step CTA |

Use a 60/25/15 mix: practical education, product education, and company/community content. Lead with the problem and the useful idea; mention Averion Compass when it clarifies the workflow. Keep StellaAI separate and secondary in property-management content.

## Workflow

1. **Evidence packet.** Hermes may research public company-level sources on demand when an angle needs it; record URLs and access dates in `Research/`. Routine educational posts can use current approved product facts and need not claim a fresh social-listening pass. No scraping gated/private LinkedIn content or copying competitor creative. Do not invent customer results, integrations, compliance claims, pricing, or performance outcomes.
2. **Check the calendar and recent-post ledger before drafting.** Review all published posts from the last 90 days, scheduled posts, and approved drafts. If a post was deleted from LinkedIn, retain its copy and topic in the local ledger as recently used. Check exact text and meaning: changing a hook, CTA, or a few words does not make a repeated post new. If Publora history is incomplete, use the saved artifacts and ask for a review instead of assuming an angle is unused.
3. **Choose a distinct angle.** Assign each proposed post a primary topic, audience pain, format, and CTA. Within one batch, and against the recent-post ledger, each must teach a different idea or address a different operator problem. Rotate among resident communication, vendor coordination, turn readiness, work-order triage, assignment, scheduling, completion, overdue work, repeat repairs, and staff workload. Do not reuse the maintenance-handoff/visibility proposition or the same demo CTA in consecutive posts. A different wording of the same claim is still a duplicate.
4. **Draft the queue.** Create evergreen posts as separate artifacts, plus timely posts when scheduled. Each artifact includes a hook, one clear idea, body copy, CTA, source/fact note, research observation, risk flags, and an internal uniqueness note naming the closest recent post and the substantive difference. Keep this note outside the publish preview.
5. **Quality check.** Check factual support, readability, accessibility, character length, research freshness, CTA, and similarity against recent published, scheduled, approved, and same-batch posts. Reject and redraft any post whose central idea or promise substantially overlaps another. Do not send an overlapping item for approval or scheduling; if no distinct supported idea is available, return fewer posts and say why.
6. **Human approval.** A reviewer checks and explicitly approves each exact caption and attached image individually. The retired mobile approval sync is not a review path; the reviewed copy and media must match what is uploaded to Publora. The reviewer confirms the uniqueness note and rejects near-duplicates.
7. **Schedule.** A human adds only approved posts to Publora’s calendar for assigned dates and times. Before inserting, check that date and slot are not already covered by an approved or scheduled post. Prefer a two-day approved buffer; never fill a gap with an unapproved item.
8. **Engage.** During each engagement block, respond to inbound comments first, then add value to up to five relevant public conversations. Do not argue, make commitments, give support decisions, collect personal data, or send unsolicited DMs.
9. **Measure.** Record impressions, reactions, comments, profile/page visits, follows, link clicks when available, qualified conversations, and any human corrections. Review weekly and adjust the next week’s lanes.

## Recent-post ledger and uniqueness gate

Maintain `Research/LinkedIn Content Ledger.md` with each post’s publish date, scheduled date/slot, exact approved copy, topic, audience pain, format, CTA, and status (draft, approved, scheduled, published, or deleted). Check it before every batch and update it when a draft is approved, scheduled, published, or deleted. Keep deleted copy in the ledger so it cannot be accidentally regenerated. Compare against the previous 90 days and all future scheduled/approved posts. Similarity means the central idea, claim, or intended takeaway is substantially the same, even when wording differs. Each new post must bring a distinct takeaway; if that cannot be shown in one sentence, it does not pass.

## Approval checklist

- [ ] Exact copy and media reviewed.
- [ ] Relevant image attached successfully; catalog asset ID and alt text recorded; rotation and asset restrictions checked.
- [ ] Averion Software LinkedIn Company Page selected.
- [ ] Sources or approved product facts recorded.
- [ ] Current social-listening note linked, dated, and less than 14 days old.
- [ ] Competitor observations informed the angle without copying language, claims, or creative.
- [ ] Recent-post ledger checked, including deleted LinkedIn copy, scheduled posts, and other posts in this batch.
- [ ] Topic, audience pain, format, and takeaway are distinct; a wording-only change does not pass.
- [ ] Internal uniqueness note names the closest comparator and explains the substantive difference.
- [ ] No unsupported customer, ROI, compliance, integration, or AI claim.
- [ ] No pricing, contract, implementation, roadmap, or outcome commitment.
- [ ] CTA is appropriate for the audience.
- [ ] Comment/reply is specific, respectful, and non-spammy.
- [ ] Publish/schedule action confirmed in Publora.

## Default CTA library

- “What does this handoff look like on your team?”
- “Where does this process usually lose visibility?”
- “If this is a live workflow problem, we’re open to comparing notes.”
- “Would a short conversation about the current baseline be useful?”

## Operating guardrails

Publora and Canva are the selected free tools for this workflow. HubSpot remains a separate CRM option and is not required for LinkedIn publishing. LinkedIn publishing, comments, reactions, and reshares remain human-approved representational actions.
