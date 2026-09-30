const fs=require('node:fs');
const vm=require('node:vm');
const assert=require('node:assert/strict');
const path=require('node:path');
// Exercise the renderer as a pure text-to-HTML function, without a browser or network.
const element={addEventListener(){}};
const context={window:{},URL,location:{origin:'https://example.invalid',href:'https://example.invalid/'},document:{getElementById(){return element;},addEventListener(){}}};
vm.runInNewContext(fs.readFileSync(path.resolve(__dirname,'../dist/client/markdown-viewer.js'),'utf8'),context);
const render=context.window.MarketingMarkdown.render;
const html=render('# Run result\n\n## Findings\n\n- Clear owner\n- **Evidence needed**\n\n| Metric | Result |\n| --- | --- |\n| Leads | Unknown |','https://example.invalid/report.md');
assert.match(html,/<h1>Run result<\/h1>/);assert.match(html,/<strong>Evidence needed<\/strong>/);assert.match(html,/<table>/);
const unsafe=render('<script>alert(1)</script>\n\n[Bad](javascript:alert)\n\n[Good](https://example.invalid/evidence)','https://example.invalid/report.md');
assert.ok(!unsafe.includes('<script>'));assert.ok(!unsafe.includes('href="javascript:'));assert.match(unsafe,/href="https:\/\/example.invalid\/evidence"/);
console.log('PASS: report headings, lists, emphasis, and tables render; raw HTML is escaped and dangerous URL protocols are rejected.');
