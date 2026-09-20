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
  await env.DB.prepare(`CREATE TABLE IF NOT EXISTS lead_records (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    lead_key TEXT NOT NULL UNIQUE,
    company TEXT NOT NULL,
    website TEXT NOT NULL DEFAULT '',
    segment TEXT NOT NULL DEFAULT '',
    fit_score INTEGER NOT NULL DEFAULT 0,
    disposition TEXT NOT NULL DEFAULT 'review',
    approval_status TEXT NOT NULL DEFAULT 'pending',
    evidence TEXT NOT NULL DEFAULT '',
    next_action TEXT NOT NULL DEFAULT '',
    updated_at TEXT NOT NULL
  )`).run();
  await env.DB.prepare('CREATE INDEX IF NOT EXISTS lead_records_status_idx ON lead_records(approval_status, fit_score)').run();
  await env.DB.prepare(`CREATE TABLE IF NOT EXISTS research_articles (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    title TEXT NOT NULL,
    dek TEXT NOT NULL DEFAULT '',
    finding TEXT NOT NULL,
    advantage TEXT NOT NULL,
    source_label TEXT NOT NULL,
    source_url TEXT NOT NULL DEFAULT '',
    confidence TEXT NOT NULL DEFAULT 'working signal',
    session_date TEXT NOT NULL,
    created_at TEXT NOT NULL
  )`).run();
  await env.DB.prepare('CREATE INDEX IF NOT EXISTS research_articles_session_idx ON research_articles(session_date, id)').run();
}

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    if (request.method === 'GET' && url.pathname === '/api/health') {
      return json({ ok: true, zohoConfigured: Boolean(env.ZOHO_CLIENT_ID && env.ZOHO_CLIENT_SECRET && env.ZOHO_REFRESH_TOKEN && env.ZOHO_ACCOUNT_ID), assetConfigured: Boolean(env.ASSETS) });
    }
    if (request.method === 'GET' && url.pathname === '/api/leads') {
      await ensureSchema(env);
      const rows = await env.DB.prepare('SELECT id, lead_key, company, website, segment, fit_score, disposition, approval_status, evidence, next_action, updated_at FROM lead_records ORDER BY fit_score DESC, updated_at DESC').all();
      return json({ leads: rows.results || [] });
    }
    if (request.method === 'GET' && url.pathname === '/api/research') {
      await ensureSchema(env);
      const rows = await env.DB.prepare('SELECT id, title, dek, finding, advantage, source_label, source_url, confidence, session_date, created_at FROM research_articles ORDER BY session_date DESC, id DESC LIMIT 50').all();
      return json({ articles: rows.results || [] });
    }
    if (request.method === 'POST' && url.pathname === '/api/research') {
      await ensureSchema(env);
      let body;
      try { body = await request.json(); } catch { return json({ error: 'Invalid JSON' }, 400); }
      const article = {
        title: clean(body.title, 240), dek: clean(body.dek, 500), finding: clean(body.finding, 3000), advantage: clean(body.advantage, 3000),
        source_label: clean(body.source_label, 240), source_url: clean(body.source_url, 1000), confidence: clean(body.confidence || 'working signal', 80), session_date: clean(body.session_date, 40), created_at: new Date().toISOString(),
      };
      if (!article.title || !article.finding || !article.advantage || !article.source_label || !article.session_date) return json({ error: 'title, finding, advantage, source_label, and session_date are required' }, 400);
      const result = await env.DB.prepare('INSERT INTO research_articles (title, dek, finding, advantage, source_label, source_url, confidence, session_date, created_at) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)').bind(article.title, article.dek, article.finding, article.advantage, article.source_label, article.source_url, article.confidence, article.session_date, article.created_at).run();
      return json({ ok: true, id: result.meta?.last_row_id ?? null }, 201);
    }
    if (request.method === 'POST' && url.pathname === '/api/leads') {
      await ensureSchema(env);
      let body;
      try { body = await request.json(); } catch { return json({ error: 'Invalid JSON' }, 400); }
      const lead = {
        lead_key: clean(body.lead_key, 240), company: clean(body.company, 240), website: clean(body.website, 500),
        segment: clean(body.segment, 240), fit_score: Math.max(0, Math.min(100, Number(body.fit_score) || 0)),
        disposition: ['qualified', 'review', 'nurture', 'disqualify'].includes(body.disposition) ? body.disposition : 'review',
        approval_status: ['pending', 'approved', 'rejected', 'nurture'].includes(body.approval_status) ? body.approval_status : (body.disposition === 'qualified' ? 'approved' : 'pending'),
        evidence: clean(body.evidence, 4000), next_action: clean(body.next_action, 1000), updated_at: new Date().toISOString(),
      };
      if (!lead.lead_key || !lead.company) return json({ error: 'lead_key and company are required' }, 400);
      await env.DB.prepare(`INSERT INTO lead_records (lead_key, company, website, segment, fit_score, disposition, approval_status, evidence, next_action, updated_at)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        ON CONFLICT(lead_key) DO UPDATE SET company=excluded.company, website=excluded.website, segment=excluded.segment, fit_score=excluded.fit_score, disposition=excluded.disposition, approval_status=excluded.approval_status, evidence=excluded.evidence, next_action=excluded.next_action, updated_at=excluded.updated_at`).bind(lead.lead_key, lead.company, lead.website, lead.segment, lead.fit_score, lead.disposition, lead.approval_status, lead.evidence, lead.next_action, lead.updated_at).run();
      return json({ ok: true, lead_key: lead.lead_key }, 201);
    }
    if (request.method === 'PATCH' && url.pathname.startsWith('/api/leads/')) {
      await ensureSchema(env);
      const id = Number(url.pathname.split('/').pop());
      if (!Number.isInteger(id) || id < 1) return json({ error: 'Invalid lead id' }, 400);
      let body;
      try { body = await request.json(); } catch { return json({ error: 'Invalid JSON' }, 400); }
      const status = clean(body.approval_status, 40);
      if (!['pending', 'approved', 'rejected', 'nurture'].includes(status)) return json({ error: 'Invalid lead approval status' }, 400);
      const result = await env.DB.prepare('UPDATE lead_records SET approval_status = ?, updated_at = ? WHERE id = ?').bind(status, new Date().toISOString(), id).run();
      if (!result.meta?.changes) return json({ error: 'Lead not found' }, 404);
      return json({ ok: true, id, approval_status: status });
    }
    if (request.method === 'POST' && url.pathname === '/api/approvals') {
      await ensureSchema(env);
      let body;
      try { body = await request.json(); } catch { return json({ error: 'Invalid JSON' }, 400); }
      const data = {
        task: clean(body.task, 240), action: clean(body.action, 40), decision: clean(body.decision, 40),
        note: clean(body.note), recipient: clean(body.recipient, 320), subject: clean(body.subject, 240), content: clean(body.content),
        status: body.decision === 'Pending review' ? 'pending' : 'queued', result: '',
      };
      if (!data.task || !['workbench', 'social-publish', 'social-engage', 'publish', 'outreach'].includes(data.action) || !['Approved', 'Changes requested', 'Pending review'].includes(data.decision)) return json({ error: 'Missing or invalid approval fields' }, 400);
      if (data.action === 'outreach' && (!data.recipient || !data.subject || !data.content)) return json({ error: 'Recipient, subject, and message are required for outreach' }, 400);
      const id = await record(env, data);
      if (data.decision === 'Pending review') return json({ ok: true, id, status: 'pending' }, 202);
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
    if (request.method === 'POST' && url.pathname.startsWith('/api/approvals/') && url.pathname.endsWith('/execute')) {
      await ensureSchema(env);
      const id = Number(url.pathname.split('/')[3]);
      if (!Number.isInteger(id)) return json({ error: 'Invalid approval id' }, 400);
      const row = await env.DB.prepare('SELECT id, task, action, recipient, subject, content, status FROM approval_actions WHERE id = ?').bind(id).first();
      if (!row) return json({ error: 'Approval not found' }, 404);
      if (row.status !== 'approved') return json({ error: 'Only approved items can be executed' }, 409);
      if (row.action !== 'outreach') return json({ error: 'Only outreach approvals can be sent through Zoho' }, 409);
      await env.DB.prepare('UPDATE approval_actions SET status = ?, result = ? WHERE id = ?').bind('running', 'Zoho send in progress.', id).run();
      try {
        const providerResult = await sendZoho(env, row);
        await env.DB.prepare('UPDATE approval_actions SET status = ?, result = ? WHERE id = ?').bind('sent', JSON.stringify(providerResult).slice(0, 4000), id).run();
        return json({ ok: true, id, status: 'sent', provider: providerResult });
      } catch (error) {
        await env.DB.prepare('UPDATE approval_actions SET status = ?, result = ? WHERE id = ?').bind('failed', clean(error.message, 1000), id).run();
        return json({ ok: false, id, status: 'failed', error: 'Zoho send failed; the approval remains recorded.' }, 502);
      }
    }
    if (request.method === 'PATCH' && url.pathname.startsWith('/api/approvals/')) {
      await ensureSchema(env);
      const id = Number(url.pathname.split('/').pop());
      if (!Number.isInteger(id) || id < 1) return json({ error: 'Invalid approval id' }, 400);
      let body;
      try { body = await request.json(); } catch { return json({ error: 'Invalid JSON' }, 400); }
      const status = clean(body.status, 40);
      const result = clean(body.result, 4000);
      if (!['approved', 'queued', 'running', 'completed', 'sent', 'failed', 'manual-execution-required', 'changes-requested'].includes(status)) return json({ error: 'Invalid status' }, 400);
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
