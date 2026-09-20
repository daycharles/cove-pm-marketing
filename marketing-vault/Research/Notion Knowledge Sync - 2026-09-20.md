# Notion Knowledge Sync — 2026-09-20

---
date: 2026-09-20
type: knowledge-sync
status: reviewed
source_system: Notion — Averion Software
approval_required: false
---

## Scope

Read-only review of the connected Averion Software Notion workspace. This snapshot reconciles
Notion's product, ICP, account-research, pilot, operating-brief, and system-map documentation with
the local marketing vault and the Cove PM product PDF.

## System-of-record reconciliation

| System | Role after review | Local handling |
| --- | --- | --- |
| Notion | Marketing documentation, sales context, research, reusable materials, meeting notes, and knowledge base | Read as strategic context; snapshot durable decisions into this vault |
| GitHub product knowledge base | Durable product implementation truth | Use for capability and limitation verification |
| Linear | Product execution, delivery, and project status | Not connected to this local MVP |
| Local marketing vault | Operational mirror, local task queue, Ollama runs, evidence archive, and approval preparation | Current local automation source of truth; no bidirectional sync yet |
| Zoho Mail bridge | Approved mailbox read/draft path; outbound email approval queue by default | Not used by local runner; no messages sent |

The local vault and Notion are not yet automatically synchronized. This file is the current manual
reconciliation point; it does not claim that a live Notion webhook or bidirectional sync exists.

## Canonical decisions confirmed

1. **Primary ICP:** housing authorities and affordable-housing operators, still provisional but now
   supported by the product page, ICP task, HACLA validation research, and product PDF.
2. **P1 workflow:** inspection workflow + make-ready / field coordination. Keep “inspection routing”
   qualified until route optimization or dispatch behavior is product-owner confirmed.
3. **P2 workflow:** maintenance/work-order orchestration around intake, assignment, prioritization,
   completion, evidence, follow-up, and reporting.
4. **Core narrative:** inspection findings -> accountable make-ready work -> clear ready state.
5. **Evidence boundary:** no verified customer proof, quantified Cove PM outcomes, named integrations,
   security certifications, SLAs, deployment timelines, or public-sector compliance claims.
6. **Pilot discipline:** one workflow, one measurable baseline, one bounded outcome, and explicit
   implementation/procurement questions before calling an account pilot-ready.

## Notion account map to preserve

The Notion priority-account work is more mature than the original local candidate list. It identifies
six multifamily/affordable accounts and public buyer-role hypotheses:

- **Drucker + Falk** — Priority A / discovery-first; 43,000+ apartment homes and a reachable regional
  scale hypothesis.
- **WinnResidential / WinnCompanies** — Priority A / affordable-housing discovery; active renovation,
  resident-experience, and maintenance signals.
- **Bell Partners** — Priority B / change-trigger research; recent BGO combination and centralized
  operations, IT, procurement, and regional maintenance functions.
- **Greystar** — Priority B / strategic learning account; very large platform and active portfolio
  transition signals; use a regional champion hypothesis, not broad enterprise outreach.
- **Bozzuto Management Company** — Priority B / buyer-map completion; large regional portfolios and
  operations-technology signals.
- **Avenue5 Residential** — Priority C / differentiation test; already promotes AI-powered
  maintenance at select properties, so avoid a generic automation pitch.

The local web-researched public/affordable queue adds NYCHA, CHA, HACLA, PHA, FPI Management, and
National Church Residences. These should be treated as an additional research set, not as replacements
for Notion's Priority A/B accounts.

## Funnel state confirmed by Notion

- 0 outbound marketing emails sent.
- 0 demos booked.
- 0 pilot conversations.
- 0 commercial commitments.
- No account has passed all pilot-readiness gates.
- Public buyer roles were identified for only part of the existing account set.

This is consistent with the local rule: research and drafting may run autonomously; contact selection,
message send, pricing, commitments, and sensitive-data transfer remain approval-gated.

## Operating details to align locally

- Notion's operating brief says the Zoho Mail bridge is connected to `info@averionsoftware.com`, with
  outbound email entering an approval queue by default. The local runner should not assume this
  capability until a deliberate mailbox adapter is implemented and tested.
- The operating brief references weekday runs at 9:00 AM and 12:00 PM Eastern. The local vault still
  documents transitional 7:30 AM / 8:00 AM / 12:30 PM tasks. Treat Notion's schedule as the current
  human workflow reference and the local PowerShell runner as the local automation path until they are
  explicitly unified.
- A later Notion funnel checkpoint says the reporting channel was renamed from `#notion-updates` to
  `#marketing-updates`. Local documentation should use `#marketing-updates` going forward.

## Local actions taken

- Reviewed the key Notion product, ICP, strategy, HACLA validation, priority-account, operating-brief,
  pilot-scorecard, and system-map pages.
- Preserved Notion's six-account priority map alongside the newly researched public/affordable queue.
- Kept the product PDF evidence boundary and unknowns intact.
- Did not edit Notion, send email, create contacts, or make commercial commitments.
- Imported the six Notion priority accounts into `agent-workbench/leads/candidates-notion.json`.
- Ran local Ollama qualification run `20260920T153042Z`; report: `agent-workbench/outputs/lead-qualification-notion.md`.
- Added source provenance fields to local lead records so Notion-sourced hypotheses remain traceable.

## Recommended next sync

1. Add the six Notion priority accounts to the local lead database with their existing evidence and
   buyer-role hypotheses.
2. Add a local `source_system` and `notion_page_url` field to lead records so every account can be
   traced back to its source.
3. Reconcile the 9:00 AM / 12:00 PM Notion cadence with the local runner before retiring or changing
   any scheduled task.
4. Prepare two approval-gated discovery drafts: WinnResidential (affordable-housing maintenance
   accountability) and Drucker + Falk (regional maintenance visibility).
