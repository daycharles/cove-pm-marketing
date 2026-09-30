# Private Operator RFP Readiness Audit — 2026-09-30

**Audience:** Averion Software marketing, sales, product, security, and implementation  
**Status:** Internal readiness audit; not an RFP response or customer commitment  
**Scope:** Local approved positioning and research records reviewed on 2026-09-30. No external outreach or product-owner verification was performed for this audit.

## Readiness summary

Compass has a narrow, defensible workflow story for private multifamily discovery: inspection findings, assigned follow-up work, and a unit moving through review and sign-off. The materials do not yet support a complete enterprise RFP response. Security posture, current integration behavior, delivery/implementation commitments, support terms, customer references, and measured outcomes need evidence from their owners before answers are submitted.

**Engineering-assessment update (2026-09-30):** The supplied engineering assessment reports v1.0.0-rc.10 as a prerelease, R1 acceptance as open, and no live production demo or recorded workflow acceptance. Treat feature claims as candidate behavior unless release-accepted production evidence is supplied. See [[Private Operator RFP Engineering Readiness Assessment - 2026-09-30]]. The linked GitHub/Linear sources could not be independently retrieved in this workspace update and should be rechecked before external use.

Use this audit to assemble an answer library and identify owners. Treat every unknown as **TBD pending evidence**, not as a “yes.” Do not describe Compass as generally available or production-accepted based on candidate documentation.

## Claims currently safe to use

| Topic | Sourced wording or fact | Boundary |
|---|---|---|
| Company and product | “Averion Compass is developed by Averion Software LLC, a Virginia company.” | Formation documents establish the LLC only; they do not establish licensing, insurance, certification, customer experience, or product results. See [Company Identity](../Strategy/Company%20Identity.md). |
| Product wedge | Compass helps property teams manage inspections and unit turns. Teams can record inspection findings, assign follow-up work, and track a unit through review and sign-off. | This is the approved current positioning. Do not expand it into claims about integrations, compliance, AI, or a full property-management suite. See [Positioning](../Strategy/Positioning.md). |
| Workflow narrative | Inspection findings → accountable follow-up work → review/sign-off / clear ready state. | Frame as product workflow, not a guaranteed operational outcome or quantified improvement. See [Product PDF Review](Averion%20Compass%20Product%20PDF%20Review%20-%202026-09-20.md). |
| Product evidence, for internal validation | A product-PDF review summarizes work assignment, scheduling, bulk actions, timelines, attachments, messages, audit history, tenant-scoped reports, CSV export, and scheduled delivery as implementation-backed surfaces in that PDF. | The source PDF is a marketing input, not a complete or current release source. Product owner must confirm each exact surface, roles, limits, and current availability before it enters an external answer. |
| Segment | Lean multifamily operators are a candidate discovery audience; roughly 50–500 units is explicitly a research hypothesis. | Not a validated ICP, qualification rule, or claim of fit. See [Market Gaps and Competitive Review](Averion%20Compass%20Market%20Gaps%20and%20Competitive%20Review%20-%202026-09-25.md). |

## RFP answer-pack structure and current evidence status

| Section | Prepare | Current status / owner evidence needed |
|---|---|---|
| Company profile | Legal entity name, product relationship, business address and authorized signatory as appropriate. | Entity name/state are documented. Confirm address, signatory, tax forms, insurance, business licenses, and requested vendor onboarding documents with operations/legal. Never expose private registration data in public materials. |
| Product and scope | Supported workflows, user roles, screenshots/demo path, exclusions, roadmap handling. | Use the narrow positioning above. Product must verify current release behavior and scope. Exclude roadmap items unless clearly labeled and formally approved. |
| Security and privacy | Architecture/data-flow diagram; hosting and data locations; authentication and access controls; tenant isolation; encryption; logging; backup/recovery; vulnerability management; incident response; retention/deletion; subprocessors; privacy terms; security contacts; certifications and audit reports if any. | **Material gap.** Reviewed marketing sources explicitly say security certifications and public-sector compliance are not safe claims; no current evidence pack was present. Security/product/legal must answer each control from current evidence. Do not imply SOC 2, ISO, encryption, residency, SLA, or compliance without substantiation. |
| Integrations and data | Supported integrations by edition, direction, objects, sync cadence, failure handling, API docs, import/export formats, and customer prerequisites. | **Material gap.** No named production PMS connector is safe to claim. CSV/XLSX-first assisted migration is a recommended operating model, not proof that the import product/control is implemented. Confirm current export, import, API, and secure-transfer capabilities. See [Data Migration and Pilot Readiness](../Plans/Averion%20Compass%20Data%20Migration%20and%20Pilot%20Readiness.md). |
| Implementation and migration | Discovery, data mapping, validation, configuration, training, cutover, acceptance, rollback, dependencies, customer responsibilities, and duration. | Existing migration flow and pilot lanes are **proposals**. No verified delivery timeline, staffing capacity, production migration proof, support model, or implementation commitment is recorded. Have implementation/product validate before answering. |
| Support and service levels | Coverage hours, channels, response targets, severity definitions, escalation, uptime/maintenance, RTO/RPO, and service credits if offered. | **Unknown.** No approved service levels, support policy, uptime evidence, or DR targets found. Obtain owner-approved terms; avoid “24/7,” uptime, or response-time claims. |
| Outcomes and references | Customer references, case studies, adoption, quantified results, benchmark methodology. | **No verified customer proof or pilot results** recorded. Pilot metrics template is unscheduled and has no customer, baseline, or results. See [Pilot Measurement Plan](../Strategy/Pilot%20Measurement%20Plan.md). |
| Commercial and legal | Pricing basis, licensing, term, renewal, taxes, data processing agreement, IP, liability, termination, and procurement forms. | **Unknown / approval required.** Pricing research or hypotheses are not approved offers. Obtain company/legal approval for requested terms. |

## Explicit exclusions until evidence is approved

- Production AppFolio, Buildium, Yardi, or other PMS connectors; complete import capability; or real-time synchronization.
- Route optimization, dispatch behavior, production AI/autonomous outcomes, offline mobile use, or a complete mobile field workflow.
- NSPIRE/HUD compliance, “HUD approved,” regulatory reporting, or compliance guarantees.
- Security certifications, public-sector readiness, SLAs, deployment timelines, insurance, or a named hosting/data-residency posture.
- Faster turns, reduced costs, improved performance, customer adoption, quantified ROI, or case-study outcomes.
- Full property-management replacement, accounting, leasing, payments, or resident/vendor communications beyond specific features verified in the current release.

These guardrails are consistent with the [Product PDF Review](Averion%20Compass%20Product%20PDF%20Review%20-%202026-09-20.md), [Material Harvest](Marketing%20Material%20Harvest%20-%20here-x20%20Docs%20-%202026-09-25.md), and [Market Gaps review](Averion%20Compass%20Market%20Gaps%20and%20Competitive%20Review%20-%202026-09-25.md).

## Recommended completion sequence

1. **Product owner:** produce a current, role-by-role capability matrix for the workflow claims; link release evidence, demo steps, constraints, and exclusions.
2. **Security owner:** complete a control questionnaire from current architecture and policies. Mark unimplemented controls plainly and identify whether any third-party reports or certificates exist.
3. **Engineering/product:** inventory live APIs, imports/exports, connectors, and supported data objects. Separate shipped behavior from planned adapter/import work.
4. **Implementation/operations:** write a bounded pilot and migration runbook with prerequisites, customer responsibilities, acceptance checks, support, and realistic duration only after validating capacity.
5. **Legal/operations:** assemble company/vendor onboarding documents and approved commercial, privacy, and contract positions.
6. **Marketing/sales:** convert the verified answers into a modular response library, with source/date/owner on every answer and a clear “not currently supported” answer for gaps.
7. **Account team:** tailor each private-operator response to the actual RFP, flag mandatory requirements that Compass cannot substantiate, and obtain approval for commitments before submission.

## Sources reviewed

- [Averion Compass Positioning](../Strategy/Positioning.md)
- [Company Identity and Legal Status](../Strategy/Company%20Identity.md)
- [ICP and Personas](../Strategy/ICP%20and%20Personas.md)
- [Averion Compass Product PDF Review — 2026-09-20](Averion%20Compass%20Product%20PDF%20Review%20-%202026-09-20.md)
- [Averion Compass Market Gaps and Competitive Review — 2026-09-25](Averion%20Compass%20Market%20Gaps%20and%20Competitive%20Review%20-%202026-09-25.md)
- [Marketing Material Harvest — 2026-09-25](Marketing%20Material%20Harvest%20-%20here-x20%20Docs%20-%202026-09-25.md)
- [Averion Compass Data Migration and Pilot Readiness](../Plans/Averion%20Compass%20Data%20Migration%20and%20Pilot%20Readiness.md)
- [Averion Compass Pilot Measurement Plan](../Strategy/Pilot%20Measurement%20Plan.md)
