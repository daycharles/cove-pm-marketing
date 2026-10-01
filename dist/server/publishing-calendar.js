export async function publishingCalendar(request, env, fetcher = fetch) {
  const url = new URL(request.url);
  const from = url.searchParams.get('from');
  const to = url.searchParams.get('to');
  const start = new Date(from), end = new Date(to);
  if (!from || !to || !Number.isFinite(+start) || !Number.isFinite(+end) || end <= start || end - start > 50 * 86400000) {
    return { status: 400, body: { error: 'Choose a valid calendar range of up to 50 days.' } };
  }
  if (!env.PUBLORA_API_KEY) return { status: 503, body: { error: 'Publishing calendar is not connected to Publora.' } };
  try {
    const posts = new Map();
    const headers = { 'x-publora-key': env.PUBLORA_API_KEY, accept: 'application/json' };
    const connectionResponse = await fetcher('https://api.publora.com/api/v1/platform-connections', { headers, signal: AbortSignal.timeout(8000) });
    if (!connectionResponse.ok) throw new Error('Connection lookup failed');
    const connections = (await connectionResponse.json()).connections || [];
    const names = new Map(connections.map(c => [c.platformId, c.displayName || c.username || c.platformId]));
    let more = true, page = 1;
    while (more && page <= 10) {
      const endpoint = new URL('https://api.publora.com/api/v1/list-posts');
      Object.entries({ fromDate: start.toISOString(), toDate: end.toISOString(), sortBy: 'scheduledTime', sortOrder: 'asc', limit: '100', page: String(page) }).forEach(([k,v]) => endpoint.searchParams.set(k,v));
      const response = await fetcher(endpoint, { headers, signal: AbortSignal.timeout(8000) });
      if (!response.ok) throw new Error('Post lookup failed');
      const data = await response.json();
      if (!Array.isArray(data.posts)) throw new Error('Invalid calendar response');
      for (const post of data.posts) {
        if (!post.postGroupId || !Number.isFinite(+new Date(post.scheduledTime))) continue;
        posts.set(post.postGroupId, {
          id: post.postGroupId, content: String(post.content || '').slice(0,10000), status: String(post.status || 'unknown'),
          scheduledTime: post.scheduledTime,
          accounts: (post.platforms || []).map(p => ({ name: names.get(p.platformId) || p.platformId, platform: p.platform, status: p.status })),
          mediaUrls: (post.mediaUrls || []).filter(v => { try { return new URL(v).protocol === 'https:'; } catch { return false; } })
        });
      }
      more = data.pagination?.hasNextPage === true; page++;
    }
    if (more) throw new Error('Calendar range contains too many posts');
    return { status: 200, body: { posts: [...posts.values()], timeZone: 'America/New_York', refreshedAt: new Date().toISOString() } };
  } catch {
    return { status: 502, body: { error: 'Publora could not refresh the publishing calendar. Try Refresh again.' } };
  }
}
