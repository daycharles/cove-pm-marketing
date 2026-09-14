# Isolated sandbox pool

The repository provides three isolated Cove sessions. Each has a dedicated PostgreSQL container
and volume, API port, web port, Data Protection key directory, and per-session Next.js build/lock
directory under `apps/web/.next-sandbox-<id>`. Each API is published into its own
`.sandbox/<id>/api` directory so running APIs do not lock the shared build output.
Runtime state is gitignored in `.sandbox/registry.json`; the pool definition lives in
`scripts/Initialize-SandboxPool.ps1`.

```powershell
./scripts/Initialize-SandboxPool.ps1       # provision, migrate, seed, and start all three
Get-Content .sandbox/registry.json
./scripts/Claim-Sandbox.ps1 -TaskId CPM-xx.yy
./scripts/Release-Sandbox.ps1 -SandboxId sandbox-1 -TaskId CPM-xx.yy
```

If claim reports no available slot, the agent waits and retries. It must not reuse or stop another
agent's slot. Before UI work or recording, run `Test-LocalStack.ps1` against the claimed ports.

Findings from the M10 cockpit recording/demo are encoded in this setup: separate ports and per-
session Next.js output avoid cross-session collisions; separate volumes prevent cross-session data;
readiness must pass
before browser work; Release builds avoid stale binaries; HTTPS plus trusted dev certificates are
required for secure cookies; and temporary recording resources must be torn down after capture.
