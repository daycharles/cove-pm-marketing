# CovePM Local Marketing Workbench

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

The LangGraph path now runs a cheap deterministic website audit and an optional local market/
competitor research agent before the local writer. For a website review task, it audits the current
root `index.html`, checks approved local market sources, and passes both findings into the writer.
Unsupported numbers and source attributions are blocked by QA. The site is never edited or
published by this workflow.

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
next-review metrics for the weekly operating checkpoint.

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
