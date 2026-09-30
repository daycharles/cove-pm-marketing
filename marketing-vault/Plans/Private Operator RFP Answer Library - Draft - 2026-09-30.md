# Private Operator RFP Answer Library — Draft

**As of:** 2026-09-30  
**Status:** Internal working draft; not submission-ready  
**Purpose:** Reusable starting answers for private multifamily software evaluations. Complete every TBD with evidence and owner approval before external use.

**Release caveat:** Engineering-agent assessment reports the latest published release as v1.0.0-rc.10 (prerelease), with R1 go/no-go items unchecked and no recorded production workflow evidence. Therefore the product copy below is candidate positioning; do not answer that these capabilities are available in a current GA production release. See [[Research/Private Operator RFP Engineering Readiness Assessment - 2026-09-30]]. The linked source pages were not independently fetchable in this update and must be rechecked at the current commit before submission.

## Company profile

**Question: Identify the company and product.**  
**Draft answer:** Averion Compass is property-management software developed by Averion Software LLC, a Virginia company.  
**Evidence:** [Company Identity](../Strategy/Company%20Identity.md).  
**TBD before submission:** business address, authorized signatory, tax/vendor forms, insurance, licensing, and any requested financial or corporate qualifications. Do not include private registration data in general marketing materials.

## Product overview

**Question: What does the product do?**  
**Draft answer (use only after release/product validation):** Averion Compass is being developed to help property teams manage inspections and unit turns. The current candidate materials describe recording inspection findings, assigning follow-up work, and tracking units toward review. The evidence reviewed does not establish a generally available production workflow for inspection capture or inspection/unit-turn sign-off.  
**Evidence:** [Positioning](../Strategy/Positioning.md); detailed capability/release caveats in [[Research/Private Operator RFP Engineering Readiness Assessment - 2026-09-30]].  
**TBD before submission:** GA release evidence; product-owner confirmation for current production behavior; supported roles, exact workflow steps, limitations, supported devices, screenshots, and a recorded demonstration.

**Question: How does the product address the stated workflow?**  
**Draft answer:** If a release-accepted workflow is available, an evaluation could trace an inspection finding through follow-up assignment and review using the operator’s agreed workflow. The supplied evidence does not establish a complete unit-turn checklist, inspection-specific approval, or sign-off in production.  
**Boundary:** Do not present this proposed evaluation as an existing end-to-end capability. It does not promise faster turns, cost reduction, compliance, or any other outcome.

## Requirements response format

For each requirement, select one after product-owner review:

- **Supported:** shipped and verified in the current release; cite release evidence and demo step.
- **Partially supported:** state exactly what is supported, the limit, and any manual step.
- **Not supported:** answer plainly; do not substitute roadmap language.
- **Needs validation:** leave open until the named owner supplies evidence.

| RFP section | Current answer | Required evidence / owner |
|---|---|---|
| Release / production availability | Needs validation; supplied assessment reports v1.0.0-rc.10 prerelease and open R1 acceptance | Product/release owner: current GA release, completed go/no-go, accepted workflow evidence, production demo |
| Workflow and product scope | Partially supported in candidate materials; inspection web flow and templates are Backlog; complete turn/review/sign-off not substantiated | Product: current-release capability matrix, roles, screenshots, exclusions, demo steps, acceptance evidence |
| Data import/export and integrations | Candidate supports documented REST/API scope, custom full-snapshot sandbox pull, and report CSV; no named production PMS connector; no incremental sync; full migration unverified | Product/engineering: supported interfaces, objects, direction, sync, limits, failure handling, release state |
| Security and privacy | Needs validation; supplied audit notes open identity-table RLS finding, no established production residency/at-rest encryption, no SSO/MFA evidence, no independent attestation | Security/engineering/legal: architecture, access, isolation, encryption, logging, backup, incident response, retention, subprocessors, privacy terms, reports/certifications if present |
| Hosting and availability | Needs validation; architecture does not establish production provider, region, residency, uptime, or recovery objectives | Engineering: hosting, data location, uptime evidence, recovery objectives, maintenance process |
| Implementation and migration | Needs validation; custom sandbox migration is not evidence of general CSV/PMS migration; no capacity or delivery timeline | Product/implementation: prerequisites, mapping, validation, training, cutover, acceptance, rollback, customer responsibilities, capacity and duration |
| Support and service levels | Needs validation; no approved support model or SLA | Operations: channels, hours, severity definitions, response targets, escalation, service credits if any |
| Customer references and results | No verified references, case studies, or measured customer outcomes are recorded | Sales/leadership: permissioned references and substantiated methodology/results, if available |
| Pricing and contract terms | Needs explicit company/legal approval | Leadership/legal: pricing basis, term, renewal, liability, privacy, IP, termination, discount authority, authorized signer |
| Vendor onboarding | Partially documented legal entity only | Operations/legal: W-9/tax form, address, insurance, licenses, banking, procurement records as requested |

## Security and data questionnaire

Do not answer “yes” by assumption. Record the evidence artifact, accountable owner, verification date, and any exception for each question.

- What data is collected, stored, transmitted, and retained?
- Where is production data hosted and processed?
- How are authentication, authorization, tenant separation, and privileged access managed?
- What encryption is used in transit and at rest?
- What logs, monitoring, backups, recovery tests, and incident procedures exist?
- How are vulnerabilities, patches, dependencies, and third-party subprocessors managed?
- How can customer data be exported, retained, and deleted at termination?
- Which security attestations or independent audit reports are current and available?

For known candidate findings and precise boundaries, use [[Research/Private Operator RFP Engineering Readiness Assessment - 2026-09-30]]. The internal repository audit is not an independent certification.

## Implementation and pilot response

**Draft answer:** A bounded evaluation can be considered after the operator’s workflow, roles, data needs, baseline, success criteria, and responsibilities are understood. Scope, duration, enablement, data handling, and commercial terms require separate validation and approval before they are proposed.

**TBD:** named delivery owner, resourcing/capacity, onboarding method, supported data path, timeline, support model, pilot price, contract terms, and conversion path.

Do not offer a 30-day delivery timeline solely because a pilot issue describes a 30-day package; the supplied assessment found no evidence of delivery capacity or an approved SLA.

## Bid / no-bid gate

Do not submit until:

1. Product confirms every mandatory capability against current release evidence.
2. Security and privacy answers are complete and approved by their owners.
3. Integrations, data transfer, implementation, and support are stated accurately.
4. Required insurance, legal, financial, and vendor qualifications are verified.
5. Commercial and contractual answers are approved by the authorized company owner.
6. The operator's mandatory requirements can be met without roadmap promises or unsupported claims.

If a mandatory requirement remains unverified or unsupported, escalate for a bid/no-bid decision. Never conceal a gap with vague wording.
