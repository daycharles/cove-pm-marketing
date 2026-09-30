# Private Operator RFP Engineering Readiness Assessment

**Date:** 2026-09-30  
**Basis:** Engineering-agent assessment and repository/release links supplied in this conversation. The links could not be fetched independently in this workspace pass; verify the source revision and current state before submitting an RFP response.  
**Status:** Internal evidence map; candidate capabilities only where noted; not a security attestation or customer-facing answer.

## Release status

The supplied evidence identifies [Averion Compass v1.0.0-rc.10](https://github.com/daycharles/AverionCompass/releases/tag/v1.0.0-rc.10) as the latest published release and marks it a prerelease. The [R1 acceptance checklist](https://github.com/daycharles/AverionCompass/blob/main/docs/r1-release-acceptance.md) has go/no-go items unchecked, while the [release tracker](https://github.com/daycharles/AverionCompass/blob/main/docs/r1-release-tracker.md) lists acceptance, QA, documentation, and release notes as Started. No live production demo or recorded end-to-end workflow evidence was identified in the supplied assessment.

**RFP consequence:** Do not claim a current GA/production release. Describe implementation evidence as candidate behavior pending release acceptance and production validation.

## Workflow capability status

| Area | Evidence supplied | Safe classification |
|---|---|---|
| Inspections | [Mobile operations guide](https://github.com/daycharles/AverionCompass/blob/main/docs/mobile-field-operations.md) describes offline checklists/findings, photos, replay, and conflict handling. Separate web inspection and template-configuration issues [AC-297](https://linear.app/averion/issue/AC-297/add-web-workflow-to-record-inspection-findings) and [AC-296](https://linear.app/averion/issue/AC-296/add-web-configuration-screens-for-work-categories-and-inspection) are Backlog. The guide is not production acceptance evidence. | Partially supported; needs production validation |
| Follow-up work | API documents follow-up actions and work items; assignment and updates are described. SMS/email enter an outbox and are not delivered to real recipients in the candidate. | Partially supported; live messaging not supported by evidence |
| Unit turns | Calendar API has move-in/move-out events and unit-turn targets; no evidence of a complete turn checklist, completion review, or sign-off. | Partially supported; complete turn lifecycle not substantiated |
| Review/approvals | Generic approval requests/decisions exist; review states cover Autopilot findings and integration conflicts. Inspection-specific or unit-turn sign-off is not established. | Partially supported |
| Release sign-off | R1 go/no-go checklist remains open. | Production availability not substantiated |

## Roles and access

Documented roles are Organization Admin, Property Manager, Regional Manager, Maintenance Supervisor, Read Only, Technician, Vendor, and Resident Portal. Broad access is described for admins/property managers; narrower work/asset access for regional/maintenance roles; technician/vendor access is bound to assigned work; resident access is scoped to their own information and service requests. See [architecture/capability rules](https://github.com/daycharles/AverionCompass/blob/main/docs/architecture.md) and [communications/portal behavior](https://github.com/daycharles/AverionCompass/blob/main/docs/r1-communications-integrations-guide.md).

Limitations identified: field-role access depends on a linked employee/vendor; some capabilities are granted before their consuming endpoints exist; SSO and MFA were not evidenced. The security audit reports an open medium finding: identity control-plane tables lack row-level security. Do not generalize business-table tenant controls to every table.

## API, data, and integrations

| Mechanism | Supplied evidence | Boundary |
|---|---|---|
| REST API | Tenant-scoped JSON API; request-driven; endpoint auth/validation/conflict behavior documented in [API contract](https://github.com/daycharles/AverionCompass/blob/main/docs/api.md). | No external API support or version-commitment policy identified. |
| Sandbox adapter | Inbound pull of properties, spaces, occupancies, work orders, assets using Averion's JSON contract; manual full-snapshot sync. Mapping issues/conflicts go for review. See [sandbox adapter contract](https://github.com/daycharles/AverionCompass/blob/main/docs/integration-sandbox-contract.md). | No incremental cursor/watermark. Missing arrays mean none and can retire links. This is not a named production PMS connector or general migration proof. |
| Communications | Twilio/SendGrid are configurable; candidate providers are unconfigured and messaging is mocked/outbox-only. | No live SMS/email delivery or delivery guarantee. |
| Reporting export | Supported report projections export as UTF-8 CSV. | Resident-data policy says no resident export endpoint; no scheduled export established. |

No named production PMS, accounting, SAP, or GIS connector is substantiated. The SAP strategy issue says not to claim SAP before an end-to-end test passes: [AC-272](https://linear.app/averion/issue/AC-272/define-sap-eccs4hana-integration-strategy).

## Mobile and browser

The supplied mobile guide documents iPhone iOS 16+ and Android API 29+, assigned-work caching, queued status/notes/photos/inspections, replay, conflict handling, and a 14-day default local cache. [Mobile field acceptance](https://github.com/daycharles/AverionCompass/blob/main/docs/mobile-field-acceptance.md) and [store release runbook](https://github.com/daycharles/AverionCompass/blob/main/docs/mobile-store-release.md) still call for device/release evidence. Browser/version matrix and browser offline mode were not found. Keep mobile documentation separate from web claims.

## Security and privacy

- **Hosting/residency:** ASP.NET API, PostgreSQL, and Next.js architecture is documented; production provider, region, residency, and processing locations are not substantiated.
- **Data:** [Resident data policy](https://github.com/daycharles/AverionCompass/blob/main/docs/sensitive-resident-data.md) describes resident names/contact details, occupancy/lease dates, household relationships, consent, property/work records, operational history, field data, and photos. It says SSNs, birth dates, driver’s license, bank/account details, and several other sensitive fields are not stored.
- **Retention/deletion:** Mobile cache defaults to 14 days and attachment `retainUntil` is described. Complete server retention/deletion schedule is not substantiated.
- **Identity:** Email/password, organization selection, active membership checks, secure HTTPS cookies, antiforgery checks, eight-hour cookie lifetime, role capabilities, five-failure/15-minute lockout, and production login rate limiting are documented. SSO/MFA not evidenced.
- **Tenant controls:** Forced RLS and tenant query/write guards are described for business tables. Open identity-schema RLS finding limits any broader claim.
- **Encryption:** HTTPS and encrypted local mobile storage are documented. Production database/storage at-rest encryption and ingress HTTPS enforcement were not established. Data Protection key persistence is required for Production/Staging; certificate encryption of the key ring is optional.
- **Logs/backups/recovery:** Append-only work history and some profile audit entries are documented; deployment requires backup before migrations. Centralized log retention, tested disaster recovery, and formal RTO/RPO were not substantiated.
- **Incident/vulnerability process:** Mobile device-loss procedure and internal code audit exist. Company-wide incident response, patch deadlines, vulnerability disclosure, and CI `npm audit` were not evidenced; the assessment says `npm audit` is not wired into CI.
- **Subprocessors/attestations:** Twilio and SendGrid are configurable but unconfigured in the candidate. No current subprocessor register, SOC 2/ISO attestation, or independent audit report identified. The repository audit is internal, not independent.

Primary supplied references: [security audit](https://github.com/daycharles/AverionCompass/blob/main/docs/audit-security.md), [architecture](https://github.com/daycharles/AverionCompass/blob/main/docs/architecture.md), [deployment guide](https://github.com/daycharles/AverionCompass/blob/main/docs/DEPLOYMENT.md), [communications/integrations guide](https://github.com/daycharles/AverionCompass/blob/main/docs/r1-communications-integrations-guide.md).

## Implementation, support, outcomes, and commercial terms

The described sandbox migration path uses configured mappings, validation, review, promotion, and re-sync. It is not evidence of a general CSV importer or full PMS/accounting migration. The pilot issue [AC-266](https://linear.app/averion/issue/AC-266/package-a-30-day-cove-pm-pilot-for-smaller-housing-operators) distinguishes operational imports from full PMS/accounting migration. Treat data preparation, mapping review, and acceptance as manual unless a specific supported connector and migration run are demonstrated.

No implementation capacity, standard delivery timeline, approved support hours/channels, response targets, uptime commitment, or recovery targets were substantiated. A 30-day pilot issue does not establish delivery capacity or an SLA. No permissioned references, case studies, or measured outcomes were identified; [AC-270](https://linear.app/averion/issue/AC-270/build-a-referenceable-demo-and-first-customer-evidence-loop) concerns building future reference evidence. Pricing, contract terms, privacy terms, insurance, and authorized signatory remain owner-approval items.

## RFP answer classification and no-bid triggers

**Potentially supportable within a defined scope after release evidence:** tenant-scoped work-management/report functions in the API, report CSV export, mock integration flows, and documented mobile offline behavior subject to device/release acceptance.

**Partially supported:** inspections, follow-up, approvals/review, unit-turn tracking, external API use, and operational data import. State exact behavior and exclusions.

**Not supported by supplied evidence:** live SMS/email delivery in the candidate; named production PMS/accounting/SAP/GIS connectors; incremental sandbox sync; full PMS/accounting migration; complete inspection or unit-turn sign-off workflow.

**Needs validation before answering:** GA production availability, hosting/residency, at-rest encryption, complete retention/deletion, SSO/MFA, subprocessors, independent attestations, incident/patch procedures, support/SLA, RTO/RPO, browser matrix, implementation capacity, customer references.

No-bid/escalate when mandatory requirements include an unsubstantiated GA release, formal security attestations, five permissioned references, HUD/PIC/PHAS reporting, named enterprise integrations, or guaranteed dates/capabilities. Prior qualification examples include [Wichita Falls AC-265](https://linear.app/averion/issue/AC-265/qualify-and-pursue-wichita-falls-housing-authority-rfp-2026-0727) and [San Diego AC-267](https://linear.app/averion/issue/AC-267/qualify-and-pursue-city-of-san-diego-real-estate-software-rfp); use these as risk examples, not current bids.

**Approval gap:** No named final approvers were identified for technical, security, implementation, pricing, or contract answers. Assign accountable owners before any response is submitted. Do not infer approvers from code ownership or issue authorship.
