# Sensitive resident data boundary

This policy is the M10.09 decision record for resident and military-specific fields. It is the
source of truth until an Emily-approved product requirement changes it.

## Data-minimization decisions

| Field | Decision and purpose | Owner | Retention | Authorized access |
| --- | --- | --- | --- | --- |
| Full name | Required to identify the resident and link occupancy, lease, household, and work records | People / property manager | Life of the resident record plus the existing account-retention schedule | Any tenant-scoped user with `Work.Read` |
| Email and phone | Required for resident contact and consent-aware communications; masked by default | People / property manager | Life of the resident record plus the existing account-retention schedule | `People.Manage` sees the value; other `Work.Read` users receive a masked projection |
| SMS/email consent | Required to prevent unauthorized resident communication and to show the current consent state | People / property manager | Life of the consent history | `Work.Read`; changes require `People.Manage` |
| Occupancy, lease, unit, notice, and move-out dates | Required for operational scheduling and resident lifecycle work | Leasing / property manager | Life of the related operational record | Tenant-scoped users with the relevant read capability |
| Household member name and relationship | Required only to understand the household relationship when servicing a resident | People / property manager | Life of the resident relationship | `Work.Read`; contact detail remains masked without `People.Manage` |
| SSN / SSN last four, date of birth, driver's licence, pay grade, marital status, dual-military-spouse, rental type, MAC dates, bank/account details | Not collected or stored. These fields have no approved current operational purpose; provider-hosted screening handles bureau inputs and PropFlow keeps only the minimum verdict projection | Product + security | Not applicable | No role receives a value that PropFlow does not store |

## Access and audit rules

- All resident reads are tenant-scoped by the operations RLS policy and verified session context.
- Broad resident list and directory responses never return unmasked contact values without
  `People.Manage`. Users without that capability cannot use email or phone as a directory search
  key, preventing sensitive records from being discoverable through broad search.
- A consolidated resident profile read records `SensitiveResidentProfileViewed` in the append-only
  identity audit log with the actor, resident target, UTC timestamp, and purpose. Audit entries do
  not contain contact values.
- There is no resident export endpoint. Any future export must use the masked directory projection
  by default, require `People.Manage` to include contact fields, and record the export actor,
  filters, purpose, and row count in the audit log.
- The UI must show masked values as unavailable rather than implying that a masked suffix is a
  verified identifier. New sensitive fields require an explicit purpose, owner, retention rule,
  capability, audit event, and negative-authorization test before implementation.
