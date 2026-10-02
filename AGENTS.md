# Averion marketing workspace

## Autonomous marketing agent operating loop

The active setup and first-run checklist are in `marketing-vault/Plans/Autonomous Marketing Agent Setup - 2026-10-02.md`. Read that plan and the vault `Dashboard.md` at the start of a marketing run. Treat the vault as the durable record and the local workbench as a drafting and measurement tool. Do not assume its SQLite state matches the separately scheduled checkout.

For each run, choose one bounded outcome from the current priority, then:

1. Check the latest session brief, open approvals, experiment log, and current product evidence.
2. Record a target buyer, question, source-backed hypothesis, metric, and next checkpoint.
3. Do reversible research, analysis, and drafting; date sources and distinguish observation from inference.
4. QA claims against `Brand/Brand Guide.md`, `Strategy/Positioning.md`, and the current release evidence. Put anything intended for external use into exact-artifact review.
5. Record what was completed, measured outcomes, evidence gaps, approvals needed, and the next action in a dated `Sessions/` note. Update `Dashboard.md` when priorities or major artifacts change.

The current first lane is private multifamily operator discovery and RFP readiness. Social publishing is deferred by the active workstream. Prefer one useful, reviewable artifact over filling the queue with speculative tasks. The local model may draft from a supplied evidence packet; it does not independently verify market facts.

The agent may research and draft autonomously. Public publishing, prospect contact, CRM writes, calendar changes, spend, and commercial or product commitments require the existing explicit human approval and audit path. A draft, fit score, or queue card is not approval. Do not run live approval, scheduler, or executor commands as verification.

This repository combines the Averion Compass website entry point, Marketing OS interfaces, an Obsidian marketing vault, and a local Python marketing workbench. It is not the Compass product application. The root `index.html` now redirects to `https://averionsoftware.com/products/compass/`; the root README's landing-page/pricing description is older than the current file.

## Repository map

- `index.html`: public redirect; JavaScript preserves the query string and fragment, with meta-refresh and link fallbacks.
- `marketing-os.html`, `artifacts.html`, `assets/`: root control-room interface, source-library page, and brand assets.
- `dist/server/index.js`, `drizzle/0000_approval_actions.sql`: hosted approval worker and SQL schema; separate from static GitHub Pages hosting.
- `marketing-vault/`: operational knowledge source; open this folder, not the repository root, as the Obsidian vault. Start with `Homepage.md` and `Dashboard.md`.
- `marketing-vault/agent-workbench/`: Python CLIs, `tests/test_*.py`, task briefs, generated reports, and local SQLite state.
- `.os-upgrade/`: separate Git worktree/gitlink containing the newer Marketing OS. Read its `MARKETING-OS.md` before changing that interface.
- `.mdviewer/`: another separate Git worktree/gitlink. Changes inside either nested checkout are not ordinary parent-repository file changes.
- `Plans/averion-stella/`: isolated, undeployed Stella website preview; its README describes the handoff and visual rules.

## Development environment

- Root website is dependency-free static HTML; no root package manifest, lockfile, or application build toolchain is present.
- Root preview command from `README.md`: `npx serve .` (requires Node/npm). This serves files, not the worker API.
- Workbench uses Python standard-library modules plus LangGraph; `marketing-vault/agent-workbench/requirements.txt` specifies `langgraph>=1.2,<2`. No Python version pin is provided.
- Workbench setup: copy `config.example.json` to local `config.json`; the loader falls back to the example when the local file is absent. Paths are resolved relative to the workbench directory.
- Live drafting uses Ollama at `http://127.0.0.1:11434`; documented model setup is `ollama pull qwen3:4b`. The documented local-only setting is `OLLAMA_NO_CLOUD=1`.
- Workbench commands below run from `marketing-vault/agent-workbench/`: `python control_room.py status`, `python control_room.py dashboard`. Dashboard generation writes a report; these are not test commands.

## Build, checks, and deployment

- No root build, test, or lint script is configured. `.github/workflows/pages.yml` deploys on pushes to `main` or manual dispatch; it does not run tests.
- Pages staging commands are exactly `mkdir -p public-site` followed by `cp index.html CNAME public-site/`. Only those two files are deployed: not assets, the vault, Marketing OS, or worker code.
- Workbench tests use `unittest.TestCase`, temporary directories/databases, and mock responses. The README does not document a suite invocation; do not assume a pytest/npm test setup.
- The following commands are documented in `.os-upgrade/MARKETING-OS.md`; run them from `.os-upgrade/`, not the repository root:
  - `node build-growth.cjs`: regenerate shared entry pages after editing the HTML template in that script.
  - `node tools/verify-workspace.mjs`: isolated SQLite checks for tasks, calendar dates, edit conflicts, exact approval previews, and decision history; external fetches are blocked.
  - `node tools/verify-publishing.mjs`: isolated publishing-provider contract checks.
  - `node tools/preview-workspace.mjs`: sample-only, in-memory preview on port 8897; stop it after review.
- `.os-upgrade/dist/client/` contains canonical client assets and `os-catalog.js`; its versioned `dist/server/index.js` is the production worker. Do not blanket-ignore or delete `dist` as disposable build output.
- The root and nested worktree have different entry points and deployment paths. Confirm the target before copying HTML between them or deploying; preserve `.openai/hosting.json` and the configured site audience.

## Code and content conventions

- Root interfaces use standalone HTML with inline CSS custom properties and vanilla JavaScript, not React or a CSS framework. Preserve their existing navy/gold tokens and responsive rules.
- Python modules use snake_case, type annotations, dataclasses, `pathlib.Path`, UTF-8 file I/O, `argparse` entry points, and SQLite. Tests import neighboring modules by adding the workbench directory to `sys.path`.
- Task frontmatter is a simple `key: value` parser, not full YAML. Fields include `objective`, `audience`, and `approval_required`; multiple `context_files` are separated by semicolons.
- Vault notes use readable filenames with spaces and Obsidian `[[wikilinks]]`; session notes follow `YYYY-MM-DD - Topic.md`. Browser links percent-encode spaces.
- Put sourced, access-dated findings in `Research/`, meaningful work records in `Sessions/`, recurring work in `Schedules/`, and observed experiments in `Experiments/`. Update `Dashboard.md` for major artifacts or priority changes.
- Consult `marketing-vault/Brand/Brand Guide.md` and `Strategy/Positioning.md` before changing copy. Use Averion Compass for the property-management product and Averion Software LLC for legal references; preserve retired names only in historical records or compatibility identifiers.
- Preserve the existing logo without recoloring, stretching, or redrawing it. Every social post requires an image selected using `marketing-vault/Assets/social-catalog/README.md` and its catalog.

## Operational pitfalls

- The workbench README's `tasks/inbox/example-maintenance-page.md` example has moved under `tasks/archive/compass-reset-20261001T122834Z/`. Do not run the stale example command or reactivate archived briefs as current work.
- Drafting writes `outputs/` and `data/runs.sqlite3`; these are operational state, not source fixtures. `config.json`, `.venv/`, caches, generated Markdown, and SQLite files have workbench-local ignore rules, but some historical outputs are already tracked.
- Static preview cannot provide `/api/*` queue behavior. Offline data must remain visibly offline; never replace unavailable live metrics with sample success counts.
- Approval, publication, and delivery are distinct. The local drafting runner does not publish, but hosted worker/integration paths can send externally. Do not use live approval endpoints or scheduler/executor commands as smoke tests.
- Preserve explicit human approval, suppression/opt-out checks for email, idempotency keys, and decision audit trails. Preparing a draft or board card is not authorization to publish or contact a prospect.
- Current public claims must be supported by reviewed product evidence; research and fit scores do not establish shipped capabilities, customer interest, compliance, pricing, or measured outcomes.
- Historical package archives and staging trees coexist with active files. Do not restore deleted staging artifacts or normalize nested worktrees while doing unrelated work.
