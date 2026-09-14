# Isolated sandbox pool

The repository provides three isolated Cove sessions. Each has a dedicated PostgreSQL container
and volume, API port, web port, and Data Protection key directory. Runtime state is gitignored in
`.sandbox/registry.json`; the pool definition lives in `scripts/Initialize-SandboxPool.ps1`.

```powershell
./scripts/Initialize-SandboxPool.ps1       # provision, migrate, seed, and start all three
Get-Content .sandbox/registry.json
./scripts/Claim-Sandbox.ps1 -TaskId CPM-xx.yy
./scripts/Release-Sandbox.ps1 -SandboxId sandbox-1 -TaskId CPM-xx.yy
```

If claim reports no available slot, the agent waits and retries. It must not reuse or stop another
agent's slot. Before UI work or recording, run `Test-LocalStack.ps1` against the claimed ports.

Findings from the M10 cockpit recording/demo are encoded in this setup: separate ports avoid the
Superset/PostgreSQL collision; separate volumes prevent cross-session data; readiness must pass
before browser work; Release builds avoid stale binaries; HTTPS plus trusted dev certificates are
required for secure cookies; and temporary recording resources must be torn down after capture.
