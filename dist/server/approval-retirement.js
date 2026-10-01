export async function ensureRetirementSchema(env) {
  await env.DB.prepare(`CREATE TABLE IF NOT EXISTS approval_retirement (id INTEGER PRIMARY KEY CHECK(id=1), local_approval_max INTEGER NOT NULL DEFAULT 0, retired_at TEXT NOT NULL)`).run();
}

export async function resetApprovals(request, env) {
  let body; try { body = await request.json(); } catch { return { status:400, body:{error:'Invalid JSON'} }; }
  if (body.confirmation !== 'retire-all-approval-items' || !Number.isInteger(body.localApprovalMax) || body.localApprovalMax < 0) return {status:400,body:{error:'Explicit queue reset and a valid local cutoff are required.'}};
  await ensureRetirementSchema(env);
  const retiredAt = new Date().toISOString();
  const results = await env.DB.batch([
    env.DB.prepare(`INSERT INTO approval_retirement (id,local_approval_max,retired_at) VALUES (1,?,?) ON CONFLICT(id) DO UPDATE SET local_approval_max=MAX(local_approval_max,excluded.local_approval_max),retired_at=excluded.retired_at`).bind(body.localApprovalMax,retiredAt),
    env.DB.prepare(`UPDATE approval_actions SET status='archived',result=? WHERE status!='archived'`).bind(`Retired at user request on ${retiredAt}. Legacy queue reset; do not execute or recreate.`)
  ]);
  return {status:200,body:{ok:true,retired:results[1].meta?.changes || 0,retiredAt}};
}

export async function approvalImportGuard(env, data, preview) {
  await ensureRetirementSchema(env);
  const marker = data.note.match(/Local approval id:\s*(\d+);\s*run:\s*([^\r\n]+)/);
  if (marker) {
    const reset = await env.DB.prepare('SELECT local_approval_max FROM approval_retirement WHERE id=1').first();
    if (reset && Number(marker[1]) <= reset.local_approval_max) return 'This local approval was retired by the queue reset.';
    const existing = await env.DB.prepare('SELECT id FROM approval_actions WHERE task=? AND instr(note, ?) > 0 LIMIT 1').bind(data.task,marker[0]).first();
    if (existing) return 'This local approval has already been recorded; do not recreate it.';
  }
  if (/\bcove\s*pm\b/i.test(data.task+'\n'+preview)) return 'Retired CovePM branding is not allowed in new approval titles or public copy. Use Averion Compass.';
  return null;
}
