const jsonHeaders = { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' };

function json(body, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: jsonHeaders });
}

function clean(value, max = 10000) {
  return String(value ?? '').trim().slice(0, max);
}

async function zohoAccessToken(env) {
  const response = await fetch('https://accounts.zoho.com/oauth/v2/token', {
    method: 'POST',
    headers: { 'content-type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
      grant_type: 'refresh_token',
      client_id: env.ZOHO_CLIENT_ID,
      client_secret: env.ZOHO_CLIENT_SECRET,
      refresh_token: env.ZOHO_REFRESH_TOKEN,
    }),
  });
  const payload = await response.json();
  if (!response.ok || !payload.access_token) throw new Error('Zoho token refresh failed');
  return payload.access_token;
}

async function sendZoho(env, message) {
  const accessToken = await zohoAccessToken(env);
  const response = await fetch(`https://mail.zoho.com/api/accounts/${env.ZOHO_ACCOUNT_ID}/messages`, {
    method: 'POST',
    headers: {
      authorization: `Zoho-oauthtoken ${accessToken}`,
      'content-type': 'application/json',
      accept: 'application/json',
    },
    body: JSON.stringify({
      fromAddress: env.ZOHO_FROM_ADDRESS,
      toAddress: message.recipient,
      subject: message.subject,
      content: message.content,
      mailFormat: 'plaintext',
    }),
  });
  const payload = await response.json();
  if (!response.ok) throw new Error(payload?.data?.message || payload?.message || 'Zoho send failed');
  return payload;
}

async function record(env, data) {
  const result = await env.DB.prepare(
    `INSERT INTO approval_actions (task, action, decision, note, recipient, subject, content, status, result, created_at)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`
  ).bind(data.task, data.action, data.decision, data.note, data.recipient, data.subject, data.content, data.status, data.result, new Date().toISOString()).run();
  return result.meta?.last_row_id ?? null;
}

async function ensureSchema(env) {
  await env.DB.prepare(`CREATE TABLE IF NOT EXISTS approval_actions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    task TEXT NOT NULL,
    action TEXT NOT NULL,
    decision TEXT NOT NULL,
    note TEXT NOT NULL DEFAULT '',
    recipient TEXT NOT NULL DEFAULT '',
    subject TEXT NOT NULL DEFAULT '',
    content TEXT NOT NULL DEFAULT '',
    status TEXT NOT NULL DEFAULT 'queued',
    result TEXT NOT NULL DEFAULT '',
    created_at TEXT NOT NULL
  )`).run();
  await env.DB.prepare('CREATE INDEX IF NOT EXISTS approval_actions_task_idx ON approval_actions(task, created_at)').run();
}

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    if (request.method === 'GET' && url.pathname === '/api/health') {
      return json({ ok: true, zohoConfigured: Boolean(env.ZOHO_CLIENT_ID && env.ZOHO_CLIENT_SECRET && env.ZOHO_REFRESH_TOKEN && env.ZOHO_ACCOUNT_ID), assetConfigured: Boolean(env.ASSETS) });
    }
    if (request.method === 'POST' && url.pathname === '/api/approvals') {
      await ensureSchema(env);
      let body;
      try { body = await request.json(); } catch { return json({ error: 'Invalid JSON' }, 400); }
      const data = {
        task: clean(body.task, 240), action: clean(body.action, 40), decision: clean(body.decision, 40),
        note: clean(body.note), recipient: clean(body.recipient, 320), subject: clean(body.subject, 240), content: clean(body.content),
        status: 'queued', result: '',
      };
      if (!data.task || !['workbench', 'publish', 'outreach'].includes(data.action) || !['Approved', 'Changes requested'].includes(data.decision)) return json({ error: 'Missing or invalid approval fields' }, 400);
      if (data.action === 'outreach' && (!data.recipient || !data.subject || !data.content)) return json({ error: 'Recipient, subject, and message are required for outreach' }, 400);
      const id = await record(env, data);
      if (data.decision !== 'Approved' || data.action !== 'outreach') return json({ ok: true, id, status: data.decision === 'Approved' ? 'queued' : 'changes-requested' }, 202);
      try {
        const providerResult = await sendZoho(env, data);
        await env.DB.prepare('UPDATE approval_actions SET status = ?, result = ? WHERE id = ?').bind('sent', JSON.stringify(providerResult).slice(0, 4000), id).run();
        return json({ ok: true, id, status: 'sent' });
      } catch (error) {
        await env.DB.prepare('UPDATE approval_actions SET status = ?, result = ? WHERE id = ?').bind('failed', clean(error.message, 1000), id).run();
        return json({ ok: false, id, status: 'failed', error: 'Zoho send failed; the approval remains recorded.' }, 502);
      }
    }
    if (request.method === 'GET' && url.pathname === '/api/approvals') {
      await ensureSchema(env);
      const rows = await env.DB.prepare('SELECT id, task, action, decision, note, recipient, subject, status, result, created_at FROM approval_actions ORDER BY id DESC LIMIT 100').all();
      return json({ approvals: rows.results || [] });
    }
    if (request.method === 'PATCH' && url.pathname.startsWith('/api/approvals/')) {
      await ensureSchema(env);
      const id = Number(url.pathname.split('/').pop());
      if (!Number.isInteger(id) || id < 1) return json({ error: 'Invalid approval id' }, 400);
      let body;
      try { body = await request.json(); } catch { return json({ error: 'Invalid JSON' }, 400); }
      const status = clean(body.status, 40);
      const result = clean(body.result, 4000);
      if (!['queued', 'running', 'completed', 'sent', 'failed', 'manual-execution-required', 'changes-requested'].includes(status)) return json({ error: 'Invalid status' }, 400);
      const updated = await env.DB.prepare('UPDATE approval_actions SET status = ?, result = ? WHERE id = ?').bind(status, result, id).run();
      if (!updated.meta?.changes) return json({ error: 'Approval not found' }, 404);
      return json({ ok: true, id, status });
    }
    if (env.ASSETS) {
      const assetUrl = new URL(request.url);
      if (assetUrl.pathname === '/marketing-os') assetUrl.pathname = '/marketing-os.html';
      if (assetUrl.pathname === '/artifacts') assetUrl.pathname = '/artifacts.html';
      return env.ASSETS.fetch(new Request(assetUrl, request));
    }
    return new Response('Not found', { status: 404 });
  },
};
