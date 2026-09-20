# CovePM Marketing Team Productization Roadmap

Date: 2026-09-20  
Status: active roadmap  
North star: operate the current Marketing OS as the first measurable pilot of a full AI marketing team.

## Implementation status — 2026-09-20

The local pilot implementation now includes:

- pilot event and human-effort metrics;
- approval-gated lead-to-demo workflow;
- fact-based multichannel content factory;
- inbound intent classification, escalation, and response approval;
- external-pilot configuration validation and onboarding artifact;
- combined funnel and operating report;
- 30 automated tests across the workbench.

Still intentionally manual: external email sending, social publishing, mailbox access, CRM writes,
and calendar changes. These are connector work, not missing workflow logic; the local interfaces
already stop before those external side effects and preserve approval records.

## Product direction

CovePM is building a full marketing department for property-management companies: strategy, content, social publishing, lead nurture, inbox response, qualification, demo scheduling, and reporting. The current vault and local workbench are the internal pilot. Every capability should be proven here before it is packaged for customers.

## Current state

### Working now

- Git-backed marketing vault and operating dashboard.
- Local Python workbench with LangGraph orchestration, mock mode, Ollama support, SQLite run ledger, deterministic QA, and Markdown outputs.
- Control-room queue, approval records, weekly review, and company-first lead qualification.
- Pilot-readiness scorecard and approval-gated discovery artifacts.
- Research, content-drafting, website-audit, and lead-review workflows.

### Not yet a full marketing team

- No live social publishing connector.
- No live mailbox or inbox triage.
- No CRM or calendar execution path.
- No durable suppression/opt-out system for outreach.
- No end-to-end lead-to-demo workflow with outcome attribution.
- No customer-facing tenant, permissions model, or service-level reporting.

## Roadmap

### Phase 0 — Instrument the internal pilot

**Goal:** Make the current implementation behave like a customer-ready service, even while it remains local and human-gated.

Deliverables:

- Pilot charter and operating rules.
- Canonical workflow/action/approval/outcome records.
- Weekly scorecard covering activity, quality, human effort, and pipeline impact.
- Ten evaluated workbench runs and ten reviewed company candidates.
- Explicit evidence of every human intervention and failure.

Exit criteria:

- A new task can be routed, processed, reviewed, approved, and measured locally.
- We can explain exactly what the system did and what a human did.
- We have baseline metrics for response time, output quality, approval rate, and effort.

### Phase 1 — Prove the revenue loop

**Goal:** Build the first useful marketing-team loop: discover a company, qualify it, prepare a personalized message, and propose a demo.

Deliverables:

- Lead queue with evidence, disposition, reviewer, approval, and outcome capture.
- Outreach-draft workflow with source rationale, claim QA, suppression check, and opt-out language.
- Manual approval immediately before every send.
- Demo-request and meeting-outcome records.
- Weekly pipeline report linking lead source, message, meeting, and result.

Exit criteria:

- At least ten reviewed candidates.
- At least five approved outreach drafts.
- No unsupported claims or missing approval records.
- A repeatable path from approved lead to scheduled demo.

### Phase 2 — Add the content department

**Goal:** Turn one approved source update into coordinated, measurable channel output.

Deliverables:

- Content package workflow for website, email, social, and sales/demo variants.
- Content calendar with approval and publish states.
- Social scheduling integration or a controlled export path.
- Email campaign drafts and follow-up sequences.
- Freshness review against product truth and current positioning.

Exit criteria:

- One source update produces a complete approved package.
- Publishing and engagement outcomes are recorded.
- The system can recommend refreshes rather than only creating new content.

### Phase 3 — Add the coordinator layer

**Goal:** Make the team responsive to inbound demand.

Deliverables:

- Website/contact-form and inbox intake.
- Intent classification and response drafts.
- Approved knowledge base for product, pricing boundaries, pilots, and FAQs.
- Calendar availability and demo scheduling path.
- Escalation for complaints, legal/privacy/security questions, pricing, and unusual requests.

Exit criteria:

- Routine inbound questions receive accurate responses within the target SLA.
- Qualified inquiries can reach a booked demo without manual coordination.
- Sensitive cases reliably stop for human review.

### Phase 4 — Package for external pilots

**Goal:** Sell a bounded version of the system before building a broad platform.

Deliverables:

- Customer onboarding checklist and configuration schema.
- Tenant-specific brand voice, approved facts, channels, users, and permissions.
- Customer-facing weekly report.
- Pilot agreement, scope, success criteria, and review cadence.
- Support and escalation runbook.

Exit criteria:

- One external customer can run a bounded workflow without sharing CovePM internal records.
- Setup time and recurring human effort are measurable.
- Customer outcomes can be reported without overstating attribution.

### Phase 5 — Scale into a product

**Goal:** Replace local-only operations with a reliable multi-customer service where evidence supports it.

Consider only after Phase 4:

- Hosted control room and tenant isolation.
- Connector management for social, email, CRM, and calendar.
- Role-based permissions and audit logs.
- Usage metering, billing, and service health.
- Model routing and cost controls.
- Customer self-service configuration.

## First 30-day execution plan

### Week 1 — Baseline and operating discipline

- Run the control-room dashboard and weekly review.
- Establish the pilot scorecard and record baseline human effort.
- Review the existing queue and select one priority segment and one buyer.
- Complete the first five evaluated workbench tasks.

### Week 2 — Lead-to-demo preparation

- Review ten company candidates using the lead queue.
- Resolve evidence gaps and record dispositions.
- Produce five personalized outreach drafts with approval state.
- Define the demo request and meeting outcome record.

### Week 3 — Content department slice

- Select one approved source update.
- Produce website, email, social, and sales variants.
- Run claim/freshness QA and approve the package.
- Publish through the currently approved manual path and record results.

### Week 4 — Review and productize

- Compare output quality, pipeline movement, and human effort to baseline.
- Identify the three most repeated manual interventions.
- Promote stable workflows into reusable customer-facing playbooks.
- Decide whether the next build is inbox response, scheduling, or a connector based on measured bottleneck—not excitement.

## Operating metrics

### Activity

- tasks completed by workflow
- content assets produced and approved
- leads researched and reviewed
- drafts prepared and sent
- demos requested, booked, held, and missed

### Quality and safety

- approval rate
- unsupported-claim defects
- stale-source defects
- escalation rate
- opt-out/suppression errors
- human correction time

### Business value

- qualified conversations
- demo conversion rate
- pilot opportunities created
- pipeline influenced
- cost or human hours per qualified opportunity

## Non-negotiable boundaries

- No autonomous pricing, discounts, contracts, guarantees, or product commitments.
- No outbound sending without recipient-level approval until suppression, compliance, and deliverability controls are proven.
- No personal-contact discovery as a prerequisite for the pilot.
- No customer data mixed with internal CovePM data.
- No claim is marketable until its source, date, and approval status are recorded.

## Decision rule

Build the next capability only when the current stage has a measurable bottleneck, a defined approval boundary, and a clear customer outcome. The objective is not to accumulate agents; it is to prove that a coordinated marketing team can produce qualified demand with decreasing human effort and trustworthy behavior.
