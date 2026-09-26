no# Averion Compass AI Marketing Team - Implementation Plan

Date: 2026-09-20  
Status: proposed architecture and build plan; revised after independent red-team review  
Decision owner: Averion Compass

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

LangGraph is now the one deliberate orchestration dependency: it runs locally inside the workbench and does not require a hosted service. Continue to defer FastAPI, Postgres, webhooks, and a custom dashboard until a measured workflow limitation requires them.

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

### First build status

The minimal local workbench is now implemented under `agent-workbench/`:

- Markdown task input;
- one-call Ollama workflow contract;
- LangGraph graph wrapper with prepare -> website research -> market/competitor research -> generate -> QA -> persist nodes;
- deterministic CTA, source, approval, and unsupported-number checks;
- SQLite run ledger;
- Markdown output artifacts;
- mock mode and unit tests;
- weekly PowerShell runner.

Ollama installation remains pending because the Windows package download stalled; the workbench is currently verified in mock mode and is ready to use with Ollama once the installer completes.

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

**LangGraph** is now installed in the workbench virtual environment and wraps the minimal workflow as explicit `prepare -> generate -> QA -> persist` nodes. The graph uses a local in-memory checkpoint for the current run; durable business metadata remains in SQLite. Human approval is still represented as an output state rather than an external side effect.

### Local inference

**Ollama** as the default local model server, with model selection based on a small benchmark:

- routing and classification model;
- synthesis/research model;
- copywriting/revision model.

The service must support a model fallback or a human review path when a local model is unavailable.

### Application layer

- Python.
- LangGraph in the isolated `agent-workbench/.venv` environment.
- FastAPI only if a local API or health/status endpoint becomes necessary.
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

## Active marketing team scope

The MVP is a workbench, but the intended team is an active full-funnel system. We will grow it in five operating lanes:

1. **Intelligence** - find buyer pain, competitor movement, search demand, and evidence.
2. **Content** - create, repurpose, QA, schedule, and refresh marketing assets.
3. **Pipeline** - find and qualify companies, record fit rationale, and propose next actions.
4. **Outreach** - personalize email and follow-up drafts, manage approvals, send through an approved mailbox, and honor opt-outs.
5. **Measurement** - record baselines, attribute activity, report results, and recommend the next experiment.

The workbench should produce useful work in all five lanes, but external actions remain staged. Local research and drafting can be autonomous; lead contact and email sending require a deliberate approval state until the sending path, mailbox, suppression list, and compliance checks are implemented.

### The team as workflows

| Lane | Workflow | Inputs | Output | Default authority |
| --- | --- | --- | --- | --- |
| Intelligence | Segment and buyer research | Approved URLs, vault context, human questions | Dated finding with source, confidence, gap, next action | Autonomous local run |
| Intelligence | Competitor watch | Allowlisted competitor sources | Relevant change/outlier report | Autonomous report; task creation optional |
| Content | Content brief | Finding, segment, product truth, goal | Audience/message/proof/CTA brief | Autonomous draft |
| Content | Content factory | Approved source update or brief | Website, blog, email, social, sales variants | Draft only until approved |
| Content | Freshness and message QA | Published copy, product truth, current positioning | Specific correction queue | Autonomous flags |
| Pipeline | Lead discovery | Approved public company sources or supplied lists | Company record and fit rationale | Research only |
| Pipeline | Qualification | Lead record, ICP, pain evidence | Fit score, disqualifier, next action | Autonomous recommendation |
| Outreach | Personalization | Qualified lead, approved message, source evidence | Draft email and personalization rationale | Draft only |
| Outreach | Send and follow-up | Approved draft, consent/suppression state, mailbox | Sent message or scheduled follow-up | Human approval immediately before send |
| Measurement | Experiment report | Baseline, campaign activity, outcome data | Result, interpretation, next decision | Autonomous report |

### Lead and email operating rules

The system should find leads and prepare outreach, but zero incremental spend changes the acquisition strategy:

- No paid lead databases, enrichment services, email sequencers, or bulk-sending platforms in the MVP.
- Use manually supplied company lists and verifiable public company information.
- Prefer company-level research over personal contact scraping.
- Store only the minimum contact data needed for an approved action.
- Maintain a suppression/opt-out record before any send.
- Every draft includes why the lead fits, what source supports the personalization, the CTA, and the opt-out path.
- Never send a message that contains an invented integration, benchmark, customer, price, discount, guarantee, or outcome.
- Start with human-approved individual or small-batch sending through an existing approved mailbox. Automated sending is a later capability, not an assumption of the free MVP.

Email sending is not merely another content step: it is an external, representational action with privacy, anti-spam, deliverability, and account-risk implications. The implementation must stop at `approved-to-send` and wait for a human action until those controls are verified.

### Content factory operating rules

One approved source update should produce a content package:

- primary asset or website section;
- newsletter/email version;
- social version;
- sales/demo talking point;
- optional pilot follow-up;
- claim/source checklist;
- channel-specific review state.

The source update is the immutable fact base. Variants may change length, framing, and CTA, but not facts, numbers, dates, or product capability claims.

### Local cadence for an active team

Use local scheduled jobs rather than Codex/ChatGPT runs:

- **Daily, no LLM:** scan task queue, identify due work, stale drafts, missing approvals, and overdue commitments.
- **Two or three times weekly, local LLM:** process a small batch of research/content tasks.
- **Weekly, local LLM:** create the marketing report, experiment readout, and next-week queue.
- **On demand:** lead research, content package, email draft, or follow-up workflow.
- **Monthly, Codex/ChatGPT:** architecture review, high-value strategy, or difficult market research.

The local runner should prefer batch inputs and one model call per work item. Deterministic scans should never invoke the LLM.

## Core workflow

### Example: create a maintenance landing-page draft

1. A person creates or approves a task in Notion.
2. The Router classifies the task and assigns a workflow.
3. The workflow reads Averion Compass positioning, ICP context, current product reference, and relevant research.
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

### Example: lead-to-email workflow

1. A task requests leads for a selected segment and buyer.
2. Pipeline Research gathers only approved, verifiable company-level information.
3. Qualification scores fit, urgency, reachability, authority signals, and measurable pain.
4. Weak-fit leads are marked nurture or disqualified instead of being pushed into outreach.
5. Outreach creates a personalized draft with source rationale, CTA, and opt-out language.
6. QA checks claims, personalization, suppression state, and approval requirements.
7. The draft enters `awaiting-send-approval`.
8. A human approves the specific recipient, message, and channel immediately before sending.
9. The sender records the result, timestamp, message version, and opt-out state.
10. Follow-up is proposed only when it remains within the approved cadence and no opt-out or negative response exists.

### Example: content package workflow

1. A product update, research finding, or approved campaign brief is marked `ready-to-repurpose`.
2. The Content Factory creates channel-specific variants from the same fact base.
3. Claim QA compares each variant with the product truth and source update.
4. The package enters `awaiting-content-approval`.
5. A human approves individual channels or the entire package.
6. Publishing remains manual until an approved CMS/scheduler path is available.
7. Measurement records the published URL, date, channel, CTA, and result window.

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
- Implement plain Python workflow functions, LangGraph graph nodes, structured schemas, SQLite run store, action ledger, and claim validator.
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

### Phase 6: content factory

- Add `ready-to-repurpose`, `in-review`, `approved`, `published`, and `refresh-needed` states.
- Implement one source-update-to-content-package workflow.
- Add channel-specific templates and deterministic claim checks.
- Keep publishing manual.

### Phase 7: lead engine

- Add lead and company schemas to the local Markdown/SQLite layer.
- Add fit scoring, disqualification, nurture, suppression, and next-action rules.
- Start with supplied lists and public company pages; do not add paid enrichment.
- Produce a weekly qualified-lead review.

### Phase 8: approved outreach

- Add email draft generation and personalization rationale.
- Add approval records for recipient, message, channel, and send date.
- Add suppression and opt-out checks before the send action.
- Use an existing approved mailbox manually or through a narrow adapter.
- Test with internal addresses before any prospect outreach.

### Phase 9: measurement and iteration

- Track sent, delivered, bounced, replied, opted out, booked, qualified, and disqualified states where the approved mailbox exposes them.
- Connect outcomes to the lead, message version, experiment, and source evidence.
- Add follow-up proposals and stop conditions.
- Only consider automated sending after deliverability, compliance, and review controls are proven.

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
| Notion and vault become conflicting sources of truth | Vault remains canonical during the MVP; Notion is a future one-way marketing workspace until migration stabilizes |
| Agent invents product claims | Product reference, Linear verification, claim QA, and approval gate |
| Too many connectors too early | Mock adapters first; add one live integration at a time |
| Local model quality is insufficient | Benchmark real tasks, use model routing, and preserve human review |
| Sensitive data enters prompts or logs | Data classification, redaction, retention rules, and no secrets in Git |
| Web research becomes noisy or unverifiable | Source URLs, dates, confidence, evidence thresholds, and explicit gaps |
| Automation creates duplicate tasks or messages | Idempotency keys, action ledger, and approval-linked execution |
| Product changes silently invalidate marketing | Linear read-only signals plus freshness/change review |
| Free outreach damages deliverability or violates requirements | Small approved batches, suppression list, opt-out handling, no purchased lists, human approval |
| Lead research becomes personal-data scraping | Company-first research, minimum data collection, allowlists, retention limits, and escalation |
| Content volume outruns review quality | Per-channel approval state, source-update fact base, claim QA, and publishing hold |

## Open decisions

1. Which Notion workspace/page should be the marketing root?
2. Which Linear team or project should be visible to the marketing integration?
3. Which marketing outcome is the first optimization target: demos, pilots, content velocity, or learning velocity?
4. Which segment gets first priority?
5. What data may leave the local machine, if any?
6. What hardware and model latency are acceptable?
7. Should the vault export be daily, per agent run, or only at approved milestones?
8. Which current ChatGPT scheduled tasks should remain permanently as human-facing briefs?
9. Which existing mailbox, if any, is approved for prospect communication?
10. What outreach volume and cadence are acceptable before the workflow must stop for review?
11. Which lead sources are explicitly allowed, and which are prohibited?
12. What constitutes a qualified opportunity for Averion Compass: portfolio size, urgency, authority, systems, budget, or another threshold?
13. Which content channels are in scope first: website, blog, newsletter, LinkedIn, or sales enablement?

## Recommended next build order

1. Define the zero-cost boundary and pause unnecessary routine AI schedules.
2. Install Ollama only after confirming the local model benchmark target.
3. Create the local service skeleton with Markdown fixtures and a SQLite action ledger.
4. Implement one evidence-backed research-to-draft workflow.
5. Run ten local evaluations with no Codex/ChatGPT dependency.
6. Add one-way Notion export if it reduces human friction.
7. Add Linear context and a control-room view only after demonstrated need.
8. Add lead qualification and research before any sending capability.
9. Add approved, human-gated email sending only after suppression and compliance checks are tested.
10. Add automated follow-up and broader channel publishing only after measured deliverability and content results.

This design keeps Notion pleasant for marketing, Linear clean for product development, the repository safe for code and durable evidence, and the agent team useful without giving it authority it has not earned.
