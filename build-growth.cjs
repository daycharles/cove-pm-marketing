const fs = require('node:fs');
const path = require('node:path');
const root = __dirname;
const client = path.join(root, 'dist/client');
const old = fs.readFileSync(path.join(root,'index.html'),'utf8');
const catalog = path.join(client,'os-catalog.js');
if (!fs.existsSync(catalog)) {
  const start = old.indexOf('    const WORKBENCH =');
  const end = old.indexOf('    const TITLES=', start);
  if (start < 0 || end < 0) throw new Error('Original catalog not found');
  fs.writeFileSync(catalog, `(() => {\n${old.slice(start,end)}\nwindow.OS_CATALOG={docs:DOCS,workbench:WORKBENCH};\n})();\n`);
}
const html = `<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><meta name="theme-color" content="#101820"><title>Marketing OS · Averion Software</title><link rel="stylesheet" href="/growth-os.css"><link rel="stylesheet" href="/markdown-viewer.css"></head>
<body><a class="skip-link" href="#workspace">Skip to workspace</a><div class="app-shell">
<aside class="sidebar"><a class="brand" href="#today"><img src="/averion-mark.png" alt="Averion"><span>AVERION<small>MARKETING OS</small></span></a><p class="rail-label">Your growth workspace</p><nav id="navigation" aria-label="Workspace"></nav><div class="rail-bottom"><span class="eyebrow">Operating mode</span><strong>Research → prepare → review</strong><p>Agents prepare the work.<br>You approve publishing and outreach.</p><button class="rail-help" data-view="guide">How to use your OS ↗</button></div><div class="profile"><span class="avatar">A</span><div>Shared workspace<small>Averion Software</small></div></div></aside>
<div class="main-shell"><header class="topbar"><div class="breadcrumb">Marketing OS <span>/</span> <strong id="crumb">Today</strong></div><div class="top-actions"><button id="open-search" class="search-launch" aria-label="Search workspace">Search anything <kbd>Ctrl K</kbd></button><button class="button subtle" id="refresh">↻ Refresh</button><button class="button primary" data-new>＋ New work</button></div></header>
<main id="workspace" tabindex="-1"><div id="connection" aria-live="polite"></div><div id="view"></div></main><footer>Evidence before claims. Progress measured in customer outcomes. <span id="sync-time"></span></footer></div></div>
<dialog id="detail-dialog" class="detail-dialog" aria-labelledby="detail-title"><div class="dialog-top"><span class="eyebrow" id="detail-kind">Workspace</span><button class="icon-button" data-close aria-label="Close details">×</button></div><div id="detail-content"></div></dialog>
<dialog id="search-dialog" class="search-dialog" aria-label="Search workspace"><div class="search-header"><label class="sr-only" for="global-search">Search records and documents</label><input id="global-search" placeholder="Find companies, research, drafts, reports…" type="search"><button class="icon-button" data-close aria-label="Close search">×</button></div><div id="search-results"></div></dialog>
<div id="toast" role="status" aria-live="polite"></div>
<div id="markdown-viewer" class="md-reader" aria-hidden="true"><section class="md-reader-panel" role="dialog" aria-modal="true" aria-labelledby="markdown-viewer-title"><header class="md-reader-head"><div><p class="md-reader-eyebrow">Source document</p><h2 id="markdown-viewer-title" class="md-reader-title">Document</h2></div><button id="markdown-viewer-close" class="md-reader-close">Close</button></header><p id="markdown-viewer-status" class="md-reader-status" aria-live="polite"></p><article id="markdown-viewer-content" class="md-reader-content"></article><footer class="md-reader-foot"><a id="markdown-viewer-raw" class="md-reader-raw" data-raw-file download>Download original Markdown</a></footer></section></div>
<script src="/os-catalog.js" defer></script><script src="/growth-os.js" defer></script><script src="/markdown-viewer.js" defer></script></body></html>`;
for (const file of ['index.html','dist/index.html','dist/client/index.html','dist/marketing-os.html','dist/client/marketing-os.html']) fs.writeFileSync(path.join(root,file),html);
console.log('Built the shared Marketing OS entry points.');
