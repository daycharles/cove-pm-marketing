"""Retire the old local work queues without deleting research or output evidence."""
from pathlib import Path
import sqlite3, json, shutil
from datetime import datetime, timezone

home = Path(__file__).resolve().parents[2]
active = home.parent / 'averion-software/products/cove-pm/marketing-vault/agent-workbench'
roots = [active, home / 'marketing-vault/agent-workbench']
stamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%SZ')
results = []
for root in roots:
    root = root.resolve()
    archive = (root / 'tasks/archive' / ('compass-reset-' + stamp)).resolve()
    assert archive.is_relative_to(root)
    archive.mkdir(parents=True, exist_ok=True)
    for task in (root / 'tasks/inbox').glob('*.md'):
        source = task.resolve(); target = (archive / task.name).resolve()
        assert source.is_relative_to(root) and target.is_relative_to(root)
        shutil.move(str(source), str(target))
    db = root / 'data/runs.sqlite3'
    if db.exists():
        with sqlite3.connect(db) as con:
            with sqlite3.connect(archive / 'runs-before-reset.sqlite3') as backup:
                con.backup(backup)
            tables = {r[0] for r in con.execute("SELECT name FROM sqlite_master WHERE type='table'")}
            cutoff = con.execute('SELECT COALESCE(MAX(approval_id),0) FROM approval_queue').fetchone()[0] if 'approval_queue' in tables else 0
            if 'approval_queue' in tables:
                con.execute("UPDATE approval_queue SET status='retired',resolved_at=?,reviewer='user',decision='Retired: fresh Averion Compass queue requested'", (stamp,))
            if 'work_queue' in tables:
                con.execute("UPDATE work_queue SET status='retired',last_error='Retired by fresh Compass queue reset',updated_at=?", (stamp,))
            con.commit()
            results.append({'root': str(root), 'local_approval_max': cutoff, 'archive': str(archive)})

# Keep the new Compass brief out of the scanned inbox until the next daily generation.
for root in roots:
    templates = root / 'tasks/templates'; templates.mkdir(exist_ok=True)
    source = home / 'marketing-vault/agent-workbench/tasks/archive' / ('compass-reset-' + stamp) / 'linkedin-daily-content-and-engagement.md'
    text = source.read_text(encoding='utf-8').replace('StellaAI by Averion Software', 'Averion Stella')
    text += '\nFresh queue reset: use Averion Compass in every new title and public draft. Never regenerate retired CovePM artifacts. Historical drafts stay archived.\n'
    (templates / 'linkedin-daily-content-and-engagement.md').write_text(text, encoding='utf-8')
    social = root / 'social_workflow.py'
    code = social.read_text(encoding='utf-8').replace('TEMPLATE = INBOX / "linkedin-daily-content-and-engagement.md"', 'TEMPLATE = ROOT / "tasks" / "templates" / "linkedin-daily-content-and-engagement.md"')
    social.write_text(code, encoding='utf-8')
    control = root / 'control_room.py'
    code = control.read_text(encoding='utf-8').replace('WHERE task_path=?""",', "WHERE task_path=? AND status!='retired'\"\"\",")
    code = code.replace("SELECT 1 FROM approval_queue WHERE run_id=? AND status='pending'", "SELECT 1 FROM approval_queue WHERE run_id=?")
    control.write_text(code, encoding='utf-8')

script = active / 'run-weekly.ps1'
code = script.read_text(encoding='utf-8').replace("$_.note -like \"*$marker*\" -and $_.status -in @('pending', 'queued', 'approved')", '$_.note -like "*$marker*"')
script.write_text(code, encoding='utf-8')

# The scheduled checkout must receive the catalog requirement as well as the reporting copy.
source_assets = home / 'marketing-vault/Assets/social-catalog'
target_assets = active.parent / 'Assets/social-catalog'
shutil.copytree(source_assets, target_assets, dirs_exist_ok=True)
source_workflow = home / 'marketing-vault/Schedules/LinkedIn Publishing and Engagement Workflow.md'
shutil.copy2(source_workflow, active.parent / 'Schedules' / source_workflow.name)

(home / 'Plans/marketing-queue-reset/local-reset.json').write_text(json.dumps(results, indent=2), encoding='utf-8')
print(json.dumps(results, indent=2))
