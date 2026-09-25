# Averion Software Local Marketing Workbench

For current product names, approved public wording, and visual rules, read `../Brand/Brand Guide.md`
and `../Strategy/Positioning.md`. Current live-site screenshots are in `../Assets/website-screenshots/`.

Minimal first workflow from the red-team plan:

```text
Markdown task -> local website/evidence research -> one Ollama writer call -> deterministic QA -> Markdown draft
```

This workbench intentionally has no Notion, Linear, Slack, hosted service, FastAPI, or database server dependency. It uses:

- Python standard library;
- Ollama at `http://127.0.0.1:11434`;
- Markdown files in this repository;
- SQLite for technical run metadata only.
- LangGraph locally for explicit workflow orchestration.

The local writer does not perform web research. The Codex agent supplies a dated, compact research
packet from approved public sources, and the local model uses that packet plus approved product
context to draft and repurpose content. This keeps web usage in one place and avoids spending
local-model or external-search usage on duplicate research. Unsupported numbers and source
attributions are blocked by QA. The site is never edited or published by this workflow.

## Setup

1. Install Ollama from the official installer.
2. Pull one small model, for example:

   ```powershell
   ollama pull qwen3:4b
   ```

3. Copy `config.example.json` to `config.json` and adjust paths only if needed.
4. Run the example in mock mode first:

   ```powershell
   python run.py --task tasks/inbox/example-maintenance-page.md --mock
   ```

5. Run against Ollama:

   ```powershell
   python run.py --task tasks/inbox/example-maintenance-page.md
   ```

Run the same workflow through LangGraph:

```powershell
.\.venv\Scripts\python.exe langgraph_runner.py --task tasks/inbox/example-maintenance-page.md --mock
.\.venv\Scripts\python.exe langgraph_runner.py --task tasks/inbox/example-maintenance-page.md
```

The repeatable local scheduler entry point is:

```powershell
powershell -ExecutionPolicy Bypass -File .\run-weekly.ps1
```

The current machine has 32 GB RAM and integrated Intel graphics. Expect CPU-oriented inference and benchmark before adding any second model.

Ollama models run locally; the workbench does not send task or context data to a hosted model.
Ollama cloud features are disabled for this workstation with `OLLAMA_NO_CLOUD=1`.

## Output

Each run writes:

- a Markdown draft under `outputs/`;
- a SQLite record under `data/runs.sqlite3`;
- a source list and deterministic QA result inside the draft.

The human approval step is deliberately outside the runner. Nothing is published, sent, priced, or committed by this workbench.

## Control room

The local control room adds a durable queue, action ledger, approval queue, and Markdown status
view on top of the existing runner. It is still local-only and does not send email, publish social
content, edit the website, or commit commercial terms.

```powershell
python control_room.py dashboard
python control_room.py status
python control_room.py run-next
python control_room.py weekly-review
```

`run-next` processes one queued inbox task with Ollama and moves a successful draft into
`awaiting-approval`. Use `--mock` for deterministic tests. The generated control-room view is
`outputs/CONTROL-ROOM.md` and the queue/action/approval records live in `data/runs.sqlite3`.
`weekly-review` writes `outputs/WEEKLY-REVIEW.md` with lead disposition, approval, outcome, and
next-review metrics for the weekly operating checkpoint. The review now also includes pilot events
recorded by the control room: completed/failed workbench runs and approval requests.

Pilot events can be inspected or recorded locally:

```powershell
python control_room.py pilot-metrics
python control_room.py record-event --event-type human_minutes --quantity 30
python control_room.py record-event --event-type demo_booked
```

Use event names that describe measurable pilot behavior, such as `human_minutes`, `demo_booked`,
`outreach_draft`, `content_approved`, or `inbound_response`. These records are measurement inputs;
they do not send, publish, or approve anything.

## Lead qualification

The company-first lead workflow accepts supplied candidates with approved public evidence and
returns a fit score, qualified/review/nurture/disqualify disposition, evidence gaps, and a next
research action. It does not discover personal contacts or send outreach.

```powershell
python lead_engine.py --candidates leads/candidates.json
```

See `leads/README.md` for the accepted candidate shape and privacy boundary.

The lead queue is approval-gated and can be operated locally without sending outreach:

```powershell
python lead_engine.py --queue
python lead_engine.py --db data/runs.sqlite3 --lead-id "Company|https://example.com" --resolve-gaps --reviewer "Reviewer"
python lead_engine.py --db data/runs.sqlite3 --lead-id "Company|https://example.com" --approve --reviewer "Reviewer" --action "Review the company-level maintenance workflow"
python lead_engine.py --db data/runs.sqlite3 --lead-id "Company|https://example.com" --outcome advance --note "Evidence supported a next conversation"
```

Qualification recommendations remain separate from human approval. Evidence gaps must be resolved before approval, and outcomes can only be recorded after approval.

## Lead-to-demo pipeline

After a lead is approved, the local pipeline can prepare and track the next steps without sending
messages or changing a calendar:

```powershell
python pipeline.py status
python pipeline.py create-draft --lead-id "Company|https://example.com" --recipient-label "approved business contact" --subject "Maintenance workflow" --body "Would a short conversation be useful?" --source-url "https://example.com/about"
python pipeline.py approve-outreach --draft-id 1 --reviewer "Reviewer"
python pipeline.py record-send --draft-id 1 --sender "Reviewer"
python pipeline.py schedule-demo --lead-id "Company|https://example.com" --draft-id 1 --scheduled-at "2026-09-25T14:00:00-04:00"
python pipeline.py demo-outcome --demo-id 1 --status held --note "Discovery completed"
```

The draft-creation function requires an approved lead and evidence URLs. Sending is represented by
an explicit human action, and demo outcomes are recorded for pilot measurement.

## Content factory

Create one fact-based package for website, email, social, and sales variants:

```powershell
python content_factory.py create --title "Unit-turn workflow update" --audience "property operators" --fact "Averion Compass helps teams record inspection findings, assign follow-up work, and track a unit through review and sign-off." --cta "How it works" --source-url "https://averionsoftware.com/products/compass/"
python content_factory.py approve --package-id 1 --reviewer "Reviewer"
python content_factory.py publish --package-id 1 --channel social --published-url "https://example.com/post" --recorded-by "Reviewer"
python content_factory.py status
```

The package shares one immutable fact base across channels. It must be approved before a
publication can be recorded, and the local workflow does not publish to an external channel.

## Inbound coordinator

Inbound messages are classified locally. Demo requests and product questions can receive a draft;
pricing, support, complaints, and unknown intents escalate before drafting:

```powershell
python inbound.py intake --source website --sender-label prospect --body "I would like to book a demo."
python inbound.py draft --message-id 1 --body "Thanks — we can help coordinate a demo."
python inbound.py approve --response-id 1 --reviewer "Reviewer"
python inbound.py record-sent --response-id 1 --sender-label "Reviewer"
python inbound.py status
```

These commands record the workflow only. A future mailbox or website connector must call the same
approval functions and preserve the event trail.

## External pilot configuration

Validate a customer-specific pilot boundary before loading customer facts or enabling channels:

```powershell
python pilot_config.py pilot.example.json --output outputs/EXTERNAL-PILOT.md
```

The configuration requires an accountable owner, one bounded workflow, baseline and success
metrics, approved facts, allowed channels, a data boundary, and a review date. Email pilots must
also include inbound handling for replies and opt-outs.

## Pilot reporting

Render the combined funnel and operating report after a review cycle:

```powershell
python reporting.py --output outputs/PILOT-REPORT.md
```

It reports approval, send, demo, inbound-response, content, event, and human-effort signals. A
missing denominator is shown as `—` rather than being treated as zero performance.

## External integrations

The connector boundary covers email, social, CRM, and calendar actions:

```powershell
python integrations.py status --config integrations.example.json
python integrations.py email --config integrations.example.json --to "approved-recipient" --subject "Demo" --body "Approved message" --approved --suppression-checked --idempotency-key "lead-123-email-1"
```

The example configuration uses manual mode. Every action requires explicit approval; email also
requires a suppression/opt-out check. Actions are idempotent, so retrying the same key returns
`already-recorded` instead of creating a duplicate side effect. Live provider adapters are not
enabled until provider, credentials, scopes, account ownership, and compliance rules are approved.

Provider-specific setup is tracked in `integrations.live.example.json` and
`Plans/Live Provider Activation Plan.md`. The recommended first stack is Zoho Mail, Google Calendar,
and HubSpot; social publishing remains manual until a specific provider is selected.

The cross-provider lead-to-demo handoff is prepared as one auditable packet. It can read HubSpot
company matches, queue an approved draft through the Zoho Mail bridge, and hold the Calendar step
for human review:

```powershell
python lead_to_demo.py --lead-id "example pm|https://example.com" --company "Example PM" --website "https://example.com" --to "approved-recipient" --subject "Demo" --body "Approved draft" --idempotency-key "lead-123-demo-1"
```

## HubSpot CRM adapter

HubSpot is now configured for the Averion Software workspace. The API adapter is ready for a
narrowly scoped private-app token:

```powershell
python hubspot_api.py status
python hubspot_api.py health
python hubspot_api.py search-company --domain example.com
```

See `hubspot.example.env.md` for the required environment variables and scopes. Health checks and
company search are read-only; company upserts remain explicitly approval-gated.

Pilot readiness is scored separately after an approved discovery conversation:

```powershell
python -c "from pilot_readiness import PilotReadiness, render_scorecard; print(render_scorecard(PilotReadiness('Example')))"
```

The readiness score is a qualification aid only; it does not approve pricing, contracts, implementation commitments, or security claims.

## Model choice

Start with one model only. `qwen3:4b` is the default because it is small enough to benchmark on the current machine and supports structured task output. Compare `gemma3:4b` later if copy quality is weak. Do not install a model fleet until the first ten runs are evaluated.

## Local task format

```markdown
---
objective: Draft a maintenance landing-page section
audience: Multifamily operations leaders
approval_required: true
---

Use only approved product and research files. Include a source list.
```
