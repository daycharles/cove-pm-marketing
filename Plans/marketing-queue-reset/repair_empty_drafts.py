from pathlib import Path

for root in [Path('marketing-vault/agent-workbench'), Path('../averion-software/products/cove-pm/marketing-vault/agent-workbench')]:
    p = root / 'run.py'
    text = p.read_text(encoding='utf-8')
    text = text.replace('"format": "json",', '"format": {"type":"object", "required":["brief","draft","claims","questions","approval_required"], "properties": {"brief":{"type":"string"},"draft":{"type":"string","minLength":1},"claims":{"type":"array","minItems":1,"items":{"type":"object","required":["claim","source","status"],"properties":{"claim":{"type":"string"},"source":{"type":"string"},"status":{"type":"string","enum":["supported","needs_review"]}}}},"questions":{"type":"array","items":{"type":"string"}},"approval_required":{"type":"boolean"}}},')
    text = text.replace('            return json.loads(content)', '''            parsed = json.loads(content)
            if not isinstance(parsed, dict) or not str(parsed.get("draft", "")).strip() or not isinstance(parsed.get("claims"), list) or not parsed["claims"]:
                if attempt == 0:
                    continue
                raise RuntimeError("Drafting model returned an empty or incomplete draft after one retry; no approval item was created")
            return parsed''')
    text = text.replace('r"book a demo|start a pilot"', 'r"book a demo|start a pilot|talk with us|how it works"')
    text = text.replace('Draft is missing the required CTA: Book a demo or Start a pilot.', 'Draft is missing a supported CTA: Talk with us, How it works, Book a demo, or Start a pilot.')
    text = text.replace('StellaAI by Averion Software', 'Averion Stella').replace('Keep StellaAI secondary', 'Keep Averion Stella secondary')
    p.write_text(text, encoding='utf-8')
    for p in [root / 'tasks/templates/linkedin-daily-content-and-engagement.md', *list((root / 'tasks/inbox').glob('linkedin-compass-content-*.md'))]:
        text = p.read_text(encoding='utf-8')
        text += '\nWorker output contract: this run prepares ONE distinct LinkedIn post, not a combined batch or engagement actions. Return the required JSON object with a nonempty plain-text draft and claims review. Use one relevant ready catalog asset and record its selection. Follow-up posts are separate future runs.\n'
        p.write_text(text, encoding='utf-8')

p = Path('.os-upgrade/dist/client/growth-os.js')
text = p.read_text(encoding='utf-8')
pos = text.index('  function linkedTasks(type,id)')
text = text[:pos] + '''  function approvalMedia(note){const url=String(note||'').match(/^Asset image URL: (https:\/\/\\S+)$/m)?.[1];const alt=String(note||'').match(/^Asset alt text: (.+)$/m)?.[1]||'Attached post image';return url&&href(url)?`<h3>Exact attached image</h3><img class="publishing-media" src="${esc(href(url))}" alt="${esc(alt)}"><p class="row-sub">${esc(alt)}</p>`:'';}
''' + text[pos:]
text = text.replace('${row.result?`<h3>Recorded outcome', '${approvalMedia(row.note)}${row.result?`<h3>Recorded outcome')
p.write_text(text, encoding='utf-8')
