const { chromium } = require('C:/Users/cd104535/AppData/Local/npm-cache/_npx/e41f203b7505f1fb/node_modules/playwright');
const fs = require('fs');
const path = require('path');
const http = require('http');
(async () => {
  const root = __dirname;
  const evidence = path.join(root, 'evidence');
  fs.mkdirSync(evidence, { recursive: true });
  const server = http.createServer((req, res) => {
    const file = path.join(root, decodeURIComponent(req.url.split('?')[0] === '/' ? '/index.html' : req.url.split('?')[0]));
    if (!file.startsWith(root + path.sep)) { res.writeHead(403).end(); return; }
    try { res.setHeader('Content-Type', file.endsWith('.css') ? 'text/css' : file.endsWith('.png') ? 'image/png' : 'text/html'); res.end(fs.readFileSync(file)); }
    catch { res.writeHead(404).end(); }
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  let browser;
  const results = [];
  try {
    browser = await chromium.launch({ executablePath: 'C:/Users/cd104535/AppData/Local/ms-playwright/chromium-1208/chrome-win64/chrome.exe', headless: true });
    for (const viewport of [{ width: 1440, height: 960 }, { width: 390, height: 844 }]) {
      const desktop = viewport.width > 600;
      const context = await browser.newContext({ viewport, recordVideo: desktop ? { dir: evidence, size: { width: 1440, height: 960 } } : undefined });
      const page = await context.newPage();
      const errors = [];
      page.on('pageerror', e => errors.push(e.message));
      await page.goto(`http://127.0.0.1:${server.address().port}`);
      await page.screenshot({ path: path.join(evidence, desktop ? 'desktop.png' : 'mobile.png'), fullPage: true });
      const checks = await page.evaluate(() => ({ title: document.title, heading: document.querySelector('h1').innerText, overflow: document.documentElement.scrollWidth > innerWidth, logoLoaded: document.querySelector('.brand-link img').naturalWidth > 0, oldBrand: /StellaAI|STELLAAI/.test(document.body.innerText), brokenAnchors: [...document.querySelectorAll('a[href^="#"]')].filter(a => !document.querySelector(a.getAttribute('href'))).length }));
      if (checks.overflow || !checks.logoLoaded || checks.oldBrand || checks.brokenAnchors || errors.length) throw new Error(JSON.stringify({ checks, errors }));
      if (desktop) {
        await page.waitForTimeout(1500);
        await page.getByRole('link', { name: 'See how it works' }).click();
        await page.waitForTimeout(1800);
        await page.evaluate(() => window.scrollTo({ top: 1600, behavior: 'smooth' }));
        await page.waitForTimeout(1800);
        await page.locator('#controls').scrollIntoViewIfNeeded();
        await page.waitForTimeout(1800);
        await page.locator('.closing').scrollIntoViewIfNeeded();
        await page.waitForTimeout(1500);
      }
      const video = page.video();
      await context.close();
      if (video) fs.renameSync(await video.path(), path.join(evidence, 'averion-stella-preview.webm'));
      results.push({ viewport, ...checks, errors });
    }
    fs.writeFileSync(path.join(evidence, 'checks.json'), JSON.stringify(results, null, 2));
    console.log(JSON.stringify(results));
  } finally { if (browser) await browser.close(); await new Promise(resolve => server.close(resolve)); }
})().catch(e => { console.error(e); process.exitCode = 1; });
