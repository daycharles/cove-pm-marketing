const fs = require('node:fs');
const path = require('node:path');

const root = __dirname;
const cssLink = '<link rel="stylesheet" href="/markdown-viewer.css">';
const viewer = `<div id="markdown-viewer" class="md-reader" aria-hidden="true">
  <section class="md-reader-panel" role="dialog" aria-modal="true" aria-labelledby="markdown-viewer-title">
    <header class="md-reader-head"><div><p class="md-reader-eyebrow">Workspace document</p><h2 class="md-reader-title" id="markdown-viewer-title">Loading…</h2></div><button class="md-reader-close" id="markdown-viewer-close" type="button">Close</button></header>
    <p class="md-reader-status" id="markdown-viewer-status" aria-live="polite"></p>
    <article class="md-reader-content" id="markdown-viewer-content"></article>
    <footer class="md-reader-foot"><a class="md-reader-raw" id="markdown-viewer-raw" data-raw-file download>Download original Markdown</a></footer>
  </section>
</div>`;
const scriptTag = '<script src="/markdown-viewer.js" defer></script>';

for (const relative of ['index.html', 'dist/index.html', 'dist/client/index.html']) {
  const file = path.join(root, relative);
  let html = fs.readFileSync(file, 'utf8');
  if (!html.includes(cssLink)) {
    html = html.replace('</head>', `  ${cssLink}\n</head>`);
  }
  if (!html.includes('id="markdown-viewer"')) {
    html = html.replace('</body>', `  ${viewer}\n  ${scriptTag}\n</body>`);
  }
  fs.writeFileSync(file, html, 'utf8');
}
console.log('Added the in-app Markdown viewer to the root and static entry pages.');
