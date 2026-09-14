# PropFlow backlog

Epics and tasks for the remaining roadmap (milestones 3–9 from [milestones.md](milestones.md)).
Milestones 1 and 2 (repository/foundation, tenant-aware identity + PostgreSQL persistence) are
implemented. **Milestones 3, 4, 5, 6 and 7 are complete and on `main`** — every task issue
`#6`–`#73` is closed, as is every epic issue `#1`–`#5` (PF-4.06's atomicity half is carried
forward as a follow-up, not an open issue). Milestone 6 was built in parallel with milestone 5,
so milestone number is not milestone order; milestone 7 is the cross-cutting deployment and
hardening track. The delivered M3–M7 roadmap is complete; remaining work is tracked in the FS
successor roadmap, [followups.md](followups.md), the M8 Autopilot milestone, and the new M9
Native Field Apps milestone. This document is the source for GitHub milestones and issues.

Counts here drift; re-measure with `gh issue list --state all` rather than propagating them.

## How this maps to GitHub

- Each **epic** below becomes one GitHub **milestone** (`M3` … `M9`) and one tracking **issue**
  labelled `epic`.
- Each **task** (`PF-3.01`, …) becomes one **issue**, assigned to the epic's milestone, linked
  from the epic issue's checklist.
- Suggested labels: `epic`, `area:web`, `area:api`, `area:application`, `area:domain`,
  `area:infra`, `area:tests`, `area:ci`, `type:feature`, `type:chore`, `type:test`.
- Estimates: `S` ≈ ≤1 day, `M` ≈ 2–3 days, `L` ≈ 4–8 days, `XL` needs a split before starting.
- **✅ in the Task cell means the GitHub issue is closed.** It is a binary marker and nothing more;
  where the truth needs a qualifier — a carve-out, a caveat, work carried forward — that lives in
  the epic's `Progress:` prose or in the row text, not in the glyph. PF-3.27 is the one ✅ with no
  GitHub issue: it shipped after the M3 issues were cut.

The full closed/open split per milestone is in [milestones.md](milestones.md). To re-derive it:
`gh issue list --state all --limit 200 --json number,title,state,milestone`.

Task IDs are stable references; do not renumber. Add new tasks with the next free number.

---

## Epic M3 — First usable vertical slice

**Milestone:** `M3`
**Goal:** Login → open Work → multi-select → assign vendor → confirm → see updated timeline,
running end to end through a real Next.js UI against persisted property/vendor/work data.
**Definition of done:** the demo workflow above works in a browser against seeded data;
integration + e2e tests cover the success and failure paths; CI builds and tests the web app.

**Progress:** the slice is delivered end to end. Every task row below is ✅ — issues `#6`–`#31`
(PF-3.01–PF-3.26) are closed and the slice was promoted to `main` in `b31b864`. **PF-3.27** shipped
in `baf4498` (PR #98) with no tracker issue, because it was written after the M3 issues were cut.
Employee assignment (`POST /api/work/{id}/employee`, `Work.AssignEmployee`) also landed with the
slice although no task numbered it.

Carried forward rather than closed, tracked in [followups.md](followups.md): Tailwind is declared
(`apps/web/package.json:24`) and imported (`apps/web/app/styles.css:1`) but nothing processes it —
no PostCSS config, no `@tailwindcss/postcss` — and the `M3Operations` migration is not safe against
a populated milestone-2 database. The third carry-forward, the half-finished `TimelineEntry`
generalization, was closed by PF-3.10 in `c362bfd` (PR #107).

| ID | Task | Area | Est | Depends on |
| --- | --- | --- | --- | --- |
| PF-3.01 | ✅ Scaffold `apps/web` Next.js app (TypeScript, App Router, Tailwind, ESLint/Prettier, env config) | web | M | — |
| PF-3.02 | ✅ API client + session layer in web: CSRF token fetch/refresh, `no-store`, 401/403 handling, cookie passthrough | web | M | PF-3.01 |
| PF-3.03 | ✅ Auth UI: login page (email / password / organizationId), logout, session bootstrap on load | web | M | PF-3.02 |
| PF-3.04 | ✅ Capability-gated routing/navigation in web (hide/deny routes by `capabilities` from `/api/session`) | web | S | PF-3.03 |
| PF-3.05 | ✅ Domain + persistence: `Portfolio`, `Property`, `Building`, `Unit/Space` tenant entities with the Organization→Portfolio→Property→Building→Unit hierarchy; not apartment-specific | domain | L | — |
| PF-3.06 | ✅ Migration + RLS policies/grants for the property hierarchy tables (follow the per-table RLS rule in architecture.md) | infra | M | PF-3.05 |
| PF-3.07 | ✅ Expand `Vendor` (contact info, trade/category, active flag) and add `Employee`/staff entity; migrations + RLS | domain | M | — |
| PF-3.08 | ✅ Expand `WorkItem`: description, workType, category, priority, status, property/building/unit/resident refs, scheduledStart/End, dueDate, createdDate, completedDate, cost, internal vs resident-visible notes | domain | L | PF-3.05, PF-3.07 |
| PF-3.09 | ✅ Migration + RLS for the expanded work schema; composite tenant FKs for new work relationships | infra | M | PF-3.08 |
| PF-3.10 | ✅ Generalize the timeline: `TimelineEntry` records arbitrary event type, actor, old value, new value, related object refs (not just `VendorAssigned`) | domain | M | PF-3.08 |
| PF-3.11 | ✅ Application use cases: create work, update work (low-risk field edits), with `Work.Create` / `Work.Update` capabilities mapped centrally | application | M | PF-3.08 |
| PF-3.12 | ✅ Emit timeline events for work created, status changed, priority changed, scheduled, vendor assigned, employee assigned | application | M | PF-3.10, PF-3.11 |
| PF-3.13 | ✅ API: work create/update endpoints; vendor list/detail; property-hierarchy read endpoints; employee list — all tenant-scoped, CSRF on mutations | api | M | PF-3.11 |
| PF-3.14 | ✅ API: work list filtering + search (category, status, priority, property/unit, text) with sort and pagination beyond the current top-100 | api | M | PF-3.13 |
| PF-3.15 | ✅ Client-facing concurrency token: expose the work version (xmin) in work responses; require it on updates; 409 on mismatch | api | S | PF-3.13 |
| PF-3.16 | ✅ API: bulk vendor assignment endpoint — bounded batch size, all-or-nothing transaction, per-item concurrency check, one timeline entry per item, `Work.AssignVendor` | api | L | PF-3.15, PF-3.12 |
| PF-3.17 | ✅ Server-side saved views (named filter + column sets, tenant + user scoped): entity, migration, CRUD endpoints | api | M | PF-3.14 |
| PF-3.18 | ✅ Web: Work list view — table, filters, text search, sortable columns, multi-select, bulk-action toolbar | web | L | PF-3.04, PF-3.14 |
| PF-3.19 | ✅ Web: unified Work detail workspace — details, scheduling, vendor, employee, internal notes, timeline; autosave low-risk fields, explicit confirm for consequential actions | web | L | PF-3.13, PF-3.15 |
| PF-3.20 | ✅ Web: bulk "assign vendor" flow — select → assign vendor → confirm → success summary → refreshed timeline | web | M | PF-3.18, PF-3.16 |
| PF-3.21 | ✅ Web: saved views UI (create/apply/delete, default view) | web | S | PF-3.18, PF-3.17 |
| PF-3.22 | ✅ Seed data: two organizations for isolation plus a minimal property/vendor/employee/work dataset covering each status and priority | infra | M | PF-3.09 |
| PF-3.23 | ✅ Integration tests: assignment rollback on audit failure, unauthorized assignment (403), stale update (409), audit entry created, cross-tenant read/write blocked, bulk all-or-nothing | tests | L | PF-3.16 |
| PF-3.24 | ✅ e2e (Playwright): the full vertical-slice workflow against seeded data, in CI | tests | M | PF-3.20, PF-3.22 |
| PF-3.25 | ✅ CI: add web install/lint/build/unit steps and the Playwright job to `.github/workflows/ci.yml` | ci | S | PF-3.01 |
| PF-3.26 | ✅ Update `docs/api.md` and `docs/local-development.md` for the new endpoints and the web dev server | chore | S | PF-3.13 |
| PF-3.27 | ✅ Refuse vendor/employee assignment on `Completed` and `Cancelled` work, and write the demo script | fix | S | PF-3.16, PF-3.20 |

---

## Epic M4 — Polished pest-control workflow

**Milestone:** `M4`
**Goal:** the bulk pest-control demo — filter to a category, select ~12 work orders,
assign a vendor, schedule a window, notify residents, confirm — completes as one fast operation
with a success summary, durable communication dispatch, and a full seeded demo dataset.

**Progress:** all 12 task issues (`#32`–`#43`) are closed and on `main`. That is the
resident/occupancy domain with per-channel consent, the `communications`-schema context with mock
SMS/email senders, the transactional outbox and its at-most-once dispatcher, resident message
templates rendered and queued through `POST /api/work/{id}/message` and folded into the work
timeline, the extended bulk work actions, the web assign &amp; notify flow, the mobile pass, the
Tidewater demo seed, and the demo e2e + outbox/rendering/consent tests. See
[milestones.md](milestones.md) for what each one delivered.

Carry-forward and caveats the ✅ column cannot express: `add tag` is carved out of PF-4.07 pending
the tag-vocabulary decision below; and PF-4.06 closed on its **read-side** half only — outbox rows
carry `WorkId`/`ResidentVisible` and `GET /api/work/{id}/timeline` merges them in, but the
audit-communications C11 atomicity half (enqueue inside the transaction of the *work event* that
triggers it) is tracked in [followups.md](followups.md).

| ID | Task | Area | Est | Depends on |
| --- | --- | --- | --- | --- |
| PF-4.01 | ✅ `Resident`/`Person` domain + occupancy (unit ↔ resident over time); migrations + RLS | domain | L | PF-3.05 |
| PF-4.02 | ✅ Contact channels (SMS/email) with per-channel consent state on residents | domain | M | PF-4.01 |
| PF-4.03 | ✅ Resident message templates: entity + CRUD endpoints + variable substitution. `POST /api/work/{id}/message` renders a template against `resident.name`/`work.*`/`property.name`/`schedule.*` and queues it. | api | M | PF-4.01 |
| PF-4.04 | ✅ Communication provider abstraction with mock SMS + mock email implementations (no real send) | application | M | PF-4.02 |
| PF-4.05 | ✅ Transactional outbox + idempotent dispatch worker for communications | infra | L | PF-4.04 |
| PF-4.06 | ✅ Communication records + delivery status surfaced as timeline entries with explicit visibility. Outbox messages carry `WorkId` + `ResidentVisible`; `GET /api/work/{id}/timeline` folds them in as `MessageQueued`/`Sent`/`Failed` (read-side merge, no cross-context write). The C11 "enqueue atomic with the work event" half is still deferred. | application | M | PF-4.05, PF-3.10 |
| PF-4.07 | ✅ Extend bulk actions — **`add tag` carved out**: `bulk/status`, `bulk/priority`, `bulk/schedule`, `bulk/note`, `bulk/reopen` (bounded ≤100, one transaction, per-item version check, one timeline entry per changed item). `close` = `bulk/status` to `Completed`/`Cancelled`; `reopen` is the sanctioned exit from a terminal state via `WorkItem.Reopen()`. `add tag` waits on the tag-vocabulary decision. | api | L | PF-3.16 |
| PF-4.08 | ✅ Bulk "assign &amp; notify" flow in web: vendor + schedule window + resident message + confirm + success summary. `AssignNotifyFlow` runs `bulk/vendor` → `bulk/schedule` (fresh version re-read) → per-item `POST /api/work/{id}/message`, with a per-step outcome summary (queued / deduped / skipped-no-consent / failed). Seed gains 3 message templates. e2e: `assign-notify.spec.ts`. | web | L | PF-4.07, PF-4.03, PF-3.20 |
| PF-4.09 | ✅ Mobile-responsive Work list and detail; large touch targets for field use. The card-layout table + sidebar collapse + 44px targets landed as the `m4-mobile-prereq` (`1d523b8`); the phone Sort control that replaces the hidden column headers landed separately. `e2e/responsive-work.spec.ts` + `e2e/mobile-sort.spec.ts`. | web | M | PF-3.19 |
| PF-4.10 | ✅ Seed: Tidewater Residential Management — deterministic `TidewaterSeed` (fixed RNG): 3 properties, 10 buildings, ~80 spaces, ~70 current residents with mixed consent, 6 vendors, 5 employees, 6 categories, ~70 assets, 100 work items across every status/priority, 30 pest-control, a third of open items overdue. The isolation tenant keeps the minimal 12-item seed. | infra | M | PF-4.01, PF-3.22 |
| PF-4.11 | ✅ e2e: bulk pest-control assignment + notify demo workflow in CI. `apps/web/e2e/pest-control-demo.spec.ts` — provisions its own pest-control queue, follows the demo-script §3a filter → select → assign &amp; notify → confirm sequence, asserts the summary + the vendor/schedule/message timeline. Runs in the CI `e2e` job. | tests | M | PF-4.08, PF-4.10 |
| PF-4.12 | ✅ Tests: outbox idempotency (no duplicate sends on retry), template rendering, consent respected. Idempotency: `Enqueue_is_idempotent_by_key`, `Retried_dispatch_does_not_send_a_message_twice`, `Competing_dispatchers_claim_a_message_at_most_once`, and the new `A_lost_acknowledgement_retries_without_sending_the_message_twice`. Rendering: `TemplateRendererTests` + `Persisted_template_body_renders_against_supplied_values` + `A_scheduled_visit_renders_in_the_property_time_zone`. Consent: `Sending_is_refused_without_consent_a_resident_or_a_template`. | tests | M | PF-4.05 |

---

## Epic M5 — Events and field workflows

**Milestone:** `M5`
**Goal:** technicians work assigned jobs from a phone (including an "On The Way" status that
notifies the resident), and admins configure simple automation rules instead of hard-wired
workflows.

| ID | Task | Area | Est | Depends on |
| --- | --- | --- | --- | --- |
| PF-5.01 | ✅ Property- and assignment-level authorization scope model (regional / technician / vendor); define before field access ships | application | L | PF-3.04 |
| PF-5.02 | ✅ Grant `Technician` and `Vendor` capabilities scoped to assigned work / assigned property | application | M | PF-5.01 |
| PF-5.03 | ✅ Technician "On The Way" status workflow: status transition, timeline event, triggers resident communication | application | M | PF-4.06, PF-5.02 |
| PF-5.04 | ✅ Automation rules engine: persisted WHEN/IF/THEN rules over domain events, a predefined action set, evaluation on dispatch | application | XL (split) | PF-4.06 |
| PF-5.05 | ✅ Automation rules admin UI (list, create, enable/disable) — predefined triggers/conditions/actions only | web | L | PF-5.04 |
| PF-5.06 | ✅ Notes with explicit visibility (internal vs resident-visible) across API and web, consistently enforced | api | M | PF-3.19 |
| PF-5.07 | ✅ Communication status (queued / sent / delivered / failed) shown inline in the work timeline | web | S | PF-4.06 |
| PF-5.08 | ✅ Remaining authorized bulk actions from the handoff (send resident message, assign employee) wired to the bulk toolbar | api | M | PF-4.07 |
| PF-5.09 | ✅ Technician mobile view: my assigned work, status changes, add note/photo | web | L | PF-5.02, PF-4.09 |
| PF-5.10 | ✅ e2e: technician On The Way demo workflow in CI | tests | M | PF-5.03, PF-5.09 |
| PF-5.11 | ✅ Tests: scope model denies cross-property/cross-assignment access for technician + vendor | tests | M | PF-5.01 |
| PF-5.13 | ✅ Automation rules evaluator. `EfWorkOperations` raises `WorkCreated` / `WorkStatusChanged` after the change commits; `IAutomationEngine` loads the tenant's enabled rules for the trigger, ANDs conditions against the re-read work item, and runs `SetPriority` / `SendResidentMessage` (the latter via the shared `IResidentMessenger`). Each rule is fenced by an `AutomationApplied` timeline entry (id derived from rule + occurrence) for idempotency, runs in its own transaction, and a failure is logged and isolated. `AutomationEngineTests` covers matching / non-matching / disabled / duplicate-delivery / cross-tenant / message-action / failure-isolation and the two API triggers. | application | L | PF-5.04 |
| PF-5.12 | ✅ Expose the remaining authorized bulk actions on the work-list toolbar — a "Bulk edit…" flow (gated by `Work.Update`) that applies status, priority, schedule, resident/internal note or reopen to the selected batch via the existing bounded, all-or-nothing `/api/work/bulk/*` endpoints. Shows changed / unchanged / total on success and treats any rollback (409 / not-assignable) as a failure with nothing changed. `e2e/bulk-edit.spec.ts` covers status (desktop) and note (phone viewport). | web | M | PF-4.07, PF-5.08 |
| PF-5.14 | ✅ `WorkNoteAdded` automation trigger + resident notification on a visible note. A resident-visible note (`POST /api/work/bulk/note` with `internal` unset) fires the trigger with the note text; an internal note never does. A rule may carry a `SendResidentMessage` action whose template can render `{{ note.text }}`; it honors resident channel consent, dedupes through the outbox idempotency key, and records queued / skipped (with reason) on the timeline without exposing internal note text. `EfAutomationEngine` re-reads the committed `WorkNote` timeline entry and refuses a non-resident-visible one. `WorkNoteAdded` domain record added alongside `WorkCreated` / `WorkStatusChanged`. `AutomationEngineTests` adds visibility / consent / deduplication / tenant-isolation / API-path coverage. | application | M | PF-5.06, PF-5.13 |

---

## Epic M6 — Assets and attention

**Milestone:** `M6`
**Goal:** assets carry maintenance history, repeat repairs are flagged automatically, the home
screen is an actionable attention queue rather than a chart wall, global search spans all
entities, and the integration abstraction exists (mocked).

| ID | Task | Area | Est | Depends on |
| --- | --- | --- | --- | --- |
| PF-6.01 | ✅ `Asset` domain: type, manufacturer, model, serial, install date, warranty expiration, expected service life, condition, replacement cost estimate, notes; migrations + RLS; read/write API. Photos deferred to PF-7.01 (attachments). | domain | L | PF-3.06 |
| PF-6.02 | ✅ Link work items to an optional asset; exposed on `POST`/`PUT /api/work` (`assetId`, `null` clears) and the web detail workspace's Details panel. The asset must be at the work's own property; a foreign/mismatched id is a 400. A change writes an `AssetLinked` timeline entry; `WorkItems` gains a `(OrganizationId, AssetId)` index and composite FK to `Assets`. | api | M | PF-6.01, PF-3.13 |
| PF-6.03 | ✅ Asset detail page (`/assets/[id]`) with complete maintenance history. `GET /api/assets/{id}/history` returns the asset + every linked work item newest-first + roll-ups (`workOrderCount`, `totalCost`, `ageInYears`, `underWarranty`). The page shows the asset record, the roll-ups, and a history table linking each work item; reached from the work detail's "View asset maintenance history →" link. | web | M | PF-6.02 |
| PF-6.04 | ✅ Configurable repeat-repair detection. Per-org `RepeatRepairPolicy` (threshold default 3, window default 120 days, category-similarity option), forced RLS, upserted via `GET`/`PUT /api/assets/repeat-repair-policy`. `IRepeatRepairDetector` counts published work linked to an asset within the window (narrowed to a matching category when the option is on); `GET /api/assets/{id}/repeat-repair` returns count, total cost in window and the `isRepeatRepair` flag. | application | M | PF-6.02 |
| PF-6.05 | ✅ "Repeat Repair Warning" surface. `RepeatRepairWarning` component calls `GET /api/assets/{id}/repeat-repair` and, only when `isRepeatRepair`, shows repair count, total repair cost in the window and asset age. Full-width banner on the asset detail page; compact inline variant beside the work detail's asset picker (passes the work's `categoryId`). `ageInYears` added to the assessment response. | web | M | PF-6.04 |
| PF-6.06 | ✅ Attention queue backend. `AttentionRules` (pure domain) evaluates seven rules — unassigned emergency, first-response SLA breach, overdue, waiting-on-vendor, waiting-on-resident, repeat repair, unit-turn-at-risk — over each open work item. `IAttentionQueue` / `EfAttentionQueue` builds the tenant-scoped queue in three queries; `GET /api/attention` returns one item per work item (most urgent first), each carrying every finding it tripped, with per-severity distinct-work counts for PF-6.07's cards — `AttentionQueueBuilder` derives those counts from the same rows and the same `HasSeverity` predicate the cards filter on, so a card cannot disagree with the list. Thresholds fixed in `AttentionThresholds` (not yet org-configurable). | application | L | PF-6.04 |
| PF-6.07 | ✅ "Needs your attention" screen (`/attention`, in primary nav). Reads `GET /api/attention`; three severity cards (Critical / Warning / Informational) show the distinct-work counts and each toggles a filter on the item list below. Every item shows each reason it tripped with that reason's detail, plus property, priority, status and due date, and links to `/work/{id}`. | web | L | PF-6.06 |
| PF-6.08 | ✅ Fuzzy global search across properties, buildings, spaces, residents, vendors, employees, categories, work orders, assets. `GET /api/search` behind `Work.Read`; `pg_trgm` substring + `word_similarity` ranking, GIN trigram indexes, tenant-scoped. UI is PF-6.09. | api | L | PF-6.01 |
| PF-6.09 | ✅ Keyboard-first global search. `CommandSearch` palette mounted in the app shell — opens on `Cmd`/`Ctrl+K` anywhere (or `/` outside a field, or the header "Search" button), debounced calls to `GET /api/search`, `↑`/`↓` to move, `Enter` to open, `Esc` to close. Work → `/work/{id}`, Asset → `/assets/{id}`, Property/Space/Category → the work list filtered by that id (the list now seeds its query from the URL), other types → a work-list text search. | web | M | PF-6.08 |
| PF-6.10 | ✅ Integration adapter abstraction: `IIntegrationAdapter` + canonical Property/Space/Occupancy/WorkOrder/Asset records, `MockIntegrationAdapter`, `integrations` schema (Connections + RecordLinks) with forced RLS, external-id / source-system / last-sync / per-record sync-state tracking, `/api/integrations` (behind `Integrations.Manage`). Reconciling external records into the domain tables is deferred — see `docs/followups.md`. | application | L | PF-6.01 |
| PF-6.11 | ✅ Integration Health screen (`/integrations`, nav-gated by `Integrations.Manage`). One card per connection over `GET /api/integrations`: status (Healthy / Failing / Disabled / Never synced), last successful sync, last attempt, consecutive failures, tracked records, unresolved (failed) count, last error. Actions: **Sync now** (`POST /{id}/sync`), enable/disable, and an expandable record list (`GET /{id}/records`) with per-record sync state. A "Connect a system" form covers the source systems not yet connected. | web | M | PF-6.10 |
| PF-6.12 | ✅ Navigation curation. `apps/web/lib/navigation.ts` is the single source for the header nav — each entry declares the capability its destination needs; `AppShell` renders only what `visibleNav(session)` returns and drops the search control without `Work.Read`. So Work / Needs attention hide for a role with no `Work.Read`, and no "coming soon" or role-inaccessible link ships. Rule documented in `.claude/rules/web.md`. | web | S | PF-6.11 |
| PF-6.13 | ✅ e2e: repeat HVAC repair walkthrough (`apps/web/e2e/repeat-hvac-demo.spec.ts`). Creates an HVAC asset and three costed repairs inside the detection window, then asserts the asset page's repeat-repair warning (count 3) and full maintenance history, the compact warning on a work order, and the `RepeatRepair` row on `/attention` linking back to the work. | tests | M | PF-6.05 |

---

## Epic M7 — Deployment and platform hardening (cross-cutting)

**Milestone:** `M7`
**Goal:** close the production gaps called out in architecture.md. Pull tasks forward into
earlier milestones when a feature forces the issue.

| ID | Task | Area | Est | Depends on |
| --- | --- | --- | --- | --- |
| PF-7.01 | ✅ Attachment/photo storage service. `Attachment` domain entity + forced-RLS `Attachments` table + `IAttachmentStorage` (`LocalAttachmentStorage`, path-traversal guarded). `/api/work/{id}/attachments` upload (25 MB, PDF/JPEG/PNG/WebP/HEIC) / list / stream-download / delete behind `Work.ManageAttachments`, every route narrowed by the field-role scope check so a Technician only touches their own work. `residentVisible` flag; `retainUntil` enforced by `AttachmentRetentionSweep` (per-tenant hosted service, blob + row). Web: attachments panel on the work detail page + a working "Add photo" on the technician view. | infra | L | PF-3.19 |
| PF-7.02 | ✅ Multi-instance readiness: shared encrypted Data Protection keys, trusted proxy / forwarded-headers trust boundary, `Attachments:RootPath` / secret configuration gates, PostgreSQL TLS. `DeploymentConfigurationTests`. | infra | L | — |
| PF-7.03 | ✅ Observability pass: structured logs, end-to-end request trace IDs, `/health/metrics` snapshot. `ObservabilityTests`. | infra | M | — |
| PF-7.04 | ✅ Scheduling model: UTC instants + property IANA time zones, local-time projections rendered in web. | domain | M | PF-3.08 |
| PF-7.05 | ✅ Real communication providers: message consent, signed provider callbacks, retry / retention controls. | application | L | PF-4.05 |
| PF-7.06 | ✅ RLS/grants review checklist enforced in PR template for every new business table | ci | S | — |

---

## Full-suite expansion (FS) — the successor roadmap

`PF-x.yy` closed out with milestone 7. The work that follows it is tracked under **`FS-`** ids,
scoped in [full-suite-scope.md](full-suite-scope.md) and cut into GitHub issues `#184`-`#208`.
`FS-` ids are stable references on the same terms as `PF-` ids: **never renumber them.**

The shape differs from the `PF-` roadmap in one way worth stating, because it is what makes the
board confusing on first contact: **each release gate is split into two GitHub milestones, an
`A` platform track and a `B` workflow track**, so `FS-G2A` and `FS-G2B` are *milestones*, not
issues. A title scan for them finds nothing.

| Gate | Milestone | Track | Slices |
|---|---|---|---|
| 1 — Core PMS foundation | `FS-G1A` — Platform foundation | Developer A | `FS-S01` `#189` identity/orgs/permissions/audit, `FS-S03` `#191` configuration and workflow administration |
| 1 | `FS-G1B` — Core PMS workflows | Developer B | `FS-S02` `#190` portfolio/property, `FS-S04` `#192` marketing/listings/showings, `FS-S06` `#194` leases/renewals/notices, `FS-S07` `#195` resident portal |
| 2 — Financial suite | `FS-G2A` — Financial platform | Developer A | `FS-S09` `#197` general ledger, AP/AR, period close |
| 2 | `FS-G2B` — Financial workflows | Developer B | `FS-S08` `#196` charges/rent/payments/collections, `FS-S10` `#198` budgets/owner accounting/statements |
| 3 — Operations suite | `FS-G3A` — Operations platform | Developer A | `FS-S13` `#201` preventive maintenance/assets, `FS-S15` `#203` compliance/incidents/risk |
| 3 | `FS-G3B` — Operations workflows | Developer B | `FS-S11` `#199` maintenance baseline, `FS-S12` `#200` inspections/turns, `FS-S14` `#202` vendor/procurement |
| 4 — Engagement and ecosystem | `FS-G4A` — Ecosystem platform | Developer A | `FS-S05` `#193` applications/screening, `FS-S19` `#207` real integrations/reconciliation |
| 4 | `FS-G4B` — Engagement and insight | Developer B | `FS-S16` `#204` communications/inbox/announcements, `FS-S17` `#205` documents/templates/e-signature, `FS-S18` `#206` reporting/analytics |
| 5 — Production release | `FS-G5A` — Release engineering | — | `FS-S20` `#208` customer billing and production release hardening |

Epic issues: `FS-E1` `#184`, `FS-E2` `#185`, `FS-E3` `#186`, `FS-E4` `#187`, `FS-E5` `#188`.

### Delivered so far

**`FS-G1B` — Core PMS workflows.** Landed as one branch, `feat/fs-gate1-core`, closing `#190`,
`#192`, `#194`, `#195` and the `FS-E2` epic `#185`. It shipped as a single branch rather than
four because the migration set cannot be split: `20260911160000_FS_S04S06TenantSecurity.cs:11`
secures the `FS-S04` and `FS-S06` tables from one `Tables` array, and every generated
`.Designer.cs` snapshot encodes the whole model at its timestamp while the timestamps of the four
slices interleave. A per-slice split would mean regenerating all fourteen migrations in a new
order, which is a rewrite of working code rather than a split of it.

Delivered: the `Portfolio`/`Property` hierarchy with archive state, property contacts and property
documents; `Listing`/`Inquiry`/`Showing`/`Applicant` with lead source; `Lease`/`LeaseNotice`/
`LeaseParty`/`LeaseDocument`/`LeaseCharge`; `ResidentPayment`, `Announcement` and
`HouseholdMember` with the resident-portal identity binding; the `/properties`, `/marketing`,
`/leasing`, `/portal` and `/announcements` web routes and their Playwright specs.

**`FS-G1A` remains open.** The board marks `FS-S01` `#189` *In progress*, but nothing for it is on
`develop` yet; `FS-S03` `#191` is still `Backlog`.

---

## Epic M8 — CovePM Autopilot

**Milestone:** `M8`
**Goal:** turn CovePM's connected property, leasing, financial, operations, engagement, and
compliance data into a trusted daily operating brief with evidence-backed recommendations and
governed actions. Autopilot is a cross-suite intelligence layer; it does not replace the existing
event-triggered automation rules and does not grant an AI model direct database or mutation access.

**Scope boundary:** existing PMS, financial, operations, engagement, reporting, integration, and
mobile foundations remain owned by the FS roadmap. M8 adds cross-suite findings, explanation,
recommendation, feedback, and controlled action execution. Native/offline mobile delivery is not
assumed by M8; the first mobile surface is a responsive review experience using the existing
field-capable web client.

**Definition of done:** a manager can open a tenant-scoped daily brief, understand each finding's
evidence and impact, approve or reject a recommended action, and see the resulting mutation and
audit trail. Read-only questions return source-linked answers. No Autopilot action can bypass
capabilities, approvals, consent, idempotency, or tenant isolation. Deterministic findings remain
usable when an AI provider is unavailable.

| ID | Task | Area | Est | Depends on |
| --- | --- | --- | --- | --- |
| CPM-8.01 | Autopilot domain model: finding, recommendation, run, action proposal, execution, feedback, confidence, data freshness, lifecycle states, and tenant-scoped append-only audit records | domain | L | FS-S01, FS-S03 |
| CPM-8.02 | Cross-suite signal catalog and deterministic analyzers for SLA risk, stalled work, vendor follow-up, turn risk, lease/payment deadlines, budget variance, invoice exceptions, compliance deadlines, repeat repairs, and asset replacement candidates | application | L | CPM-8.01, FS-S08, FS-S10, FS-S11, FS-S12, FS-S13, FS-S15 |
| CPM-8.03 | Evidence and impact projections: source-record links, calculation inputs, operational/financial impact, confidence, freshness, and a stable explanation contract that never fabricates missing values | application | M | CPM-8.02, FS-S18 |
| CPM-8.04 | Autopilot model gateway: provider abstraction, versioned prompts/instructions, structured output validation, tenant-safe context assembly, redaction/minimization, timeout/fallback behavior, and deterministic operation without a model | infra | L | CPM-8.03, FS-S19 |
| CPM-8.05 | Daily brief API: role/property/portfolio filters, severity and impact ordering, unread/read state, snooze/reopen, source links, recommendation feedback, and bounded pagination | api | M | CPM-8.03, CPM-8.04 |
| CPM-8.06 | Autopilot command center UI: daily brief, finding detail, evidence drawer, impact explanation, approve/edit/reject/snooze controls, empty/error/provider-unavailable states, and capability-gated navigation | web | L | CPM-8.05 |
| CPM-8.07 | Governed action framework: typed action schemas, preview/diff, capability re-check at execution, approval routing, idempotency keys, optimistic concurrency, consent enforcement, and append-only execution history | application | XL (split) | CPM-8.01, FS-S01, FS-S03 |
| CPM-8.08 | Initial action adapters: assign vendor/employee, schedule work, create follow-up, draft resident/vendor communication, request approval, and create purchase-order draft; no autonomous payment, lease, bank, role, or audit mutations | application | L | CPM-8.07, FS-S08, FS-S11, FS-S14, FS-S16 |
| CPM-8.09 | Mobile review surface: responsive Autopilot inbox and action-confirmation flow for property managers/supervisors, with compact finding cards and deep links into field/work records | web | M | CPM-8.06, CPM-8.08 |
| CPM-8.10 | Read-only Ask CovePM: natural-language questions over approved projections only, source-linked answers, time/tenant scope enforcement, refusal for unsupported or unauthorized requests, and prompt-injection handling for resident/vendor text | application/api | L | CPM-8.03, CPM-8.04, CPM-8.05 |
| CPM-8.11 | Autonomy policy administration: Observe/Recommend/Assist/Governed modes by organization, role, property, action, and amount threshold; default-safe settings with full audit | web/application | L | CPM-8.07, CPM-8.08 |
| CPM-8.12 | Evaluation and acceptance suite: seeded daily-brief demo, recommendation-quality fixtures, unauthorized-action tests, cross-tenant tests, duplicate-action tests, provider outage/fallback tests, prompt-injection fixtures, and browser coverage | tests | L | CPM-8.05, CPM-8.06, CPM-8.08, CPM-8.10, CPM-8.11 |
| CPM-8.13 | Operational metrics and outcome feedback: recommendation acceptance, false-positive/false-negative review, hours saved, overdue-work reduction, action latency, provider cost, and model/version traceability | application/infra | M | CPM-8.01, CPM-8.05, CPM-8.12 |
| CPM-8.14 | M8 documentation and demo: Autopilot safety contract, administrator setup, provider configuration, action policy guide, daily brief walkthrough, support runbook, and product metrics dashboard definitions | chore | M | CPM-8.11, CPM-8.13 |

**Recommended first slice:** CPM-8.01–CPM-8.06 plus the deterministic subset of CPM-8.12. Ship the
daily brief before autonomous execution; then add CPM-8.07–CPM-8.11 behind explicit confirmation and
policy controls. The first commercial proof should measure reduction in overdue work, coordinator
follow-up time, missed vendor appointments, and repeat repairs.

**CPM-8.01 delivered.** `PropFlow.Domain.Autopilot` — `AutopilotRun` (Running/Completed/Failed),
`AutopilotFinding` (New/Reviewed/Dismissed/Resolved, reusing `AttentionSeverity` rather than a
second severity vocabulary), `AutopilotRecommendation` and `AutopilotActionProposal` (both with
the same requester-cannot-decide-their-own separation-of-duties guard `ApprovalRequest` already
enforces), `AutopilotFeedback` (append-only, `Effective()` reduces a history the same way
`ApplicationConsent.Effective` does), and `AutopilotAuditEntry` (the append-only trail the task's
own title calls for — a dedicated table rather than a reuse of `operations.Timeline`, since M8's
definition of done makes that trail a load-bearing safety property of the whole epic, not an
incidental record). Deliberately **domain only, no persistence** — no EF mapping, no migration,
no RLS, no `GRANT`, matching the exact shape PF-S05.01 and PF-S19.01 shipped in before their own
`.02`/`.04` persistence tasks landed separately. `SignalType`, `SubjectType`, `ActionType` and
`Trigger` are validated free text, not closed enums, because CPM-8.02's signal catalog and
CPM-8.08's action-adapter catalog don't exist yet — same call `TimelineEntry.RelatedObjectType`
and `ApprovalRequest.SubjectType` already made. `Recommendation`/`ActionProposal` do **not**
route their decision through the generic `ApprovalRequest` primitive yet, even though that would
be the natural fit (PF-S03.05 built it as exactly this kind of reusable approve/reject
primitive) — there is nothing to persist or query against yet, so the separation-of-duties guard
is enforced independently here and left as a note for whichever task adds persistence: point the
decision through `ApprovalRequest` (`SubjectType` `"AutopilotRecommendation"` /
`"AutopilotActionProposal"`) instead of building a second general-purpose approval flow.
Verified: 43 new unit tests (`AutopilotRunTests`, `AutopilotFindingTests`,
`AutopilotRecommendationTests`, `AutopilotActionProposalTests`, `AutopilotFeedbackTests`,
`AutopilotAuditEntryTests`), 681/681 passing; no integration tests, since nothing persists yet.

**CPM-8.02 delivered.** The closed catalog `AutopilotFinding.SignalType` was left open for
(`SignalTypes.cs`, 11 constants) plus the analyzers that produce it. Four are genuinely new
deterministic rules, pure functions taking a read-only snapshot (or, where the entity already
carries its own date math, the real entity — `ComplianceObligation.IsOverdue`/`IsEscalated`,
`Asset.IsReplacementDue`), same shape as the existing `AttentionRules.Evaluate`:
`LeasingSignalRules` (`LeaseDeadline` from `LeaseNotice`, `PaymentDeadline` from `LeaseCharge`),
`AccountingSignalRules` (`BudgetVariance` from `BudgetLine` vs. summed `JournalLine` activity
signed by `ChartOfAccount.IsDebitNormal`, `InvoiceException` from `PayableInvoice`/
`ReceivableInvoice`), `ComplianceSignalRules` (`ComplianceDeadline`), `AssetSignalRules`
(`AssetReplacement`). The other five signal types (`SlaRisk`, `StalledWork`, `VendorFollowUp`,
`TurnRisk`, `RepeatRepair`) are **not** new rules — `WorkSignalRules.MapSignalType` adapts the
existing Attention queue's findings (`IAttentionQueue`/`AttentionRules`) and
`IRepeatRepairDetector`'s tenant-wide sweep, rather than re-implementing the same rules against
`WorkItem`/`Asset` a second time. `RepeatRepair` is deliberately broader than Attention's own
`RepeatRepair` reason (that one only fires when the asset also has open work referencing it;
this one is tenant-wide) — see `SignalTypes.cs`'s and `WorkSignalRules.cs`'s own comments.

`PropFlow.Application.Autopilot.ISignalCatalog` (`EvaluateAsync(runId, ct)`, one method, no
query parameters — same shape as `IAttentionQueue.BuildAsync`) is implemented by
`EfSignalCatalog`, which runs all eight sources and materializes real `AutopilotFinding` domain
objects bound to the given run id. Nothing is persisted — `AutopilotFinding` still has no EF
mapping (CPM-8.01 is domain-only) — so `EfSignalCatalog` returns an in-memory list; whichever
task adds persistence owns writing it. Registered in `Program.cs` alongside `IAttentionQueue`.

Verified: 46 new unit tests across `SignalTypesTests`, `WorkSignalRulesTests`,
`LeasingSignalRulesTests`, `AccountingSignalRulesTests`, `ComplianceSignalRulesTests`,
`AssetSignalRulesTests` (720/720 total), plus 4 new integration tests in
`SignalCatalogTests.cs` (tenant scoping for the whole assembly, and one end-to-end case each for
a projected-snapshot analyzer, a real-entity analyzer, and asset replacement) — driven directly
against `EfSignalCatalog`, not through HTTP, since no endpoint exists yet (that lands with
CPM-8.05's daily brief API).

**CPM-8.03 delivered.** `AutopilotEvidence` (`Build` factory, immutable after — same
append-only-by-construction shape as `AutopilotAuditEntry`): `FindingId` (one row per finding),
`Inputs` (`CalculationInput` name/value pairs — the exact numbers a rule read, formatted the same
way its summary text was), `SourceLinks` (`SourceLink` entity-type/id pairs a finding traces back
to), an optional `ImpactEstimate` (`Operational`/`Financial` category, a description, and a
**nullable** `EstimatedAmount` — deliberately null rather than a guessed figure whenever a
finding's real-world cost has no honest dollar number: a compliance deadline, a lease-notice
lapse, an asset with no `ReplacementCostEstimate` on file), and `Confidence` (0–1, currently a
fixed 1.0 for every analyzer — all eight are deterministic rules, not probabilistic ones, so a
lower confidence would itself be a fabricated number; confidence varying by input completeness is
a natural extension once a probabilistic source exists to compare against).

Every CPM-8.02 rule now returns evidence alongside its candidate — `SignalCandidate` gained a
mandatory `EvidenceCandidate` field (`Inputs`/`SourceLinks`/`Impact`/`Confidence`, everything
`AutopilotEvidence.Build` needs except the ids only the assembly layer can mint), populated at
each rule's own `return` site, right where the raw numbers already are — never reconstructed
later from a summary string, which is exactly the kind of drift "never fabricates missing
values" rules out. The work-item-shaped signals (reused from `IAttentionQueue`) report the
`AttentionItem` fields already available; the repeat-repair sweep was upgraded from an id-only
list to calling `IRepeatRepairDetector.AssessAsync` per flagged asset, so its evidence carries
the real repair count, threshold, window, and cost instead of a generic "crossed the threshold"
claim with nothing behind it. `ISignalCatalog.EvaluateAsync` now returns
`AutopilotFindingWithEvidence` (`Finding`/`Evidence` pair, `Evidence.FindingId == Finding.Id`)
instead of bare findings.

Verified: 9 new unit tests (`AutopilotEvidenceTests` — construction, the two "needs at least
one" guards, the confidence range) plus evidence assertions added to the existing rule-file
tests (729/729 total), and the 4 `SignalCatalogTests` extended to assert on `Evidence` alongside
`Finding` — including the two clearest "never fabricates" cases: a compliance deadline and a
lease notice both come back with `Impact.EstimatedAmount == null`, and an asset with no
`ReplacementCostEstimate` set does too, while one with a cost on file carries it through
correctly.

**CPM-8.04 delivered, scoped to the gateway machinery only — no real provider.** Explicit
decision (user asked, given a choice): ship `IModelGateway` and everything around it, wired with
a deterministic no-op implementation, and leave the actual vendor choice and API-key handling to
a later task. Matches FS-S19's own stated philosophy for its integration adapters ("this story
delivers the machinery, not nine live provider integrations") and CPM-8.01's domain-first
precedent.

`IModelGateway` (`Application/Autopilot/IModelGateway.cs`) — built on the `IScreeningProvider`
template: `ModelGatewayOutcome { Completed, Unavailable }`, outcomes not exceptions. `Unavailable`
is the single case covering "no provider configured", "provider timed out", and "provider had an
outage" — every caller already handles it, so shipping only `NoOpModelGateway` (always
`Unavailable`, no network, no credential) is not a degraded mode, it is the epic's own
non-negotiable ("deterministic operation must remain possible without an AI provider") running
as code on day one. `TimeoutModelGateway` wraps any `IModelGateway` with a real (wall-clock, not
simulated) timeout via `CancellationTokenSource(TimeSpan)`, converting a hang into `Unavailable`
rather than an exception — provider-agnostic, so it applies unchanged once a real provider's
gateway is added alongside `NoOpModelGateway` the way `SandboxIntegrationAdapter` sits alongside
`MockIntegrationAdapter`.

`IPromptCatalog`/`PromptDefinition` — one named, versioned instruction set per prompt id, a
single current version per id (not a history), seeded with one real entry
(`StaticPromptCatalog.ExplainFindingId`) no code calls yet — CPM-8.05/.06 are expected to be the
first caller. `IStructuredOutputValidator` — confirms a provider's raw output parses as JSON and
carries every required field non-empty; deliberately not a full JSON-Schema engine (no new
package, matching "machinery not vendors" — this task's own no-op gateway never has real output
to validate).

`AutopilotContextAssembler` (Domain, pure function) — CPM-8.04's "tenant-safe context assembly":
turns an `AutopilotFinding` + its `AutopilotEvidence` into the minimized key-value context a
model call is allowed to see. Deliberately excludes `SubjectId` and every `SourceLink` — a model
explaining a finding does not need the literal internal record id, and every id sent past this
boundary is one more thing a provider holds. Every free-text value (`Summary`, an impact
`Description`, each calculation input's value) passes through `AutopilotRedaction.Redact` first
— a defensive email/phone pattern strip for the user-entered names (a compliance obligation's
title, an asset's name) that flow into those fields, not a general PII classifier.

`AutopilotGatewayOptions` (`TimeoutMilliseconds`, default 8000) — the `ScreeningOptions` idiom,
bound in `Program.cs` the same way. DI: `NoOpModelGateway` registered directly,
`IModelGateway` resolves to a `TimeoutModelGateway` wrapping it; `IPromptCatalog` →
`StaticPromptCatalog`; `IStructuredOutputValidator` → `JsonStructuredOutputValidator`.

Verified: 28 new unit tests (`AutopilotRedactionTests`, `AutopilotContextAssemblerTests`,
`NoOpModelGatewayTests`, `TimeoutModelGatewayTests` — including a genuine, not simulated,
30ms-timeout-vs-500ms-delay race, and confirming the caller's own cancellation still throws
rather than being swallowed as a timeout — `JsonStructuredOutputValidatorTests`,
`StaticPromptCatalogTests`, `AutopilotGatewayOptionsTests`), 757/757 total. No integration tests
— no database or HTTP surface exists for this task (matches CPM-8.01's own precedent); the full
integration suite was run anyway to confirm the app host still boots cleanly with the new DI
registrations.

### M8 delivery slices — backlog only

These slices deliberately keep M8 independently shippable. Completing one slice does not imply
starting the next; all slices remain `Backlog` until explicitly selected for implementation.

| Slice | Scope | Included tasks | Stop condition |
| --- | --- | --- | --- |
| M8-S1 — Deterministic brief foundation | Finding lifecycle, cross-suite signals, evidence/impact projections, daily brief API/UI, and baseline acceptance coverage | CPM-8.01–CPM-8.06, deterministic portion of CPM-8.12 | A manager can review tenant-scoped findings with source evidence, impact, freshness, and feedback controls; no mutations are executed |
| M8-S2 — Assisted operations | Typed action proposals, previews, capability re-checks, approvals, idempotency, and first work/communication action adapters | CPM-8.07–CPM-8.09 | A manager can confirm a safe assignment, schedule, follow-up, or message draft and see the audited result; autonomous execution remains disabled |
| M8-S3 — Ask CovePM | Read-only natural-language questions over approved projections with source-linked answers and refusal behavior | CPM-8.03–CPM-8.05, CPM-8.10, Ask CovePM portion of CPM-8.12 | Supported questions return evidence-linked answers; unsupported, unauthorized, or unsafe requests are refused without mutation |
| M8-S4 — Governed autonomy and evaluation | Organization/role/property/action policies, model/provider controls, feedback loops, outcome metrics, and adversarial acceptance tests | CPM-8.04, CPM-8.11–CPM-8.13 | Organizations can explicitly enable limited autonomy, inspect every action, measure outcomes, and disable the feature safely |
| M8-S5 — Release package | Administrator guidance, safety contract, provider setup, support runbook, demo, and product metrics definitions | CPM-8.14 plus documentation portions of CPM-8.12/CPM-8.13 | M8 can be evaluated, configured, supported, and demonstrated without undocumented operational assumptions |

Recommended sequencing is `M8-S1 → M8-S2 → M8-S3 → M8-S4 → M8-S5`. M8-S1 is the only slice
needed to validate whether the daily brief creates customer value. M8-S2 and later remain optional
until that evidence exists.

---

## Epic M10 — Voyager parity and operator workspace

### M10 — Voyager parity and operator workspace

**Goal:** give property managers one coherent workspace for the daily dashboard, maintenance,
resident lifecycle, inspections/turns, reporting, and procurement workflows validated against
Emily's Voyager priorities.

**Definition of done:** a manager can identify exceptions, drill into source records, complete
supported maintenance/inspection/procurement actions, run and schedule reports, and preserve tenant
scope, permissions, PII minimization, concurrency, and audit history. See
[`docs/cpm-10-10-workflow-validation.md`](cpm-10-10-workflow-validation.md) for the acceptance
matrix, deterministic fixtures, browser command, and rollout runbook.

| ID | Task | Area | Est | Depends on |
| --- | --- | --- | --- | --- |
| CPM-10.01 | ✅ Role-based operations dashboard and work-order analytics | web | L | — |
| CPM-10.02 | ✅ Rich work-order detail and operational controls | web/api | L | CPM-10.01 |
| CPM-10.03 | ✅ Resident directory and profile workspace | web/api | L | CPM-10.01 |
| CPM-10.04 | ✅ Lifecycle calendar and deadline views | web/api | M | CPM-10.01, CPM-10.03 |
| CPM-10.05 | ✅ Inspections, make-ready, and unit-turn workspace | web/api | L | CPM-10.02 |
| CPM-10.06 | ✅ Reports hub and scheduled reporting UX | web/api | L | CPM-10.01 |
| CPM-10.07 | ✅ Procurement, vendor, and purchase-order workspace | web/api | L | CPM-10.02 |
| CPM-10.08 | ✅ Cross-module context, navigation, and quick actions | web | M | CPM-10.01–10.07 |
| CPM-10.09 | ✅ Sensitive resident data, permissions, and audit review | api/web | M | CPM-10.03, CPM-10.08 |
| CPM-10.10 | ✅ Emily workflow validation, seeded fixtures, and rollout documentation | tests/chore | M | CPM-10.01–10.09 |

### M10 delivery slices — backlog only

M10-S1 is the explicitly selected delivery slice and is complete. M10-S2 and later remain backlog
until explicitly selected; completing this validation task does not imply scope expansion.

| Slice | Scope | Included tasks | Stop condition |
| --- | --- | --- | --- |
| M10-S1 — Operator workspace | Dashboard, maintenance, resident/lifecycle, inspections/turns, reports, procurement, navigation, permissions, acceptance, and rollout | CPM-10.01–CPM-10.10 | Emily workflow matrix passes against seeded data and full-suite regression is recorded |
| M10-S2 — Deeper Voyager parity | Additional imports, integrations, configuration, and workflow depth selected from gaps | Backlog | Explicit product selection and a new acceptance record |

---

## Epic M9 — Native Field Apps

**Milestone:** `M9`
**Goal:** deliver installable iOS and Android apps for technicians working in the field, with a
focused “my assigned work” experience that remains useful without connectivity and safely
synchronizes status updates, notes, inspections, and photos when the device reconnects. This is a
native-distribution initiative for the Apple App Store and Google Play; a responsive web app, PWA,
or browser wrapper is not a substitute.

**Product boundary:** M9 serves technicians and other explicitly authorized field roles. The
existing web app remains the manager/admin workspace. The first release does not recreate the full
PMS, accounting, leasing, resident portal, or Autopilot surfaces on mobile. A shared native codebase
(React Native/Expo is the default candidate) is preferred, but the implementation technology is a
decision in CPM-9.01.

**Offline contract:** cached data must remain tenant- and assignment-scoped; every offline mutation
gets a durable client operation ID, local timestamp, retry state, and server acknowledgement. Sync
must be idempotent, observable, and explicit about conflicts. “Last write wins” is not an acceptable
default for work status, assignment, terminal state, or inspection completion.

**Definition of done:** a technician can install signed production apps from the App Store or Google
Play, sign in securely, view assigned work and relevant property context offline, capture
status/note/photo/inspection changes offline, reconnect and observe deterministic sync outcomes, and
resolve supported conflicts without cross-tenant or cross-assignment access. Store review,
privacy/data-safety disclosures, crash reporting, staged rollout, and upgrade behavior are covered.

| ID | Task | Area | Est | Depends on |
| --- | --- | --- | --- | --- |
| CPM-9.01 | Native product/platform decision: iOS/Android matrix, shared-codebase choice, app identifiers, environments, minimum OS versions, device capabilities, offline duration, cache limits, and release ownership | product/architecture | M | — |
| CPM-9.02 | Native application foundation: navigation, design tokens, accessibility, environment config, secure builds, deep links, error boundaries, and connectivity state | native | L | CPM-9.01 |
| CPM-9.03 | Mobile identity/device security: native sign-in/session flow, secure credential storage, refresh/revocation, organization binding, logout, device registration, and lost-device invalidation | native/api | L | CPM-9.01, FS-S01 |
| CPM-9.04 | Field read model/local database: assigned work, property/space/asset context, timeline, inspection templates, attachment metadata, schema versioning, encryption/retention, eviction, and tenant/assignment fencing | native/application | XL | CPM-9.02, CPM-9.03, FS-S11, FS-S12, FS-S13 |
| CPM-9.05 | Sync protocol: cursor/change-feed pull, bounded batch push/pull, bootstrap refresh, acknowledgements, dependencies, idempotency keys, retry/backoff, observability, and server audit records | api/application | XL | CPM-9.04, FS-S01, FS-S03 |
| CPM-9.06 | Offline work actions: status including On The Way, internal/resident-visible notes, validation, queued mutations, replay safety, terminal handling, and pending/synced/failed states | native/api | L | CPM-9.04, CPM-9.05, PF-5.03, PF-5.06 |
| CPM-9.07 | Conflict handling: version reconciliation, per-action policy, refresh/merge UI, duplicate submissions, assignment changes, revoked access, and partial-connectivity recovery | application/native | L | CPM-9.05, CPM-9.06 |
| CPM-9.08 | Offline photo capture/attachment sync: camera, compression/thumbnails, local encryption/quota, resumable or retryable upload, local-to-server IDs, retention cleanup, and failure recovery | native/api/infra | L | CPM-9.04, CPM-9.05, PF-7.01 |
| CPM-9.09 | Offline inspections: versioned template download, checklist/finding capture, finding photos, completion/approval rules, stale-template handling, and safe replay without partial inspection state | native/api | XL | CPM-9.04, CPM-9.05, FS-S12 |
| CPM-9.10 | Assignment/urgency delivery: push registration, assignment notifications, urgent-work policy, background refresh limits, preferences, and deep links | native/api/infra | L | CPM-9.03, CPM-9.05 |
| CPM-9.11 | Native quality/field acceptance suite: sync/replay, tenant/assignment isolation, offline transitions, device matrix, accessibility, performance, battery, storage, and poor-network tests | tests | XL | CPM-9.05–CPM-9.10 |
| CPM-9.12 | Store/production release: Apple/Google signing, CI builds, TestFlight/Play internal testing, privacy and data-safety disclosures, permission rationale, crash/analytics controls, staged rollout, rollback, and upgrade/migration runbooks | release/chore | L | CPM-9.02, CPM-9.03, CPM-9.11 |
| CPM-9.13 | M9 documentation/demo: offline walkthrough, sync/conflict support guide, device-loss procedure, store operations guide, supported-device matrix, retention explanation, and release checklist | chore | M | CPM-9.07, CPM-9.12 |

### M9 delivery slices — backlog only

All slices remain `Backlog` until explicitly selected. Each slice must preserve the native App Store/
Google Play target; a browser-only implementation does not satisfy the slice.

| Slice | Scope | Included tasks | Stop condition |
| --- | --- | --- | --- |
| M9-S1 — Native foundation | Platform decision, native shell, secure identity, connectivity, and signed internal builds | CPM-9.01–CPM-9.03 | Test users install iOS and Android builds and see assignment-scoped behavior |
| M9-S2 — Offline field read | Encrypted cache, assigned work/property context, timeline, cache lifecycle, bootstrap refresh | CPM-9.04 | Assigned work remains usable offline without cross-tenant or cross-assignment access |
| M9-S3 — Safe offline updates | Sync protocol, status/note queue, retries, acknowledgements, conflicts, auditability | CPM-9.05–CPM-9.07 | Changes replay once, expose failures, and never silently overwrite newer server state |
| M9-S4 — Photos and inspections | Camera/photo queue plus versioned inspection completion and finding sync | CPM-9.08–CPM-9.09 | A field visit can be completed offline and reconciled without orphaned/duplicated evidence |
| M9-S5 — Connected field operations | Push, background refresh, deep links, preferences, and UX hardening | CPM-9.10–CPM-9.11 | Notifications work within platform limits and the supported device matrix passes |
| M9-S6 — Store release | Compliance, production signing, staged rollout, upgrade/rollback, documentation, acceptance demo | CPM-9.12–CPM-9.13 | Both apps are review-ready and installable through production or approved release tracks |

Recommended sequencing is `M9-S1 → M9-S2 → M9-S3 → M9-S4 → M9-S5 → M9-S6`. M9-S3 is the
minimum product-value slice; M9-S4 is required if inspections are part of the field promise, while
M9-S5 and M9-S6 are required for a production store launch.

**Explicit non-goals:** PWA/browser delivery, a full mobile PMS, offline resident-message dispatch,
offline payment/accounting actions, autonomous background mutations without user intent, bypassing
capability/scope checks, and an unbounded local copy of tenant data.

---

## Open questions / decisions needed

- **SLA model** — *resolved in PF-6.06*: the attention queue uses a per-priority first-response
  budget measured from creation while the work is still `New` (Critical 4h, High 24h, Normal 72h,
  Low 120h), fixed in `AttentionThresholds`. Per-organization / per-category tuning is a future
  task if the demand appears.
- **Unit-turn workflow** — *resolved in PF-6.06*: a unit turn is the existing
  `WorkType.UnitTurnTask`, not a distinct entity. "At risk" = a `UnitTurnTask` still
  `New`/`Assigned` and due within 5 days. Checklist-style turn steps remain out of scope.
- **Saved views scope** (PF-3.17): user-private only, or shareable/organization-default views?
- **Portfolio depth** (PF-3.05): single portfolio layer, or arbitrary nesting for enterprise
  hierarchies?
- **Tagging** (PF-4.07 "add tag"): free-text tags or a managed tag vocabulary per organization?
- **Automation action set** (PF-5.04): finalize the predefined triggers, conditions, and actions
  for the first version before building the engine.
