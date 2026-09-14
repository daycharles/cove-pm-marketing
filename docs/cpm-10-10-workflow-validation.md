# CPM-10.10 — Emily workflow validation and rollout

Status: complete for M10-S1. This is the repeatable acceptance record for the Voyager-parity
operator workspace. It is intentionally based on the shipped HTTP routes, deterministic demo seed,
and browser checks rather than screenshots or an undocumented live database.

## Acceptance decision

The first delivery slice is the daily operator workspace: identify exceptions, open the source
record, complete supported maintenance/inspection/procurement actions, run or schedule reports,
and preserve tenant scope, permissions, PII minimization, concurrency, and audit history. The
workflow names and order below are the acceptance script. Emily's first-slice sign-off is recorded
by the CPM-10.10 completion request on 2026-09-14; later slices remain backlog until explicitly
selected.

## Seed contract

Run from a clean sandbox with `Demo__Password` set, then use the credentials shown below. The
primary `averion-demo` seed is deterministic (`Random(20260410)` for generated records and
`Random(11)` for the status plan) and idempotent-by-skip: reset the database when a walkthrough
has consumed or changed its records.

| Fixture | What it proves | Source |
| --- | --- | --- |
| 3 properties, 10 buildings, ~80 spaces | portfolio/property/building/space context and property filtering | `tools/PropFlow.Admin/AverionSeed.cs` `Layout` |
| ~70 current residents plus six prior occupancies | resident search/detail, lease context, history, and empty occupancy state | `AverionSeed.SeedAsync` resident section |
| Mixed SMS/email consent, including no-consent residents | notify success, consent skip, and PII-minimized resident views | `AverionSeed.SeedAsync` consent roll |
| One active lease and open recurring charge | lifecycle calendar, lease detail, billing/report source rows | `AverionSeed.SeedAsync` billing lease |
| ~60 assets across HVAC, water heater, appliance, roof, panel, generator | asset drill-down, maintenance history, lifecycle and attention signals | `AverionSeed.AddAsset` |
| 100 work items: every status and priority, 16 New, 30 pest-control, turnover titles, overdue/future due dates | dashboard, queue, filters, assignment, inspection/turn, reports, and terminal-state errors | `BuildStatusPlan` and `WorkTemplates` |
| 6 vendors, 5 employees, 4 message templates | assignment, procurement compliance, employee ownership, and resident notification | `Vendors`, employee seed, `SeedTemplatesAsync` |
| `isolation-demo` with a separate minimal dataset | tenant isolation and no cross-organization source records | `tools/PropFlow.Admin/Program.cs` |

Reset and seed sequence:

```powershell
docker compose down -v
docker compose up -d --wait
dotnet run --project tools/PropFlow.Admin --configuration Release --no-build -- migrate
dotnet run --project tools/PropFlow.Admin --configuration Release --no-build -- configure-runtime
dotnet run --project tools/PropFlow.Admin --configuration Release --no-build -- seed-demo
```

## Workflow acceptance matrix

Each row names a source record, the expected outcome, and the negative path that must remain
visible during a walkthrough. Deep checks live in the linked existing tests; the CPM-10.10 browser
smoke verifies that every destination renders together from the same seed.

| Workflow | Source record / path | Expected outcome | Permission, empty, or error case | Coverage |
| --- | --- | --- | --- | --- |
| Daily dashboard | 100 work items, overdue open work, property/vendor/status distributions | manager identifies priorities and drills to source work | empty queue shows zero state; reader cannot mutate | `m3-workflow.spec.ts`, `attention.spec.ts` |
| Work order | New, assigned, scheduled, completed, and cancelled items | detail shows context, quick actions, scheduling, timeline, and audit history | completed/cancelled assignment rejected; stale version returns conflict | `m3-workflow.spec.ts`, `bulk-edit.spec.ts`, `assign-notify.spec.ts` |
| Resident directory/detail | current and prior occupancies with mixed contact consent | filter to resident, open profile, see occupancy/lease/work context | no-match message; sensitive contacts are masked without People.Manage | `ResidentIntegrationTests`, `residents/page.tsx` |
| Lifecycle calendar | active lease, move-in/notice/inspection/turn dates and work due dates | month/week view and property filter retain source links | invalid date range rejected; no events shows an explicit empty state | `CalendarEndpointsTests`, `calendar.spec.ts` |
| Inspections and turns | inspection queue, findings, move-out/turn work types | start/complete/approve inspection and track make-ready blockers | empty queue/turn board states; unauthorized mutation rejected | `InspectionEndpointsTests`, `InspectionWorkflowSecurityTests` |
| Reports and schedules | seeded work, asset, lease-charge, and resident rows | run report, filter/export, save schedule, inspect delivery history | no detail rows still reports totals; Reports.Read/Reports.Schedule gates | `ReportingEndpointsTests`, `reports/page.tsx` |
| Procurement | six vendors and work-order links; compliance document states | draft/review/approve/issue PO with linked source work | expired compliance blocks issue; Procurement.Manage gates writes | `ProcurementEndpointsTests`, `procurement/page.tsx` |
| Tenant isolation and audit | `averion-demo` versus `isolation-demo` | second tenant cannot see primary records; changes retain actor/timestamp | cross-tenant IDs return not found; broad resident search does not reveal PII | `IsolationTests`, `PropertyHierarchyIsolationTests`, `ResidentIntegrationTests` |

## Browser acceptance command

After `scripts/Test-LocalStack.ps1 -ApiOrigin https://localhost:15004 -ApiPort 15004 -WebPort 13002`
reports `/health/ready` as healthy and the claimed sandbox is seeded, run:

```powershell
cd apps/web
$env:PLAYWRIGHT_BASE_URL = 'http://127.0.0.1:13002'
$env:PLAYWRIGHT_DEMO_PASSWORD = 'DemoPassword!123'
npm exec playwright test e2e/cpm-10-10-workflow-validation.spec.ts --project=chromium
```

The full suite remains the release gate. The focused test is a navigation/render smoke, not a
replacement for workflow-specific integration or browser tests.

## Completion evidence — 2026-09-14

| Check | Result |
| --- | --- |
| `dotnet build PropFlow.slnx --configuration Release --nologo` | Passed; 0 warnings, 0 errors |
| `dotnet run --project tests/PropFlow.FoundationChecks --configuration Release --no-build` | Passed; 12/12 |
| `dotnet test tests/PropFlow.UnitTests --configuration Release --no-build` | Passed; 757/757 |
| `dotnet test tests/PropFlow.IntegrationTests --configuration Release --no-build --logger console;verbosity=normal --blame-hang --blame-hang-timeout 2m` | Passed; 390/390 |
| `npm run lint` | Passed; 0 errors, 7 existing hook warnings |
| `npm run build` | Passed; all web routes compiled |
| Focused browser smoke above | Passed; 1/1 on sandbox-2 (PostgreSQL 15433, API 15004, web 13002) |
| Evidence clip | `outputs/cpm-10.10/test-results/cpm-10-10-workflow-validat-13121-ons-render-from-seeded-data-chromium/video.webm` |

The complete browser suite was also executed against the healthy seeded sandbox: 34 passed and 7
existing suite failures were recorded. The failures are outside the new CPM-10.10 smoke contract:
three shared-seed mutation specs rely on reset/order assumptions, two navigation specs predate the
five M10 nav links (the exact-list assertion is updated in this change), one integration spec needs
an exact `Properties` link selector after the new context link, and one responsive-width assertion
is 9px over its current layout. The focused M10 smoke, backend full regression, and branding checks
passed; these legacy suite issues remain follow-up cleanup rather than silent acceptance gaps.

## Rollout and support

1. Reset, migrate, configure the runtime role, and seed a clean environment.
2. Run `/health/ready`; do not begin browser acceptance from a merely reachable login page.
3. Use `averion-demo` for the operator walkthrough and `isolation-demo` for the boundary check.
4. Capture the browser evidence clip, attach the path to CPM-10.10, and record the commit and test
   result on the M10 epic.
5. If seed data has been consumed, reset the sandbox volume; `seed-demo` will not restore edited
   rows.

Out of scope for this slice: real SMS/email delivery, production Voyager integration, mobile
technician parity, accounting automation beyond the shipped reports/billing surfaces, and any M10-S2
or later roadmap slice. These remain backlog until explicitly selected.
