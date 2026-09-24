# CovePM LinkedIn Publishing & Engagement Workflow

Status: active operating design  
Channel: CovePM LinkedIn Company Page via Publora  
Content creation: Canva + local marketing workbench  
Publishing mode: approval-gated; no post, comment, reaction, or reshare is sent without human approval

## Publishing model

- **16:00 ET content queue run:** prepare evergreen posts through the end of the current month. Each post is a separate approval item with an exact publish preview.
- **Three timely posts per week:** prepare from fresh research and route each one through the same QA and approval gate.
- **Publora scheduling:** only an individually approved post is placed on the Publora calendar; unapproved posts remain drafts.

## Daily output target

- **08:30 ET — Post 1: operator insight.** A practical property-operations lesson, maintenance workflow pattern, or question grounded in approved CovePM positioning and research.
- **14:30 ET — Post 2: product education.** A concise workflow explanation, product capability example, visual, or behind-the-scenes build note. Use Canva when a visual materially improves comprehension.
- **10:30 ET and 16:00 ET — engagement blocks.** Review notifications and relevant industry conversations; prepare up to five useful comments, respond to CovePM comments, and identify thoughtful accounts to follow. Keep replies specific and non-promotional.

The target is two posts per weekday and two engagement blocks per weekday. On weekends, the system may prepare drafts and monitor inbound comments, but it does not publish or engage automatically.

## Weekly content lanes

| Day | Morning lane | Afternoon lane |
|---|---|---|
| Monday | Maintenance workflow insight | CovePM workflow walkthrough |
| Tuesday | Resident-experience lesson | Short field note or checklist |
| Wednesday | Myth, bottleneck, or handoff failure | Product education with a visual |
| Thursday | Regional/portfolio operations question | Build-in-public or research note |
| Friday | Community question or operator prompt | Weekly recap and next-step CTA |

Use a 60/25/15 mix: practical education, product education, and company/community content. Lead with the problem and the useful idea; mention CovePM only when it clarifies the workflow.

## Workflow

1. **Codex research packet.** The Codex agent is the only web-research layer. Before drafting, it refreshes the public social-listening pass in `Research/Social Listening and Competitor Messaging - YYYY-MM-DD.md`, records URLs and dates, and produces a compact packet. The local LLM receives that packet as approved context; it does not browse, search, scrape, or perform duplicate competitor research. Pull from approved vault facts, product notes, and public company-level research. Do not invent customer results, integrations, compliance claims, pricing, or performance outcomes.
2. **Check the calendar and recent-post ledger before drafting.** Review all published posts from the last 90 days, scheduled posts, and approved drafts. If a post was deleted from LinkedIn, retain its copy and topic in the local ledger as recently used. Check exact text and meaning: changing a hook, CTA, or a few words does not make a repeated post new. If Publora history is incomplete, use the saved artifacts and ask for a review instead of assuming an angle is unused.
3. **Choose a distinct angle.** Assign each proposed post a primary topic, audience pain, format, and CTA. Within one batch, and against the recent-post ledger, each must teach a different idea or address a different operator problem. Rotate among resident communication, vendor coordination, turn readiness, work-order triage, assignment, scheduling, completion, overdue work, repeat repairs, and staff workload. Do not reuse the maintenance-handoff/visibility proposition or the same demo CTA in consecutive posts. A different wording of the same claim is still a duplicate.
4. **Draft the queue.** Create evergreen posts as separate artifacts, plus timely posts when scheduled. Each artifact includes a hook, one clear idea, body copy, CTA, source/fact note, research observation, risk flags, and an internal uniqueness note naming the closest recent post and the substantive difference. Keep this note outside the publish preview.
5. **Quality check.** Check factual support, readability, accessibility, character length, research freshness, CTA, and similarity against recent published, scheduled, approved, and same-batch posts. Reject and redraft any post whose central idea or promise substantially overlaps another. Do not send an overlapping item for approval or scheduling; if no distinct supported idea is available, return fewer posts and say why.
6. **Human approval.** A reviewer approves each post individually in the mobile app. The preview is exactly the text sent to Publora; internal artifact metadata is never part of the preview. The reviewer confirms the uniqueness note and rejects near-duplicates.
7. **Schedule.** Add only approved posts to Publora’s calendar for their assigned dates and times. Before inserting, check that date and slot are not already covered by an approved or scheduled post. Keep a one-post buffer in the queue so a missed run does not create a silent day.
8. **Engage.** During each engagement block, respond to inbound comments first, then add value to up to five relevant public conversations. Do not argue, make commitments, give support decisions, collect personal data, or send unsolicited DMs.
9. **Measure.** Record impressions, reactions, comments, profile/page visits, follows, link clicks when available, qualified conversations, and any human corrections. Review weekly and adjust the next week’s lanes.

## Recent-post ledger and uniqueness gate

Maintain `Research/LinkedIn Content Ledger.md` with each post’s publish date, scheduled date/slot, exact approved copy, topic, audience pain, format, CTA, and status (draft, approved, scheduled, published, or deleted). Check it before every batch and update it when a draft is approved, scheduled, published, or deleted. Keep deleted copy in the ledger so it cannot be accidentally regenerated. Compare against the previous 90 days and all future scheduled/approved posts. Similarity means the central idea, claim, or intended takeaway is substantially the same, even when wording differs. Each new post must bring a distinct takeaway; if that cannot be shown in one sentence, it does not pass.

## Approval checklist

- [ ] Exact copy and media reviewed.
- [ ] CovePM LinkedIn Company Page selected.
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
