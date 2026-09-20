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
