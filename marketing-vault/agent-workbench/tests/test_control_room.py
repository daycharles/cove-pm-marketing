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

    def test_weekly_review_summarizes_lead_decisions(self):
        with tempfile.TemporaryDirectory() as tmp:
            db = Path(tmp) / "runs.sqlite3"
            control_room.init_control_room_db(db)
            conn = sqlite3.connect(db)
            try:
                conn.execute(
                    """CREATE TABLE leads (
                        lead_id TEXT PRIMARY KEY, company TEXT, fit_score INTEGER,
                        disposition TEXT, approval TEXT, next_action TEXT,
                        outcome TEXT, updated_at TEXT
                    )"""
                )
                conn.execute(
                    "INSERT INTO leads VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                    ("a|https://a.test", "Example PM", 82, "qualified", "pending", "Resolve volume evidence", None, "2026-09-20T12:00:00+00:00"),
                )
                conn.commit()
            finally:
                conn.close()
            review = control_room.weekly_review_snapshot(db)
            self.assertEqual(review["undecided_leads"], 1)
            rendered = control_room.render_weekly_review(review)
            self.assertIn("Example PM", rendered)
            self.assertIn("Resolve volume evidence", rendered)

    def test_run_next_records_pilot_metrics(self):
        with tempfile.TemporaryDirectory() as tmp:
            base = Path(tmp)
            inbox = base / "inbox"
            inbox.mkdir()
            task = inbox / "task.md"
            task.write_text("---\nobjective: Draft it\n---\nDraft it.", encoding="utf-8")
            db = base / "runs.sqlite3"
            control_room.scan_inbox(inbox, db)
            config = base / "config.json"
            config.write_text(json.dumps({"model": "test", "output_dir": str(base / "outputs"), "data_dir": str(base), "context_files": []}), encoding="utf-8")
            control_room.run_next(db, mock=True, config_path=config)
            metrics = control_room.metrics_snapshot(db)
            self.assertEqual(metrics["events"]["workbench_run"]["completed"]["quantity"], 1)
            self.assertEqual(metrics["events"]["approval_requested"]["completed"]["records"], 1)


if __name__ == "__main__":
    unittest.main()
