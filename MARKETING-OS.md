# Marketing OS

Desktop growth workspace for Averion Software. Primary goal: qualified leads and pilot customers. Research informs segment selection; the numerical growth target is intentionally unset until there is a baseline.

Agents may research and prepare work. Publishing and outreach require a human decision. Creating a board card is planning, not an instruction to an agent executor. The Agent activity screen queues the four existing supported preparation workflows.

## Source and deployment

The canonical client files are in `dist/client`. `build-growth.cjs` produces the common entry pages from the interface template; the document catalog is retained in `dist/client/os-catalog.js`. The packaged production worker is versioned in `dist/server/index.js`, and existing static assets are included in source. `/`, `/marketing-os`, and `/marketing-os.html` resolve to the same workspace.

Run `node build-growth.cjs` after changing the entry template. Run `node tools/verify-workspace.mjs` to check calendar persistence, edit conflicts, exact approval previews, decision history, and external-action boundaries against an isolated SQLite database. These checks never contact publishing or email providers.

Use `node tools/preview-workspace.mjs` for an isolated local preview at port 8897. All preview data is visibly labeled as sample data and stored in memory. It is never included in the production archive. Stop the preview process after review.

After committing and pushing the exact source, `node tools/package-release.cjs` writes a deployment archive from `.openai` and `dist`. Save and deploy that commit through Sites, preserving the custom site audience.

## Shared data

The Calendar includes a read-only Publora feed alongside shared task deadlines. `/api/publishing-calendar` uses the existing secret `PUBLORA_API_KEY` to load dated posts for the visible range, with pagination and account names. Post times and day placement use America/New_York, including daylight saving changes. Task due dates retain their assigned calendar dates. Refresh reloads the feed; navigating months loads the new range. Failed refreshes retain previously loaded posts and label them stale; a successful empty response clears them. Unsched­uled approval drafts are not presented as scheduled posts. Click a post to inspect its copy, status, accounts, and attached media. This integration does not create, modify, or publish posts.

Run `node tools/verify-publishing.mjs` for isolated provider-contract checks. `node tools/capture-publishing.cjs` captures sample-only desktop/mobile evidence and a walkthrough in `.review/publishing-calendar/`, then closes the preview server and browsers. This repository has no sandbox pool; the preview uses an isolated in-memory database.

- Existing approval, research, lead, and run-report data remain in their existing tables.
- `workspace_tasks` stores shared plans, owners, due dates, notes, links, stages, and optimistic edit versions. Calendar dates use YYYY-MM-DD with no timezone conversion.
- `approval_decisions` stores a decision trail. Approval and status are updated atomically with the decision record. Approving an existing item does not create a duplicate queue entry or itself contact a provider.
- Per-source loading retains last successful data during partial outages. Source freshness and underlying evidence age are separate.

## Boundaries and next integration work

The system reports available records, not unverified outcomes. Company fit does not establish buyer interest. Research confidence is the researcher's assessment. API synchronization does not establish fresh research. Agent completion does not establish draft quality or delivery.

Live hosted company/research coverage differs from the local vault and runner. Reconciliation needs stable record identity and a clear execution source before automatically importing or altering historical records. CRM outcome synchronization and a numerical 90-day growth target are follow-up decisions. Existing service configuration does not prove delivery health.

UI acceptance requires an interactive walkthrough and the short video requested in the user's AGENTS.md. Do not mark the UI upgrade Done until this evidence is captured.
