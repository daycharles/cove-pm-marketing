import json
import sqlite3
import tempfile
import unittest
from contextlib import closing
from pathlib import Path

import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import pipeline
import lead_to_demo


class PipelineTests(unittest.TestCase):
    def _approved_lead(self, db: Path) -> str:
        pipeline.init_pipeline_db(db)
        lead_id = "example pm|https://example.com"
        with closing(sqlite3.connect(db)) as conn:
            conn.execute(
                """CREATE TABLE IF NOT EXISTS leads (
                    lead_id TEXT PRIMARY KEY, company TEXT, website TEXT, approval TEXT,
                    outcome TEXT
                )"""
            )
            conn.execute("INSERT INTO leads VALUES (?, ?, ?, 'approved', NULL)", (lead_id, "Example PM", "https://example.com"))
            conn.commit()
        return lead_id

    def test_lead_to_demo_requires_each_approval_step(self):
        with tempfile.TemporaryDirectory() as tmp:
            db = Path(tmp) / "runs.sqlite3"
            lead_id = self._approved_lead(db)
            draft_id = pipeline.create_outreach_draft(db, lead_id, "approved business contact", "Maintenance workflow", "Would a short conversation be useful?", ["https://example.com/about"])
            with self.assertRaises(ValueError):
                pipeline.record_send(db, draft_id, "Michael")
            pipeline.approve_outreach(db, draft_id, "Michael")
            pipeline.record_send(db, draft_id, "Michael")
            demo_id = pipeline.schedule_demo(db, lead_id, draft_id, "2026-09-25T14:00:00-04:00")
            result = pipeline.record_demo_outcome(db, demo_id, "held", "Discovery completed")
            self.assertEqual(result["status"], "held")
            snapshot = pipeline.pipeline_snapshot(db)
            self.assertEqual(snapshot["outreach"]["sent"], 1)
            self.assertEqual(snapshot["demos"]["held"], 1)

    def test_draft_requires_approved_lead_and_sources(self):
        with tempfile.TemporaryDirectory() as tmp:
            db = Path(tmp) / "runs.sqlite3"
            pipeline.init_pipeline_db(db)
            with closing(sqlite3.connect(db)) as conn:
                conn.execute("CREATE TABLE leads (lead_id TEXT PRIMARY KEY, approval TEXT, outcome TEXT)")
                conn.execute("INSERT INTO leads VALUES ('x', 'pending', NULL)")
                conn.commit()
            with self.assertRaises(ValueError):
                pipeline.create_outreach_draft(db, "x", "contact", "Subject", "Body", ["https://example.com"])

    def test_lead_to_demo_packet_queues_bridge_and_holds_calendar(self):
        with tempfile.TemporaryDirectory() as tmp:
            db = Path(tmp) / "runs.sqlite3"
            packet = lead_to_demo.prepare_lead_to_demo(
                db,
                lead_id="example pm|https://example.com",
                company="Example PM",
                website="https://www.example.com/",
                to="approved@example.com",
                subject="Demo",
                body="Would a short conversation be useful?",
                idempotency_key="packet-1",
            )
            self.assertEqual(packet.domain, "example.com")
            self.assertEqual(packet.email_status, "queued-for-approval")
            self.assertEqual(packet.calendar_status, "manual-review-required")

    def test_lead_to_demo_stops_without_recipient_path(self):
        with tempfile.TemporaryDirectory() as tmp:
            packet = lead_to_demo.prepare_lead_to_demo(
                Path(tmp) / "runs.sqlite3",
                lead_id="nycha|https://www.nyc.gov/site/nycha/index.page",
                company="NYCHA",
                website="https://www.nyc.gov/site/nycha/index.page",
                to="",
                subject="Discovery conversation",
                body="Would a short conversation be useful?",
                idempotency_key="nycha-demo-1",
            )
            self.assertEqual(packet.email_status, "recipient-path-needed")


if __name__ == "__main__":
    unittest.main()
