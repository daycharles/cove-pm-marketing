# Next Slice: Lead Pipeline Activation

**Date:** 2026-09-20  
**Status:** Ready for implementation  
**Owner:** Averion Compass marketing OS

## Outcome

Turn the existing company-first lead qualification workflow into a small, repeatable weekly operating loop:

```text
candidate intake -> evidence check -> fit disposition -> human review -> approved next action -> outcome capture
```

The system should help Averion Compass decide who deserves research and attention without discovering personal contacts, sending outreach, publishing content, or changing commercial terms automatically.

## Why this is the next slice

- The local workbench already supports deterministic qualification and evidence gaps.
- Product positioning has been reviewed against the current product material.
- The missing operating layer is a durable queue and feedback loop: what is ready, why it is ready, who must review it, and what happened afterward.

## Scope

### In scope

1. A canonical lead-review record for each company candidate.
2. A weekly queue view grouped by `qualified`, `review`, `nurture`, and `disqualify`.
3. Human approval fields for the next action and evidence threshold.
4. Outcome capture so qualification rules can be evaluated after the first ten reviewed companies.
5. A short weekly operating note generated from the queue.

### Out of scope

- Personal-contact discovery or enrichment.
- Automated email, social, or CRM outreach.
- Website publishing.
- Autonomous qualification overrides.
- Storage of credentials, restricted personal data, or secrets.

## Canonical review record

Each candidate should preserve:

- company name and public website;
- segment, geography, and property-operations signals;
- public evidence URLs with access dates;
- fit score and disposition;
- evidence gaps and confidence;
- proposed next research/action step;
- human reviewer, decision, and decision date;
- final outcome (`advance`, `nurture`, `disqualify`, or `unknown`);
- outcome note and date.

## Review gates

### Gate 1 — Evidence quality

No candidate advances without at least one approved public source supporting the company identity and one source supporting a relevant operational signal. Missing evidence remains visible as a gap.

### Gate 2 — Human decision

The engine may recommend a disposition, but a person must approve the next action. Until then, the record stays in `awaiting-approval`.

### Gate 3 — Outcome feedback

Every reviewed candidate receives an outcome, even if the outcome is `unknown`. This prevents the queue from becoming a one-way list of optimistic guesses.

## Weekly operating rhythm

1. Monday: import or review supplied company candidates.
2. Tuesday: run deterministic qualification and resolve evidence gaps.
3. Wednesday: human review of the qualified/review queue.
4. Thursday: execute only approved research actions.
5. Friday: record outcomes and inspect false-positive/false-negative patterns.

## Definition of done

- A single command can show the current lead queue and counts by disposition.
- Every qualified/review candidate displays evidence, gaps, proposed action, and approval state.
- No action is marked complete without a human decision.
- The first ten reviewed companies can be scored for evidence quality, qualification accuracy, and next-action usefulness.
- The workflow remains local-only and testable without Ollama or external services.

## First implementation tasks

- Add a normalized review/action schema beside the existing lead engine.
- Add queue/status output to the control room.
- Add deterministic tests for approval gating and outcome capture.
- Add a weekly Markdown view under `outputs/`.
- Record the first ten reviewed-company outcomes before changing scoring thresholds.
