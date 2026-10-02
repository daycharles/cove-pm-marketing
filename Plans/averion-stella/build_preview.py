from pathlib import Path
import re, shutil

out = Path(__file__).resolve().parent
source = out.parents[2] / 'stella-ai' / 'index.html'
html = source.read_text(encoding='utf-8')
css = re.search(r'<style>(.*?)</style>', html, re.S).group(1)
css = css.replace('#10161c', '#17232e').replace('#0a0e12', '#101820').replace('#161e26', '#222d36')
css = css.replace('#4fb2e8', '#c3a36a').replace('#6b7a87', '#9caab5').replace('#232d37', '#36434e')
css = css.replace('rgba(79,178,232,0.16)', 'rgba(195,163,106,0.14)')
css += '''
/* Averion Stella brand layer. Trading outcomes keep their semantic colors. */
:root {
  --averion-navy: #101820;
  --averion-navy-raised: #17232e;
  --averion-silver: #aab1b8;
  --averion-gold: #c3a36a;
  --averion-gold-hover: #d4b980;
  --averion-white: #e8edf2;
  --surface-1: var(--averion-navy-raised);
  --surface-2: var(--averion-navy);
  --series-1: var(--averion-gold);
}
html { scroll-behavior: smooth; }
body { font-family: "Segoe UI", Arial, sans-serif; }
.site-header { border-top: 3px solid var(--averion-gold); border-bottom: 1px solid var(--border); background: var(--surface-2); }
.header-inner { max-width: 1200px; margin: auto; padding: 18px 28px; display: flex; align-items: center; justify-content: space-between; gap: 24px; }
.brand-link { display: flex; align-items: center; gap: 12px; text-decoration: none; }
.brand-link img { width: 48px; height: 48px; object-fit: contain; }
.brand-name { font-size: 18px; font-weight: 600; letter-spacing: .02em; }
.brand-name span { color: var(--averion-gold); }
.header-nav { display: flex; gap: 28px; align-items: center; font-size: 13px; }
.header-nav a { text-decoration: none; color: var(--text-secondary); }
.header-nav a:hover { color: var(--averion-gold); }
.hero { padding: 74px 24px 60px; }
.hero .logo-mark { display: block; }
.hero h1 { font-size: clamp(40px, 6vw, 72px); line-height: 1.12; letter-spacing: -.045em; font-weight: 600; }
.hero h1 span { color: var(--averion-gold); -webkit-text-fill-color: var(--averion-gold); }
.hero .badge { border-color: var(--border); color: var(--text-secondary); border-radius: 3px; }
.hero .tagline { color: var(--averion-gold); letter-spacing: .15em; }
.hero p.sub { max-width: 710px; }
.btn { display: inline-flex; justify-content: center; align-items: center; text-decoration: none; border-radius: 4px; font-family: inherit; font-weight: 600; font-size: 12px; cursor: pointer; transition: background .15s; }
.btn.primary, .mini-btn.primary { color: var(--averion-navy); }
.btn.primary:hover { background: var(--averion-gold-hover); }
.btn.ghost:hover { border-color: var(--averion-gold); }
a:focus-visible { outline: 2px solid var(--averion-gold); outline-offset: 5px; }
.shot { border-radius: 8px; }
.shot .chrome { padding: 12px; }
.stat-row { border-radius: 6px; overflow: hidden; }
.mock-stat, .mock-card, .mock-reasoning, .mock-panel, .guard-grid .g, .switch-box { border-radius: 4px; }
.screen-note { text-align: center; color: var(--text-secondary); font-size: 12px; margin: 22px 0 0; }
.safety { padding: 70px 24px; }
.safety .eyebrow { color: var(--averion-gold); }
.risk-note { font-size: 12px; color: var(--text-secondary); }
@media (max-width: 600px) {
  .header-inner { padding: 14px 18px; gap: 12px; }
  .brand-link img { width: 36px; height: 36px; }
  .brand-name { font-size: 16px; }
  .header-nav { gap: 12px; font-size: 12px; }
  .header-nav .company-link { display: none; }
  .hero { padding: 52px 20px 42px; }
  .hero p.sub { font-size: 16px; }
  .hero h1 { font-size: 42px; }
  .feature { gap: 28px; }
  .mock-portfolio { grid-template-columns: 1fr; }
  .mock-table { font-size: 9px; }
  .mock-table th, .mock-table td { padding: 6px 3px; font-size: 9px; }
  .shot .body { padding: 12px; }
  .plus { display: none; }
  .switch-box { max-width: none; }
}
@media (prefers-reduced-motion: reduce) { html { scroll-behavior: auto; } .btn { transition: none; } }
'''
(out / 'averion-stella.css').write_text(css.strip() + '\n', encoding='utf-8')
html = re.sub(r'<style>.*?</style>', '<link rel="stylesheet" href="averion-stella.css">', html, flags=re.S)
html = html.replace('StellaAI', 'Averion Stella').replace('StellaAI%20demo', 'Averion%20Stella%20demo')
html = html.replace('subject=Averion Stella%20demo', 'subject=Averion%20Stella%20demo')
html = html.replace('<body>', '''<body>
<header class="site-header"><div class="header-inner">
  <a class="brand-link" href="#top"><img src="assets/averion-mark.png" alt=""><span class="brand-name">Averion <span>Stella</span></span></a>
  <nav class="header-nav" aria-label="Main navigation"><a href="#features">Features</a><a href="#controls">Controls</a><a class="company-link" href="https://averionsoftware.com/">Averion Software ↗</a></nav>
</div></header>''')
html = html.replace('<section class="hero">', '<section class="hero" id="top">')
html = re.sub(r'<svg class="star".*?</svg>', '', html, count=1, flags=re.S)
html = html.replace('<h1>Stella</h1>', '<h1>Averion <span>Stella</span></h1>')
html = html.replace('The Future of Investing', 'Your markets. Your decisions.')
html = html.replace('<div class="btn ghost">See How It Trades</div>', '<a class="btn ghost" href="#features">See how it works ↓</a>')
html = html.replace('<main>', '<main id="features">', 1)
html = html.replace('<section class="safety">', '<section class="safety" id="controls">')
html = html.replace('let Stella ', 'let Averion Stella ').replace('order Stella ', 'order Averion Stella ').replace('Stella runs', 'Averion Stella runs')
html = html.replace('<span class="footer-brand">STELLA<span style="color:var(--series-1)">AI</span></span>', '<span class="footer-brand">AVERION <span style="color:var(--series-1)">STELLA</span></span>')
html = html.replace('stella.local/', 'Averion Stella / ')
html = html.replace('  <div class="stat-row">', '  <div class="stat-row">', 1)
html = html.replace('</section>\n\n<main id="features">', '<p class="screen-note">Product overview · Interface examples use illustrative data.</p>\n</section>\n\n<main id="features">', 1)
html = html.replace('<div class="cta-row">\n    <a class="btn primary"', '<div class="cta-row">\n    <a class="btn primary"')
html = html.replace('</section>\n\n<footer>', '<p class="risk-note">Crypto trading involves risk, including loss of invested funds. Averion Stella does not guarantee returns.</p>\n</section>\n\n<footer>')
(out / 'index.html').write_text(html, encoding='utf-8')
(out / 'assets').mkdir(exist_ok=True)
shutil.copy2(out.parents[2] / 'averion-software/assets/averion-mark.png', out / 'assets/averion-mark.png')
print(out)
