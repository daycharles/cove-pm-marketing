# Averion Compass: Data Migration & Pilot Readiness

**Audience:** Sales, marketing, solutions, implementation, and product
**Status:** Recommended operating model
**Prepared:** 2026-09-25
**Product:** Averion Compass
**Source:** Imported from `2026-09-09/here-x20/docs/marketing-data-migration-pilot.md`; review current product, security, and legal posture before making external commitments.

## Executive recommendation

Averion Compass should launch a **CSV-first, assisted-migration motion**, then add direct connectors only where repeated customer demand justifies the investment. This lets prospects bring a bounded slice of their current property data into Compass without requiring a complete system replacement before they can evaluate the product.

## How prospects can provide data

| Source | Likely path | Implication for Compass |
| --- | --- | --- |
| AppFolio | Secure exports, reports, or enterprise API access | Confirm plan entitlement and export scope before promising a connector. [AppFolio data access](https://www.appfolio.com/property-manager/accounting-reporting) |
| Buildium | Customer-created API keys, including read-only restrictions; qualifying subscription required | API-assisted migration is practical when the customer can enable access. [Buildium API](https://developer.buildium.com/) |
| Yardi Voyager | Interface APIs for property, unit, lease, rent-roll, resident, vendor, and payables data | Expect interface-partner or customer-admin involvement. [Yardi interfaces](https://www.yardi.com/company/find-an-interface-partner/) |
| Other systems | CSV/XLSX exports, scheduled reports, vendor-assisted extracts, or custom exports | The standard import templates should be the default path. |

Availability varies by vendor edition, contract, permissions, and customer relationship. We should not promise that an incumbent can provide every field or attachment.

## Three pilot lanes

### Lane A — Demo data

Use Compass seed/demo data when the prospect is evaluating workflows, security posture, or navigation and has not authorized a data transfer.

### Lane B — Sanitized operational slice

Request a small, anonymized package:

- Property, building, and unit roster
- Open work orders
- 60–90 days of recent work history
- Vendors and technicians
- Categories, priorities, and statuses
- Optional sample attachments

Use fake or pseudonymized resident values unless resident-facing functionality is part of the pilot.

### Lane C — Authorized production slice

Import one to three properties or another clearly bounded portfolio:

- Current properties and units
- Active work orders
- Required residents and occupancy records
- Vendors and assignments
- Selected historical work orders
- Leases or notices only when required by the pilot scope

Do not request SSNs, bank details, payment-card data, or unrelated accounting history for an operations pilot.

## Migration workflow

1. **Discover:** Identify the source system, edition, export/API options, data owner, modules, and retention needs.
2. **Export:** Prefer a customer-controlled read-only API, then native CSV/XLSX, then a vendor-assisted extract. Treat custom database extracts as an exception.
3. **Transfer securely:** Use an authenticated upload portal, SFTP, or customer-controlled cloud bucket. Do not accept sensitive exports through ordinary email.
4. **Stage and map:** Preserve source values and map properties, buildings, spaces, residents, vendors, employees, work statuses, priorities, and categories into Compass.
5. **Validate:** Report source count vs. imported count, unmapped values, duplicates, missing required fields, invalid dates/relationships, and untransferred attachments.
6. **Pilot import:** Load into a tenant-isolated, reversible pilot environment.
7. **Customer acceptance:** Have the customer verify units, open work, vendors, resident relationships, and key totals.
8. **Cutover:** For a full switch, run a final delta export after a short source-system freeze and retain the original export plus the migration audit record.

## First product capability to build

### Import foundation

- Standard templates for properties/units, vendors/employees, residents/occupancy, work orders, categories/statuses, and optional leases/attachments
- Staging area with preview before commit
- Configurable field/value mapping
- Duplicate detection and required-field validation
- Import history, audit trail, and retryable failed rows
- Customer-facing validation report

### Secure intake

- Expiring upload links
- Role-restricted access
- Encryption in transit and at rest
- Audit logging
- Defined retention and automatic cleanup
- File type, size, and malware checks before processing

The source project describes an integration-adapter abstraction, but says it has no live production PMS integrations and does not yet reconcile external records into the core domain tables. The staged import foundation is therefore the most reusable next step. See the [source project README](../../../2026-09-09/here-x20/README.md).

## Customer-facing positioning

> Bring a small, secure slice of your existing property data into Compass. We’ll map your properties, units, vendors, and active work, validate the results with your team, and expand only after the pilot is confirmed.

## Proposed pilot acceptance criteria

These are internal targets, not customer-facing guarantees:

- Customer can trace every imported record back to its source row.
- No unresolved critical mapping errors remain before acceptance.
- Property/unit and active-work counts reconcile with the source export.
- Pilot users can complete the agreed workflows without returning to the incumbent system for the pilot scope.
- Customer approves the validation report before additional data is imported.

## Security and privacy guardrails

- Collect only data required for the stated pilot purpose.
- Prefer synthetic or pseudonymized resident data.
- Use read-only API credentials whenever possible.
- Do not request credentials through email.
- Encrypt sensitive data during transmission and storage.
- Define retention, deletion, and customer authorization terms with counsel before handling production resident data.

FTC guidance recommends encryption during transmission and storage and collecting only information necessary for the business purpose. NIST’s Privacy Framework emphasizes governance for data transfer, access, retention, and deletion. OWASP recommends allowlisted file types, size limits, generated filenames, authorization checks, and malware scanning for uploads.

## Next checkpoint

Product and implementation should select one representative export format and define the first Compass import schema. Sales should use Lane A or Lane B by default; Lane C should require an explicit customer authorization and a documented pilot scope.

## Sources

- [AppFolio — Property management accounting and reporting](https://www.appfolio.com/property-manager/accounting-reporting)
- [Buildium — Open API documentation](https://developer.buildium.com/)
- [Yardi — Find an interface partner](https://www.yardi.com/company/find-an-interface-partner/)
- [NIST — Privacy Framework](https://www.nist.gov/privacy-framework)
- [FTC — Start with Security](https://www.ftc.gov/business-guidance/resources/start-security-guide-business)
- [OWASP — File Upload Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html)
