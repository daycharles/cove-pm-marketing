# CovePM AI Marketing Team - Implementation Plan

Date: 2026-09-20  
Status: proposed architecture and build plan; revised after independent red-team review  
Decision owner: CovePM

## Executive decision

### Cost and complexity revision

The first release must target **$0 incremental spend** and minimal Codex/ChatGPT usage. The original multi-agent architecture is retained as a future-state design, but the MVP is intentionally smaller:

- local Markdown vault as the temporary operational source of truth;
- one plain-Python workflow;
- Ollama with one benchmarked local model;
- SQLite run ledger;
- Windows Task Scheduler;
- manual or one-way Notion export;
- manual Linear context until API integration is justified.

Do not install LangGraph, FastAPI, Postgres, webhooks, or a custom dashboard in the first release. Add them only when a measured workflow limitation requires them.

Use **Notion as the intended human-facing marketing workspace**, **Linear as the product-development source of truth**, and a small **local-first marketing workbench** as the first automation layer.

Do not create a Jira project for this initiative. Jira is not currently part of the operating model and would add an unnecessary third work-management system.

The target system is:

```text
Notion marketing workspace  <-->  Local agent service  <-->  Linear product workspace
          |                              |
          v                              v
   Human approvals                  Local evidence/action store
          |                              |
          +----------> Website/Git repository (approved changes only)
```

The existing Obsidian vault remains the operational source of truth during the MVP. After the workflow proves useful, Notion can become the human-facing marketing workspace through a one-way export. This avoids prematurely creating two competing live task systems.

## What exists today

### Already working

- A Git-backed Obsidian vault inside the marketing-site repository.
- Positioning centered on maintenance speed with broader property-management-suite positioning.
- ICP/persona questions, research index, experiment log, lead and pilot templates.
- A Markdown-backed Kanban board and Tasks/Dataview/Templater/Kanban plugins.
- A Dashboard and schedule/calendar notes.
- Weekday scheduled ChatGPT/Codex work:
  - 7:30 AM daily brief
  - 8:00 AM morning marketing session
  - 12:30 PM midday marketing session
- Dated session notes saved under `Sessions/`.
- Approval rules covering spend, pricing, discounts, price locks, agreements, commitments, and product promises.
- A static website connected to the same repository and OpenAI hosting configuration.

### Not implemented yet

- A local agent runtime.
- A structured database for agent runs, claims, approvals, metrics, or commitments.
- A Notion API/webhook adapter.
- A Linear API/webhook adapter.
- A real action ledger.
- An agent control-room dashboard.
- Automated product-change-to-marketing-impact review.
- Automated content freshness review.
- A formal evaluation suite for agent quality.

The current implementation is therefore a strong operating notebook and scheduled-assistant workflow, not yet a multi-agent system.

## Recommended system of record

### Notion: marketing operations

Notion should contain the live marketing objects a human needs to browse, edit, approve, and discuss:

1. **Marketing Work Queue** - tasks, owner, agent, status, priority, due date, source, approval state.
2. **Research Findings** - question, segment, buyer, finding, sources, confidence, expiry/recheck date.
3. **Content Assets** - draft, channel, audience, source update, status, reviewer, publish state.
4. **Experiments** - hypothesis, audience, message, offer, channel, baseline, metric, result, decision.
5. **Leads / Opportunities** - company, role, fit rationale, pain evidence, next action, suppression state.
6. **Pilots** - scope, baseline, duration, success metrics, terms, approval state.
7. **Commitments** - decision, owner, deadline, source, status, overdue flag.
8. **Approvals** - requested action, risk category, artifact, reviewer, decision, timestamp.
9. **Agent Runs** - agent, trigger, input, output, sources, proposed action, actual action, result, errors.
10. **Metrics** - metric name, source, period, value, confidence, interpretation.

Notion's API supports creating and updating pages and database records, and current Notion guidance describes connection webhooks for reacting to changes in pages or databases. We should use the API for durable writes and only add webhooks after the first polling/scheduled workflow is stable. [Notion API integration guidance](https://www.notion.com/help/create-integrations-with-the-notion-api)

### Linear: product truth

Linear should remain focused on product development:

- product issues and projects;
- releases and product-change signals;
- feature names and current implementation status;
- technical decisions that affect marketing claims;
- links from marketing assets to product issues where relevant.

The marketing agents should initially have read access plus the ability to add a comment or link only when explicitly requested. They should not create or modify product issues automatically in the first release.

Linear exposes a GraphQL API and webhooks for issue, project, document, and related changes. Webhook signatures and timestamps must be verified before processing events. [Linear API](https://linear.app/developers/graphql) · [Linear webhooks](https://linear.app/developers/webhooks)

### Git / website repository: implementation truth

The repository remains the source of truth for:

- the public website;
- product-positioning reference files;
- prompts, schemas, and agent configuration;
- local service code;
- exported or sanitized marketing evidence;
- deployment configuration.

No agent should publish a website change directly from an unapproved Notion draft.

## Chosen technical stack

### Orchestration

**LangGraph** is the recommended workflow engine because the system needs resumable state, explicit approval interruptions, retries, and durable run history. CrewAI can be revisited if the project later benefits from more role-oriented autonomous crews, but it should not be the first dependency.

### Local inference

**Ollama** as the default local model server, with model selection based on a small benchmark:

- routing and classification model;
- synthesis/research model;
- copywriting/revision model.

The service must support a model fallback or a human review path when a local model is unavailable.

### Application layer

- Python.
- FastAPI for a small local API and health/status endpoints.
- Pydantic for structured inputs and outputs.
- SQLite for the first local run/action store.
- Optional Postgres only when multiple workers, shared access, or reporting needs justify it.
- Notion API client for marketing reads/writes.
- Linear GraphQL client for product reads and controlled comments.
- Scheduled jobs for periodic work; webhooks later for event-driven work.

### Human interface

Start with Notion views and the existing vault. Build a custom control room only after the first workflows produce useful records. The first dashboard should answer:

- What needs human approval?
- What changed since yesterday?
- Which evidence is weak or expired?
- Which commitments are overdue?
- Which experiments have results?
- What did each agent actually do?

## Agent library

Agents should be defined by ownership and goal, not just personality or role.

| Agent | Owns | First goal | Initial authority |
| --- | --- | --- | --- |
| Marketing Router | Incoming work and workflow selection | Route each task to the smallest safe workflow | Create run and recommendation |
| Market Intelligence | Segments, buyers, competitors, sources | Produce evidence that changes a decision | Research notes only |
| SEO / AI Search | Search themes and content gaps | Identify qualified organic opportunities | Briefs and research |
| Website Messaging | Public positioning and proof | Improve clarity and demo relevance | Drafts only |
| Content Repurposing | Approved updates and channel formats | Increase output without changing facts | Drafts only |
| Competitor Watch | Selected competitor sources | Detect relevant, verified outliers | Findings and optional tasks |
| Commitment Tracker | Decisions, owners, deadlines | Prevent agreed work from disappearing | Flags; never guesses |
| Freshness / Change Review | Product, website, and message consistency | Catch stale or contradictory claims | Correction queue |
| Experiment Analyst | Baselines, results, learnings | Convert activity into measurable learning | Reports and next-test proposals |
| Pipeline Research | Approved prospect research | Generate qualified conversations | Research; no sending |
| Pilot Designer | Pilot scope and measurement | Turn interest into a measurable pilot plan | Drafts; commercial approval required |

The first build should implement one workflow with three capabilities: evidence extraction, content drafting/repurposing, and deterministic QA/approval preparation. The Router, Market Intelligence, Website Messaging, and Experiment Analyst remain future named roles, but should initially be plain workflow steps rather than separate autonomous agents.

## Core workflow

### Example: create a maintenance landing-page draft

1. A person creates or approves a task in Notion.
2. The Router classifies the task and assigns a workflow.
3. The workflow reads CovePM positioning, ICP context, current product reference, and relevant research.
4. Market Intelligence retrieves missing evidence and records sources, access dates, confidence, and gaps.
5. Website Messaging drafts the page with a clear segment, problem, proof, CTA, and claims list.
6. Freshness / Change Review compares every product or integration claim against current product truth in the repository and Linear.
7. The system creates an Approval record in Notion.
8. A human approves, requests revision, or rejects the draft.
9. The final artifact is saved to Notion and optionally exported to the repository.
10. The Agent Run record stores inputs, sources, outputs, approval decision, and next metric.

### Example: product change review

1. A Linear issue or project update changes status.
2. The integration identifies whether the change touches a marketable capability.
3. The agent checks website copy, content assets, sales language, and product-reference claims.
4. It reports verified mismatches and links to the Linear source.
5. It creates a Notion correction task only when an actual mismatch exists.

## Approval and safety model

### Agents may do automatically

- read approved sources;
- summarize and classify;
- create research findings;
- draft content;
- create experiment proposals;
- create Notion records in non-published states;
- add comments or links to approved marketing tasks;
- generate reports and reminders.

### Human approval required immediately before

- publishing or editing public website copy;
- sending emails, DMs, or outreach;
- spending money or changing ad budgets;
- making pricing, discount, price-lock, integration, timeline, or outcome commitments;
- creating or accepting agreements;
- transmitting sensitive customer, resident, or prospect data to a third party;
- changing product truth or Linear work outside a pre-approved scope.

Every external action should be idempotent, reversible where possible, linked to an approval record, and written to the action ledger.

## Migration from the current vault

Do not throw away the current vault. Migrate in stages:

### Stage 1: preserve and clarify

- Keep the current schedules and daily sessions running.
- Treat the vault as the durable archive during migration.
- Add Notion database IDs and links to the schedule inventory once available.
- Stop adding new parallel task systems.

### Stage 2: create Notion databases

- Build the ten databases above, starting with Work Queue, Research, Content Assets, Experiments, Approvals, and Agent Runs.
- Add views for Inbox, This Week, Awaiting Approval, Evidence Gaps, Content Calendar, and Experiments.
- Import or manually link the existing positioning, ICP, research, experiment, lead, and pilot structures.

### Stage 3: add a one-way local sync

- Local service reads approved Notion work.
- Local service writes session summaries, findings, drafts, and run logs back to Notion.
- The vault receives a dated export or snapshot for Git history.
- No bidirectional conflict resolution until the object model is stable.

### Stage 4: connect Linear read-only

- Pull product changes and relevant issue/project metadata.
- Map product capabilities to marketing claims.
- Generate freshness and change-review tasks in Notion.

### Stage 5: replace scheduled prompts gradually

- Keep the current ChatGPT/Codex schedules as a fallback and executive brief while the local service is evaluated.
- Move one workflow at a time to the local orchestrator.
- Retire a scheduled prompt only after the replacement has produced reliable results for at least two weeks.

## Build phases

### Phase 0: architecture and access inventory

- Define the zero-cost boundary.
- Confirm local machine hardware and whether Ollama can run acceptably.
- Define the first five synthetic tasks and evaluation rubric.
- Decide which current scheduled ChatGPT/Codex runs should be paused or retained as human-facing briefs.

### Phase 1: local foundation

- Create a separate service directory in the parent repository.
- Add configuration templates with no secrets committed.
- Implement plain Python workflow functions, structured schemas, SQLite run store, action ledger, and claim validator.
- Add Markdown input/output fixtures; defer live adapters.

### Phase 2: first useful workflow

- Implement Router -> Research -> Draft -> QA -> Approval.
- Read the vault's current positioning and product reference.
- Produce one Notion-ready artifact and one complete run record.

### Phase 3: optional Notion export

- Export approved summaries and deliverables to a selected Notion workspace.
- Keep Markdown as the operational source of truth until conflict rules are proven.
- Keep all public/pipeline actions blocked.

### Phase 4: optional Linear context and freshness review

- Start with manually maintained or exported Linear product context.
- Add read-only Linear queries only if stale product claims become a demonstrated problem.
- Add verified product-change detection.
- Create Notion correction tasks only for specific mismatches.

### Phase 5: control room and expansion

- Add a lightweight local dashboard or Notion executive view only after the first workflows produce useful records.
- Add competitor watch, commitment tracker, repurposing, and pilot design.
- Consider external connectors only when a measurable need exists.

## Evaluation rubric

Each workflow should be tested against:

- source correctness;
- unsupported-claim detection;
- citation/source completeness;
- structured-output validity;
- correct approval classification;
- repeatability;
- latency and local resource use;
- usefulness of the next action;
- absence of sensitive-data leakage;
- accurate action-log entries.

The first milestone is five successful end-to-end runs, not the number of agents installed.

## Risks

| Risk | Mitigation |
| --- | --- |
| Notion and vault become conflicting sources of truth | Notion is canonical for live marketing operations; vault is archive/configuration until migration stabilizes |
| Agent invents product claims | Product reference, Linear verification, claim QA, and approval gate |
| Too many connectors too early | Mock adapters first; add one live integration at a time |
| Local model quality is insufficient | Benchmark real tasks, use model routing, and preserve human review |
| Sensitive data enters prompts or logs | Data classification, redaction, retention rules, and no secrets in Git |
| Web research becomes noisy or unverifiable | Source URLs, dates, confidence, evidence thresholds, and explicit gaps |
| Automation creates duplicate tasks or messages | Idempotency keys, action ledger, and approval-linked execution |
| Product changes silently invalidate marketing | Linear read-only signals plus freshness/change review |

## Open decisions

1. Which Notion workspace/page should be the marketing root?
2. Which Linear team or project should be visible to the marketing integration?
3. Which marketing outcome is the first optimization target: demos, pilots, content velocity, or learning velocity?
4. Which segment gets first priority?
5. What data may leave the local machine, if any?
6. What hardware and model latency are acceptable?
7. Should the vault export be daily, per agent run, or only at approved milestones?
8. Which current ChatGPT scheduled tasks should remain permanently as human-facing briefs?

## Recommended next build order

1. Define the zero-cost boundary and pause unnecessary routine AI schedules.
2. Install Ollama only after confirming the local model benchmark target.
3. Create the local service skeleton with Markdown fixtures and a SQLite action ledger.
4. Implement one evidence-backed research-to-draft workflow.
5. Run ten local evaluations with no Codex/ChatGPT dependency.
6. Add one-way Notion export if it reduces human friction.
7. Add Linear context and a control-room view only after demonstrated need.

This design keeps Notion pleasant for marketing, Linear clean for product development, the repository safe for code and durable evidence, and the agent team useful without giving it authority it has not earned.
