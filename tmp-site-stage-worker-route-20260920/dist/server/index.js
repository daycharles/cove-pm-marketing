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

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    if (request.method === 'GET' && url.pathname === '/api/health') {
      return json({ ok: true, zohoConfigured: Boolean(env.ZOHO_CLIENT_ID && env.ZOHO_CLIENT_SECRET && env.ZOHO_REFRESH_TOKEN && env.ZOHO_ACCOUNT_ID) });
    }
    if (request.method === 'POST' && url.pathname === '/api/approvals') {
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
      const rows = await env.DB.prepare('SELECT id, task, action, decision, note, recipient, subject, status, result, created_at FROM approval_actions ORDER BY id DESC LIMIT 100').all();
      return json({ approvals: rows.results || [] });
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
