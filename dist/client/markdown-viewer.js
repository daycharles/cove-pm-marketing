(() => {
  const modal = document.getElementById('markdown-viewer');
  if (!modal) return;
  const title = document.getElementById('markdown-viewer-title');
  const content = document.getElementById('markdown-viewer-content');
  const status = document.getElementById('markdown-viewer-status');
  const closeButton = document.getElementById('markdown-viewer-close');
  const rawLink = document.getElementById('markdown-viewer-raw');
  let lastFocus = null;

  const escapeHtml = (value) => String(value).replace(/[&<>"']/g, (char) => ({
    '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;',
  })[char]);

  function safeUrl(raw, base) {
    try {
      const url = new URL(raw.trim(), base);
      if (!['http:', 'https:', 'mailto:', 'tel:'].includes(url.protocol)) return null;
      return url;
    } catch { return null; }
  }

  function inlineMarkdown(text, base) {
    const tokens = [];
    const hold = (html) => {
      const key = `@@MDTOKEN${tokens.length}@@`;
      tokens.push(html);
      return key;
    };
    let value = escapeHtml(text);
    value = value.replace(/`([^`]+)`/g, (_match, code) => hold(`<code>${code}</code>`));
    value = value.replace(/!\[([^\]]*)\]\(([^)]+)\)/g, (_match, alt, href) => {
      const url = safeUrl(href, base);
      return url && ['http:', 'https:'].includes(url.protocol)
        ? hold(`<img src="${escapeHtml(url.href)}" alt="${alt}" loading="lazy">`)
        : alt;
    });
    value = value.replace(/\[([^\]]+)\]\(([^)]+)\)/g, (_match, label, href) => {
      const url = safeUrl(href, base);
      if (!url) return label;
      const external = url.origin !== location.origin;
      return hold(`<a href="${escapeHtml(url.href)}"${external ? ' target="_blank" rel="noopener noreferrer"' : ''}>${label}</a>`);
    });
    value = value.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      .replace(/__(.+?)__/g, '<strong>$1</strong>')
      .replace(/~~(.+?)~~/g, '<del>$1</del>')
      .replace(/(^|[^*])\*([^*]+)\*(?!\*)/g, '$1<em>$2</em>')
      .replace(/(^|[^_])_([^_]+)_(?!_)/g, '$1<em>$2</em>');
    return value.replace(/@@MDTOKEN(\d+)@@/g, (_match, index) => tokens[Number(index)] || '');
  }

  function renderMarkdown(markdown, base) {
    const lines = markdown.replace(/\r\n?/g, '\n').split('\n');
    const output = [];
    let paragraph = [];
    let list = '';
    let fenced = false;
    let code = [];
    const flushParagraph = () => {
      if (paragraph.length) output.push(`<p>${paragraph.map((line) => inlineMarkdown(line, base)).join('<br>')}</p>`);
      paragraph = [];
    };
    const closeList = () => {
      if (list) output.push(`</${list}>`);
      list = '';
    };
    const cells = (line) => line.trim().replace(/^\||\|$/g, '').split('|').map((cell) => cell.trim());

    for (let i = 0; i < lines.length; i += 1) {
      const line = lines[i];
      if (/^\s*```/.test(line)) {
        flushParagraph(); closeList();
        if (fenced) {
          output.push(`<pre><code>${escapeHtml(code.join('\n'))}</code></pre>`);
          code = [];
        }
        fenced = !fenced;
        continue;
      }
      if (fenced) { code.push(line); continue; }
      const next = lines[i + 1] || '';
      if (line.includes('|') && /^\s*\|?\s*:?-{3,}/.test(next)) {
        flushParagraph(); closeList();
        const headers = cells(line);
        i += 1;
        const rows = [];
        while (i + 1 < lines.length && lines[i + 1].includes('|')) rows.push(cells(lines[++i]));
        output.push(`<div class="md-table-wrap"><table><thead><tr>${headers.map((cell) => `<th>${inlineMarkdown(cell, base)}</th>`).join('')}</tr></thead><tbody>${rows.map((row) => `<tr>${headers.map((_header, index) => `<td>${inlineMarkdown(row[index] || '', base)}</td>`).join('')}</tr>`).join('')}</tbody></table></div>`);
        continue;
      }
      const heading = line.match(/^(#{1,6})\s+(.+)$/);
      if (heading) { flushParagraph(); closeList(); const level = heading[1].length; output.push(`<h${level}>${inlineMarkdown(heading[2], base)}</h${level}>`); continue; }
      if (/^\s*(---+|___+|\*\s*\*\s*\*)\s*$/.test(line)) { flushParagraph(); closeList(); output.push('<hr>'); continue; }
      const item = line.match(/^\s*(?:[-+*]\s+|\d+[.)]\s+)(.+)$/);
      if (item) {
        flushParagraph();
        const tag = /^\s*\d/.test(line) ? 'ol' : 'ul';
        if (list && list !== tag) closeList();
        if (!list) { list = tag; output.push(`<${list}>`); }
        output.push(`<li>${inlineMarkdown(item[1], base)}</li>`);
        continue;
      }
      if (/^>\s?/.test(line)) { flushParagraph(); closeList(); output.push(`<blockquote><p>${inlineMarkdown(line.replace(/^>\s?/, ''), base)}</p></blockquote>`); continue; }
      if (!line.trim()) { flushParagraph(); closeList(); continue; }
      closeList(); paragraph.push(line);
    }
    flushParagraph(); closeList();
    if (fenced) output.push(`<pre><code>${escapeHtml(code.join('\n'))}</code></pre>`);
    return output.join('\n');
  }

  function closeViewer() {
    modal.classList.remove('open');
    modal.setAttribute('aria-hidden', 'true');
    document.body.style.overflow = '';
    if (lastFocus && typeof lastFocus.focus === 'function') lastFocus.focus();
  }

  async function showDocument(url, trigger) {
    lastFocus = trigger;
    modal.classList.add('open');
    modal.setAttribute('aria-hidden', 'false');
    document.body.style.overflow = 'hidden';
    let filename = url.pathname.split('/').pop() || 'Workspace document';
    try { filename = decodeURIComponent(filename); } catch { /* Keep the encoded filename. */ }
    title.textContent = filename.replace(/\.md$/i, '').replace(/[-_]/g, ' ');
    status.textContent = 'Loading document…';
    content.innerHTML = '';
    rawLink.href = url.href;
    closeButton.focus();
    try {
      const response = await fetch(url.href, { headers: { Accept: 'text/markdown, text/plain;q=0.9' }, cache: 'no-store' });
      if (!response.ok) throw new Error(`Could not load the document (${response.status}).`);
      const markdown = await response.text();
      content.innerHTML = renderMarkdown(markdown, url.href);
      status.textContent = `${markdown.split(/\r?\n/).length.toLocaleString()} lines · Markdown document`;
    } catch (error) {
      status.textContent = error.message || 'The document could not be loaded.';
      content.innerHTML = '<p>This document is unavailable in the workspace right now.</p>';
    }
  }

  document.addEventListener('click', (event) => {
    const link = event.target.closest('a[href]');
    if (!link || link.hasAttribute('data-raw-file')) return;
    let url;
    try { url = new URL(link.href, location.href); } catch { return; }
    if (!url.pathname.toLowerCase().endsWith('.md')) return;
    event.preventDefault();
    showDocument(url, link);
  });
  closeButton.addEventListener('click', closeViewer);
  modal.addEventListener('click', (event) => { if (event.target === modal) closeViewer(); });
  document.addEventListener('keydown', (event) => { if (event.key === 'Escape' && modal.classList.contains('open')) closeViewer(); });
})();
