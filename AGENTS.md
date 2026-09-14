# Codex working agreement

- During active work, provide a concise progress update at least every five minutes. This applies
  while the agent turn/session is running; the agent cannot initiate messages after the session is
  idle.

## Token-efficient execution

- Start each task with the outcome, relevant paths or components, constraints, and acceptance check.
- Search targeted paths first and read bounded file sections. Do not load whole documentation,
  repositories, or logs unless a prior result establishes a specific need.
- Use the configured `gpt-5.6-terra` at low reasoning for implementation, debugging, architecture,
  and final review. Use `gpt-5.6-luna` only for bounded routine tasks: code location, concise
  summaries, test-output triage, mechanical edits, and focused documentation lookup.
- Switch to higher reasoning or Astra only when the task has a stated, concrete complexity that
  low-reasoning Terra cannot address.
- For third-party library or framework documentation, use Context7 to retrieve the precise
  library and topic needed. Do not use it for repository source, general web research, or broad
  documentation dumps.
- Run focused verification by default. Broaden or repeat checks only after a change, failure, or
  release-critical concern justifies it.

## Allowance guardrails

- Check Codex usage before and after substantial tasks and record the result in
  `docs/codex-usage-log.csv` for the next two five-hour windows.
- Below 70% usage, follow the model policy above. At 70–89%, use Luna for routine work and reserve
  Terra for implementation and final verification. At 90%+, perform only bounded fixes, evidence
  gathering, or a handoff; defer exploration, broad refactors, and repeated test cycles.
- For unfinished work, leave a concise handoff: decisions, changed paths, remaining work, and
  verification status. Split unrelated objectives into separate tasks after a milestone completes.

## GitHub project tracking

- Routine `gh` commands for this repository do not require user approval; agents may use the
  authenticated GitHub CLI autonomously for issue, milestone, project-board, PR, release, and
  status-tracking operations within the task scope.
- Agents MUST update the linked GitHub Project board whenever they work on a task.
- At task start, add the issue to the project if it is missing and move it to `In progress`.
- When work is ready for review, move it to `In review`; when accepted, move it to `Done`.
- If blocked, leave a concise issue comment describing the blocker and keep the board status
  accurate rather than leaving the task appearing active.
- Before handing off, verify the board item status and include that status in the final report.

## Milestone completion regression gate

- A milestone cannot be marked complete while a branding change or any change that can affect
  the full product suite is unverified. Before completing the milestone epic, run the full
  regression suite against the final integrated state and record the command, result, and CI
  run or equivalent evidence on the actual milestone epic.
- This is a completion gate, not a per-commit requirement: the final regression evidence must
  cover all branding/full-suite changes included in the milestone before the epic is closed or
  otherwise declared complete.

## Roadmap epic standard

- New roadmap initiatives MUST use the established Epic:M structure: create a matching GitHub
  milestone (`M<n>`), an open parent issue titled `Epic: M<n> - <initiative>`, and numbered task
  issues titled `CPM-<n>.<yy> <task>` under that milestone.
- The parent epic body MUST include a concise Goal, Definition of done, a `See \`docs/backlog.md\``
  reference, and a checked Tasks list linking every child issue. It MUST state that later delivery
  slices remain backlog until explicitly selected when sequencing matters.
- Child issues MUST contain a concrete goal/scope and acceptance criteria, use the appropriate
  `area:*` labels, belong to the matching milestone, and be added to the GitHub Project in
  `Backlog` unless implementation has explicitly been selected.
- Do not create ad-hoc milestone issues or parallel epic formats; revise the structure before
  handing off if an issue is created in the wrong form.

## Agent assignment execution

- When the user asks to assign agents to work on tasks, agents MUST begin the actual implementation immediately after assignment.
- Board setup, issue comments, planning, or status changes alone do not count as starting the work.
- A workstream may report completion only with concrete implementation evidence: changed paths or commits, verification results, and accurate remaining-scope/blocker details.

## Local session health and parallel runs

- Before browser walkthroughs or UI acceptance checks, run `scripts/Test-LocalStack.ps1` with the
  session's API origin and ports. Require that session's `/health/ready` to be healthy; a reachable
  login page does not prove that its API or database is usable.
- Every concurrent session must use a distinct web port, API port, and PostgreSQL port (and its own
  deployment state/data volume where applicable). Never stop, restart, or reconfigure another
  session to resolve a collision; identify the owning process and choose a free session port set.
- If readiness is unhealthy, stop UI diagnosis and report the failing dependency and exact session
  ports first. Do not present a login failure as an application regression until the selected
  session's readiness check passes.
- UI work is not complete until a short feature/fix video clip has been captured from the claimed
  sandbox and its artifact path is recorded in the task evidence. A live demo is optional; do not
  wait for the user to be present before recording.
- Temporary demo/recording sessions must be torn down after capture, including their processes,
  containers, volumes, and browser tabs. The three-sandbox pool remains running unless explicitly
  stopped.
