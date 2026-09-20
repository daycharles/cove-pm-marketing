# Independent Red-Team Review: CovePM AI Marketing Team

Date: 2026-09-20  
Reviewer: independent architecture and cost review  
Scope: current implementation plan, zero incremental cost, minimal Codex/ChatGPT usage, local LLM preference

## Verdict

The strategy is sound, but the first implementation should be smaller. The original plan describes a future marketing operating system; it should not be mistaken for the minimum viable build.

For the stated constraints, defer:

- LangGraph;
- FastAPI;
- Postgres;
- Notion and Linear webhooks;
- a custom dashboard;
- ten Notion databases;
- autonomous outbound, publishing, or paid-media actions;
- Slack automation;
- external research connectors.

Start with a local, single-process workflow using Ollama, Python, Markdown, SQLite, and Windows Task Scheduler. Keep Notion and Linear available as human-facing systems, but integrate them manually or through occasional one-way exports until the local workflow proves valuable.

## Most important gaps

### Zero cost needs a boundary

Define zero incremental cost as:

> No new paid subscriptions, API bills, hosted infrastructure, or paid model usage.

Local inference still has hardware, electricity, storage, and maintenance costs. Existing Notion, Linear, ChatGPT, Codex, and hosting subscriptions should be optional human tools, not required runtime dependencies.

### The current scheduled AI cadence is expensive in usage

The vault documents three weekday AI runs: 7:30 AM, 8:00 AM, and 12:30 PM. That is approximately fifteen scheduled AI runs per week.

Replace routine AI work with local weekly or on-demand runs. Use Codex/ChatGPT for strategic review, difficult research, final review of important positioning, and troubleshooting.

### Local inference and web research are different modes

The system needs explicit modes:

- **Local-only:** use approved vault files and cached sources.
- **Human-source:** use URLs or documents supplied by a person.
- **Lightweight web-fetch:** fetch a small allowlist of approved pages.
- **Escalation:** use ChatGPT/Codex only when current research or judgment is genuinely needed.

A local model cannot automatically provide current web knowledge.

### Notion should not be called canonical before it exists

The repository contains a functioning Markdown/Obsidian operating system, but no Notion schema, IDs, adapter, sync rules, or conflict strategy. For the MVP:

- Markdown is the operational source of truth.
- Notion is the human marketing workspace.
- Only approved summaries or deliverables are exported to Notion.
- Avoid bidirectional sync.

### Most proposed “agents” are workflows or validators

Freshness Review is a validator. Commitment Tracker is a scheduled query. Experiment Analyst is a report template. Content Repurposing is a transformation step. Marketing Router is task classification.

The initial system needs three capabilities, not twelve agents:

1. Evidence extraction and research synthesis.
2. Content drafting and repurposing.
3. Deterministic QA and approval preparation.

### Linear integration is premature

Linear is the right product system, but API and webhook integration creates authentication and maintenance work. Start with a manually maintained product-truth file and occasional copied release notes. Add read-only Linear access only after stale product claims become a demonstrated problem.

## Revised minimal architecture

```text
Markdown vault
  ├── strategy and product truth
  ├── research notes
  ├── content drafts
  ├── experiments
  └── task queue
          |
          v
Local Python runner
  ├── deterministic workflow logic
  ├── Ollama local model
  ├── source and claim validator
  ├── SQLite run ledger
  └── Markdown output writer
          |
          +--> optional Notion export
          +--> optional Linear reference
          +--> human approval
```

| Component | Recommendation |
| --- | --- |
| Model runtime | Ollama |
| Model | Benchmark one small general model first |
| Orchestration | Plain Python functions initially |
| Storage | Markdown plus SQLite |
| Scheduler | Windows Task Scheduler |
| Marketing UI | Existing Obsidian vault; optional Notion export |
| Product context | Manual Linear export initially |
| Web research | Optional approved-page fetcher or human-provided sources |
| API framework | None initially |
| Database server | None |
| Hosting | None |
| Notifications | None initially |
| Codex/ChatGPT | Escalation and periodic review only |

Introduce LangGraph only if plain Python cannot handle a real need for branching, resumability, or approval interrupts.

## Minimal first workflow

1. Read a task from Markdown.
2. Load positioning, ICP, product truth, and relevant research.
3. Ask Ollama for a structured brief.
4. Ask Ollama for a draft.
5. Run deterministic checks:
   - audience is present;
   - CTA is present;
   - no unsupported claims;
   - no invented proof;
   - no pricing or commitment language;
   - sources are included.
6. Save the draft, claim checklist, source list, run metadata, and approval request.
7. Human reviews the Markdown artifact.
8. Approved content may be copied to Notion or the website manually.

## Cost-minimizing operating model

### Run locally

- task classification;
- research-note cleanup;
- content repurposing;
- claim checking;
- experiment summaries;
- stale-file detection;
- weekly dashboard generation.

### Reserve Codex/ChatGPT for

- architecture decisions;
- difficult strategic questions;
- high-value market research;
- reviewing local-model output;
- final approval of important positioning;
- diagnosing failures.

### Recommended cadence

- Local automation: weekly or on demand.
- Human review: weekly.
- Codex/ChatGPT review: monthly or when blocked.
- Notion update: only when a deliverable or decision changes.
- Linear review: only around relevant product releases.

## Prioritized changes

### P0: change before building

1. Define the zero-cost boundary.
2. Reduce or pause routine scheduled ChatGPT/Codex work.
3. Make the vault the temporary operational source of truth.
4. Reduce the first build to one workflow with three capabilities.
5. Confirm hardware before selecting an Ollama model.
6. Add data classes: Public, Internal, Confidential, Personal/Sensitive.

### P1: build first

1. Install Ollama and benchmark one local model.
2. Create a plain Python runner.
3. Add SQLite run metadata.
4. Implement research -> draft -> QA -> approval.
5. Add deterministic claim and approval checks.
6. Schedule one weekly local run.
7. Keep publishing, outreach, pricing, and commitments manual.

### P2: add only after demonstrated need

1. Notion one-way export.
2. Linear read-only import.
3. LangGraph for real branching or resumability.
4. Lightweight approval dashboard.
5. Competitor monitoring.
6. Automated freshness checks.
7. Additional specialized workflows.

## Revised success criteria

The MVP succeeds after ten local runs with:

- no paid API calls;
- no Codex/ChatGPT dependency;
- valid structured outputs;
- traceable source files;
- no unsupported product claims;
- no sensitive-data leakage;
- useful drafts requiring limited human revision;
- repeatable execution on the local machine.

The central recommendation is to build a **local marketing workbench**, not a full autonomous marketing platform. Prove one reliable local workflow saves time before adding APIs, webhooks, dashboards, or a larger agent team.
