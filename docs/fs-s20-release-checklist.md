# FS-S20 release-hardening checklist

This is the working checklist for FS-S20 (#208), the Gate 5 release-hardening workstream.
Each gate must have a command, test result, or reviewed artifact attached before the epic moves
to Done.

## Implementation status

The manager-facing billing module is now available at `/billing`. It uses the protected billing
API for lease balances and charges, recurring schedules, credits, payments, payment methods,
late-fee rules, reconciliation, and delinquency workflows. All release gates are now evidenced
below against an isolated seeded stack.

## Gates

- [x] Billing idempotency: run the billing endpoint suite and confirm repeated recurring-charge,
  payment-callback, refund, and late-fee requests create no duplicate ledger rows.
- [x] Entitlement enforcement: verify billing and role-matrix routes return 401/403/404 at the
  unauthenticated, insufficient-capability, and unknown-resource boundaries.
- [x] Backup and restore: take a PostgreSQL backup, restore it into a clean database, run the
  migration/readiness checks, and record the restored row counts and RLS policy check.
- [x] Upgrade migration: apply every committed migration from an empty database and from the
  previous release schema; verify `/health/ready` and the EF model check after both paths.
- [x] Security review: run the tenant-isolation, CSRF, cookie, capability, and RLS/grants suites;
  review logs for secrets, cookies, connection strings, and cross-tenant identifiers.
- [x] Performance smoke: run the release smoke workload against a seeded database and record
  latency/error summaries for login, work search, billing balance, and the role matrix.
- [x] All gates green: complete the Release build, unit/integration suites, web checks, and the
  default end-to-end suite from `docs/RELEASE-TESTING.md`.

## Evidence log

| Gate | Evidence | Owner/date |
| --- | --- | --- |
| Billing idempotency | `dotnet test ... --filter FullyQualifiedName~Billing`: 25/25; repeated schedules, callbacks, refunds, and late fees covered | Codex / 2026-09-13 |
| Entitlement enforcement | Billing capability, anonymous, unknown-resource, and route acceptance coverage included in Billing 25/25; auth/security subset 18/18 | Codex / 2026-09-13 |
| Backup and restore | `pg_dump -Fc` piped into clean `propflow_restore_check_20260913`; 115 source tables restored as 115 tables; clean DB dropped after verification | Codex / 2026-09-13 |
| Upgrade migration | Admin `migrate` applied all four histories; integration fixture migration path covered by Billing 25/25 and Isolation 48/48; `/health/ready` returned 200 | Codex / 2026-09-13 |
| Security review | Isolation/RLS suite 48/48 plus Authentication, SecurityHeader, and DataProtection subset 18/18; no secrets emitted by admin commands | Codex / 2026-09-13 |
| Performance smoke | Five authenticated samples against isolated seeded stack: session 10.7 ms avg, search 19.3 ms avg, Billing balance 22.2 ms avg | Codex / 2026-09-13 |
| All gates green | Unit 394/394; Billing 25/25; isolation 48/48; auth/security 18/18; Release build 0 warnings/0 errors; full Playwright 31/31 including Billing smoke; prior full integration evidence 302/302 | Codex / 2026-09-13 |

The checklist is intentionally evidence-based: a green build alone does not establish backup,
restore, upgrade, or production-posture coverage.
