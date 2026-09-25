# Marketing material review: here-x20 docs

**Reviewed:** September 25, 2026  
**Source folder:** `C:\Users\cd104535\Documents\Codex\2026-09-09\here-x20\docs`  
**Purpose:** Preserve reusable marketing, sales, product-story, and pilot material in Averion’s marketing vault while marking old or unverified claims.

## Added to this vault

- [Averion Compass Data Migration and Pilot Readiness](../Plans/Averion%20Compass%20Data%20Migration%20and%20Pilot%20Readiness.md) — copied from the source project’s September 25 pilot brief. It contains the CSV-first assisted-migration recommendation, three pilot lanes, intake/mapping/validation flow, customer-facing language, and data-handling guardrails.
- [Legacy Capability Statement source files](../Archive/Legacy%20Capability%20Statement%20-%202026-09-16/) — original Word and PDF plus its associated cropped logo, archived for reference. This is a historical Cove PM draft, not approved current copy.

## Reusable marketing material

### A focused company and product story

The capability statement’s useful ideas are: focused workflows, accountable execution, explainable recommendations, pilot-first adoption, and expansion only after value is demonstrated. Its headline and broad platform framing need to be reconciled with Averion’s current product positioning before reuse.

Current product wording already approved in the vault is the safer base: **Averion Compass helps property teams manage inspections and unit turns. Teams can record findings, assign follow-up work, and track a unit through review and sign-off.** See [Averion Compass Positioning](../Strategy/Positioning.md) and [Company Identity](../Strategy/Company%20Identity.md).

### A customer-friendly data migration offer

The September 25 pilot brief gives us a concrete answer to the “Do we have to replace our whole system first?” question:

> Bring a small, secure slice of your existing property data into Compass. We’ll map your properties, units, vendors, and active work, validate the results with your team, and expand only after the pilot is confirmed.

The brief recommends starting with demo data or a sanitized operational slice, then using an explicitly authorized production slice only when a pilot requires it. CSV/XLSX exports are the default initial path; direct integrations vary by provider, product edition, permissions, and customer relationship. Do not promise an AppFolio, Buildium, Yardi, or other live connector on the strength of this document. The source brief itself says current adapters do not establish a live production integration or complete import capability.

### Demo story worth keeping

The September 9 demo script puts bulk vendor assignment at the center: find unassigned work, select multiple items, preview the action, confirm, and show the resulting timeline. It also describes filters and saved views, employee/vendor assignment, scheduled visits, resident messages, and audit history. This is a useful **demo-story hypothesis** because it shows an operator completing work, rather than touring a feature catalog.

Before using this as a current Averion Compass demo, reconcile it with the current release and the M10 workflow record. The old script uses “PropFlow” and “Cove PM” names, seeded vendor labels, and says technician/vendor roles lack a mobile view. Later project material documents additional field-role workflows. Its product and setup details are not current claims by default.

### Competitive product and UX research

The September 13 competitive review supports these product-story principles:

- Lead with the operator’s next action and exceptions, not a long feature list.
- Keep work ownership, due dates, property/unit context, and history together while users move between records.
- Make repeated work batchable; show a preview and result summary.
- Let reports lead directly to the records that need action.
- Use sensible defaults and allow safe customization rather than making teams configure everything first.
- Keep automation explainable, reviewable, and tied to source records.
- Design field screens around completion and evidence capture, not a shrunken desktop table.

These are product-design implications from public competitor pages and selected review summaries. They are not proof that competitors commonly fail at those jobs or that Compass already performs them better. The original review states this limitation and recommends operator interviews and hands-on task comparisons.

## Claim and freshness review

| Source item | What it contributes | Use status |
| --- | --- | --- |
| Averion Software Capability Statement, September 16 | Company headline, broad offering list, design principles, intended segments, and pilot-friendly framing | Archive only. It says “Cove PM,” uses a Cove PM subdomain, and describes a broad connected workspace. Reconcile naming and verify every product claim before republishing. |
| `demo-script.md`, September 9 | Six-minute operator walkthrough; bulk assignment, queue filters, saved views, assignment timeline, and a bulk “assign and notify” flow | Historical sales input. Product name, seeded details, role scope, and some capabilities are stale. Validate against today’s release before use. |
| `property-management-ui-competitive-research.md`, September 13 | Competitor comparison and interface recommendations | Research input, not current marketing copy. Review dates and vendor offerings need refresh before publishing externally. |
| `cpm-10-10-workflow-validation.md`, September 14 | Evidence of a tested operator-workspace slice, including work, inspections/turns, reports, procurement, resident context, permissions, and tenant separation | Internal proof reference only. Its recorded tests and seed data do not establish customer adoption, deployment readiness, or outcomes. Verify the current release before selecting public claims. |
| `full-suite-scope.md`, updated through September 24 | Detailed engineering scope, completion notes, and known UI/API gaps | Product-owner reference only. A landed API, model, or test does not prove the complete user-facing workflow is available. |
| `marketing-data-migration-pilot.md`, September 25 | Assisted migration story, pilot data boundaries, workflow, and security questions | Added to `Plans/`. Treat it as a recommended operating model; implementation, security, legal, and vendor-specific details still need confirmation. |

### Claims not to lift from the older capability statement without fresh confirmation

- Autopilot/AI recommendations as an active customer-facing capability.
- Technician/inspector mobile readiness or offline field use.
- Production integrations with property-management systems.
- Broad promises about faster decisions, improved performance, or accountable outcomes.
- Public-sector readiness, certification, compliance, security assurance, or customer proof.
- Full delivery of resident and vendor communications.

Some underlying software surfaces and security controls may exist, but external claims require current product, security, and deployment evidence. The marketing vault’s existing product notes remain the approved reference until they are updated with reviewed evidence.

## Source files reviewed

Marketing and sales material:

- `Averion_Software_Capability_Statement.docx` and `.pdf`
- `demo-script.md`
- `marketing-data-migration-pilot.md`
- `property-management-ui-competitive-research.md`
- `cpm-10-10-workflow-validation.md`

Product-claim and context checks:

- `full-suite-scope.md`
- `api.md` (selected route and delivery notes)
- Existing vault references: `Strategy/Positioning.md`, `Strategy/Company Identity.md`, and `Brand/Brand Guide.md`.

Engineering audits, release instructions, security implementation detail, raw backlog, local setup, and user-data handling docs were not copied as marketing material. They remain in the source project and should only be consulted when a product, security, deployment, or procurement question requires them.

## Source locations

- [Source capability statement (DOCX)](../../../2026-09-09/here-x20/docs/Averion_Software_Capability_Statement.docx)
- [Source capability statement (PDF)](../../../2026-09-09/here-x20/docs/Averion_Software_Capability_Statement.pdf)
- [Source demo script](../../../2026-09-09/here-x20/docs/demo-script.md)
- [Source migration and pilot brief](../../../2026-09-09/here-x20/docs/marketing-data-migration-pilot.md)
- [Source competitive UI research](../../../2026-09-09/here-x20/docs/property-management-ui-competitive-research.md)
- [Source M10 workflow validation](../../../2026-09-09/here-x20/docs/cpm-10-10-workflow-validation.md)
- [Source product scope](../../../2026-09-09/here-x20/docs/full-suite-scope.md)
