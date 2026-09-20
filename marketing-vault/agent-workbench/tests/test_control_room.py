import json
import sqlite3
import tempfile
import unittest
from pathlib import Path

import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import control_room


class ControlRoomTests(unittest.TestCase):
    def test_scan_and_status_registers_inbox_tasks(self):
        with tempfile.TemporaryDirectory() as tmp:
            base = Path(tmp)
            inbox = base / "inbox"
            inbox.mkdir()
            (inbox / "task.md").write_text(
                "---\nobjective: Find a gap\npriority: 80\nworkflow: research\n---\nResearch it.",
                encoding="utf-8",
            )
            db = base / "runs.sqlite3"
            found = control_room.scan_inbox(inbox, db)
            snapshot = control_room.queue_snapshot(db)
            self.assertEqual(len(found), 1)
            self.assertEqual(snapshot["counts"], {"queued": 1})
            self.assertEqual(snapshot["tasks"][0]["priority"], 80)

    def test_run_next_creates_action_and_approval(self):
        with tempfile.TemporaryDirectory() as tmp:
            base = Path(tmp)
            inbox = base / "inbox"
            inbox.mkdir()
            task = inbox / "task.md"
            task.write_text("---\nobjective: Draft it\naudience: Operators\n---\nDraft it.", encoding="utf-8")
            db = base / "runs.sqlite3"
            control_room.scan_inbox(inbox, db)
            config = base / "config.json"
            config.write_text(json.dumps({"model": "test", "output_dir": str(base / "outputs"), "data_dir": str(base), "context_files": []}), encoding="utf-8")
            result = control_room.run_next(db, mock=True, config_path=config)
            self.assertEqual(result["status"], "completed")
            snapshot = control_room.queue_snapshot(db)
            self.assertEqual(snapshot["counts"], {"awaiting-approval": 1})
            self.assertEqual(len(snapshot["pending_approvals"]), 1)
            conn = sqlite3.connect(db)
            try:
                self.assertEqual(conn.execute("SELECT COUNT(*) FROM action_ledger").fetchone()[0], 1)
            finally:
                conn.close()


if __name__ == "__main__":
    unittest.main()
