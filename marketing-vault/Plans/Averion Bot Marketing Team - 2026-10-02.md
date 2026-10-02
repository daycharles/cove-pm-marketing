# Averion Bot Marketing Team — 2026-10-02

**Status:** Five Hermes Bot Mode profiles configured for internal research, drafting, and review. On-demand only; no routines, group orchestration, external connectors, or sending authority were enabled.  
**Owner/reviewer:** Averion Software human marketing owner.  
**Source of truth:** [[Dashboard]] and [[Plans/Autonomous Marketing Agent Setup - 2026-10-02]]; the vault records evidence and decisions. The scheduled workbench is a separate checkout and is not replaced by these profiles.

## Roster and boundaries

| Bot (Hermes profile) | Owns | Internal output | Stop before |
| --- | --- | --- | --- |
| `averion-research` | Private multifamily operator and official evaluation-route research | Dated `Research/` note, source URLs, ranking/gaps | Contact or vendor-form submission |
| `averion-rfp` | Product-claim matrix and RFP answer readiness | Internal response draft, evidence classification and owner gaps | RFP submission or commercial/security commitment |
| `averion-content` | Buyer-specific Compass copy, including social drafts if later requested | Exact draft plus claim/source checklist | Publishing or scheduling; social remains deferred |
| `averion-pipeline` | Company-first lead fit and discovery question | Evidence-qualified individual outreach draft and opt-out path | Email/DM, CRM write, calendar booking |
| `averion-review` | Independent claim, source, approval, and metric QA | Specific defects, pass/fail, needed human reviewer | Self-approval or external execution |

The default Hermes profile remains the human-facing coordinator. Bot roles are specialist workstations, not independent approval authorities. They currently share the configured model via the local Hermes installation and use no cloned marketing credentials. Their CLI toolsets exclude browser automation, shell, desktop control, connections, cron, delegation, image generation, TTS, and session search; file and web tools remain available for internal work. Tool restrictions are a defense in depth, not an approval system.

## Handoff for the first lane

1. Give Research one bounded task: reconcile the differing first-wave private-operator rankings and official technology-evaluation routes. Require dated primary sources, target buyer, hypothesis, metric (verified relevant routes), and checkpoint.
2. Have RFP classify any reusable capability or response statement against current release evidence; leave security, integrations, implementation, pricing, outcomes, and production readiness as gaps unless owners substantiate them.
3. If a specific company is evidence-qualified, Pipeline prepares the exact outreach draft and personalization rationale. Content may refine copy without adding facts. Review checks the exact artifact, source freshness, approved CTA, suppression/opt-out requirements, and missing evidence.
4. A human decides whether to submit a specific artifact into the existing approval path. Approval of a draft is not authorization to send; recipient, message, channel, suppression/opt-out state, idempotency, and decision audit must be checked immediately before any external action. No bot may claim a demo, reply, or outcome until observed.
5. Record each meaningful run in a dated `Sessions/` note; update [[Experiments/Experiment Log]] only when a hypothesis/result changes and [[Dashboard]] when a priority or major artifact changes.

## Current checkpoint

- **Baseline:** No verified contact, RFP submission, demo, pilot, or publication is established by creating these bots. Latest workbench counts remain those in [[Sessions/2026-10-02 - Averion Marketing Daily Brief]], not these profiles.
- **Metric:** Number of evidence-qualified official operator technology-evaluation routes and human-reviewed exact artifacts; measure qualified conversations only after actual approved contact.
- **Next checkpoint:** Review the first Research route-reconciliation packet and QA it before asking Pipeline for an outreach draft.
- **Dependencies:** Reconcile the separate scheduled checkout before changing routines. Product/engineering, security, implementation, legal, and leadership owners must validate their RFP claims and terms. Social publishing and live actions remain deferred.
