from pathlib import Path
from datetime import datetime, timezone
import sys, shutil, csv
home=Path(__file__).resolve().parents[2]
active=home.parent/'averion-software/products/cove-pm/marketing-vault/agent-workbench'
artifact=home/'marketing-vault/agent-workbench/outputs/20261001-compass-inspection-caption-recovery.md'
shutil.copy2(artifact,active/'outputs'/artifact.name)
sys.path.insert(0,str(active))
import run,control_room
now=datetime.now(timezone.utc).isoformat()
run.save_run(active/'data/runs.sqlite3',('manual-recovery-20261001-inspection-caption',now,now,str((active/'tasks/inbox/linkedin-compass-content-20261001.md').resolve()),'Codex recovery','submitted-for-review',str((active/'outputs'/artifact.name).resolve()),'[]',None))
control_room.sync_runs(active/'data/runs.sqlite3')
for vault in [home/'marketing-vault',active.parent]:
    usage=vault/'Assets/social-catalog/usage.csv'
    rows=list(csv.DictReader(usage.open(encoding='utf-8')))
    if not any(r['post_id']=='6abe60d340315cd9dbf7c2ed' for r in rows):
        with usage.open('a',encoding='utf-8',newline='') as file:
            csv.writer(file).writerow(['6abe60d340315cd9dbf7c2ed','AC-G002','reserved',now,'','','Averion Software LinkedIn',''])
    next_post=vault/'Assets/social-catalog/NEXT-POST.md'
    text=next_post.read_text(encoding='utf-8').replace('Status: pending next post build.','Status: fulfilled in draft build on October 1, 2026. Draft: 6abe60d340315cd9dbf7c2ed; asset: AC-G002; Marketing OS approval: #95. Exact asset uploaded and ready; image shown in the approval preview. Publishing remains pending approval.')
    next_post.write_text(text,encoding='utf-8')
print('Recorded hosted review #95, reserved AC-G002, and prevented the completed daily preparation from being retried as an empty draft.')
