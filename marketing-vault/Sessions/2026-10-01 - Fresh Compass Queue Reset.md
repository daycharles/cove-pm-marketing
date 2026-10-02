# Fresh Averion Compass queue — October 1, 2026

User requested clearing every legacy approval item and preventing the 12 CovePM items from returning.

The active Windows monitor runs the `averion-software/products/cove-pm/marketing-vault/agent-workbench` checkout. Its local approval rows remained pending after remote dismissals. Its sync considered only active remote statuses when deduplicating, so it uploaded the same 12 approvals again. The earlier status-only cleanup did not retire that local source.

Actions completed:

- Temporarily disabled the monitor during cleanup, then restored it with the existing cadence.
- Backed up the local SQLite ledgers and moved the old inbox briefs to dated archive directories in both the active checkout and this reporting vault.
- Retired the active checkout's 17 work items and 27 approval records; the reporting copy's records were also retired. Old run synchronization cannot reactivate retired tasks, and any prior local approval decision prevents recreating its approval row.
- Moved the recurring template out of the scanned inbox. New daily briefs use a distinct `linkedin-compass-content-YYYYMMDD.md` identity and an Averion Compass objective.
- Copied the new social asset collection, next-post asset requirement, and publishing workflow to the actual scheduled checkout.
- Fixed remote sync deduplication to recognize an existing item regardless of its current status.
- Deployed Marketing OS version 61 with durable archive/reset handling, retired-local-ID import rejection, duplicate-source prevention, and a guard against CovePM in new approval titles/public copy.
- Retired all 94 hosted approval records, preserving their stored audit history while removing them from the inbox. Research, prospect records, published posts, and existing publishing calendar entries were outside this approval reset.

Verification: active local inbox scan and historical run sync leave zero active work and zero pending approvals. A temporary fresh-task generation test confirmed the Compass objective, distinct task identity, and new-asset requirement. Hosted queue returned zero items after reset. Replaying a retired local approval returned HTTP 409. Isolated service tests verified archived items cannot be revived via status updates or decisions; fresh Compass imports remain accepted; duplicate imports are blocked.

Local backups and archived task briefs are in each workbench's `tasks/archive/compass-reset-20261001T122834Z/`. Counts and locations are recorded in `Plans/marketing-queue-reset/local-reset.json` in the reporting checkout. No content was published or scheduled during the reset.
