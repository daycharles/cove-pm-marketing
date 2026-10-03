---
type: marketing-session
date: 2026-10-02
status: partial-external-removal
segment: private multifamily operators
objective: retire old marketing schedules and move to a human-directed Hermes plan
approval_required: true
---

# 2026-10-02 - Marketing Automation Retirement

## Decision and completed work

The owner directed the team to retire prior recurring marketing runs and the Marketing OS report/mobile approval sync, retaining only the evidence needed for a fresh marketing plan. The Windows `\CovePM Marketing OS Monitor` task in the separate active checkout was deleted via Task Scheduler. An exact-name query then returned "The system cannot find the file specified." The task previously fired on weekdays at 8:00, 10:00, 12:00, 14:00, 16:00 and 18:00 local time; the last observed run was 2026-10-02 14:00 with result 0. No Hermes cron jobs existed, and none were created.

Removed the hosted `/api/approvals` and `/api/run-reports` bridge from the active checkout's `agent-workbench/run-weekly.ps1`, including its local token-file read. The remaining local script is manual-only and still generates a social brief if run; it is not used by the new plan. The hosted API and old hosted records were not deleted. The durable vault now points to [[Plans/Hermes Marketing Plan - 2026-10-02]], with on-demand specialist work, dated source notes and human-reviewed exact artifacts. [[Dashboard]], [[Schedules/Marketing Schedule]], [[Schedules/Marketing Calendar]] and [[Schedules/Automation Inventory]] record the transition; historical notes remain evidence, not running instructions.

## Incomplete external removal

The ChatGPT Scheduled tasks page redirected to login and account access was not available, so the old Morning/Midday ChatGPT tasks (historically paused) and the reported 7:30 AM ChatGPT Daily Brief (historically active) could **not** be removed or live-verified. The owner must remove them in the ChatGPT account, or grant access via the approved browser-vault flow on a later turn. Do not state that all former schedules are canceled.

## Current target, guardrails and next action

**Buyer/question:** Private multifamily operations or maintenance owner; can Winn's published corporate information email properly route a technology-workflow inquiry, and who would evaluate the inspection → follow-up → unit review workflow? Winn is a conditional research slot, not a verified buyer or technology route. **Hypothesis:** a verified appropriate company route plus role evidence can support a useful discovery draft. **Baseline:** 0/2 appropriate routes documented in the two-slot internal packet; no contact, response or qualified conversation is established. **Checkpoint:** 2026-10-05 internal source review, not a procurement deadline.

Research official sources with access dates; RFP and independent Review check claims. Draft only if route and role evidence warrants it. Human approval, suppression/opt-out, exact recipient and message, idempotency and audit must precede any external action. The old mobile bridge is gone; there is no substitute sending authority. Social production remains deferred. No posts, outreach, form submission, CRM/calendar change or spend were performed by this transition.