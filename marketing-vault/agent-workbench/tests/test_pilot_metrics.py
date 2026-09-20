import tempfile
import unittest
from pathlib import Path

import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from pilot_metrics import metrics_snapshot, record_event, render_metrics


class PilotMetricsTests(unittest.TestCase):
    def test_records_and_aggregates_events(self):
        with tempfile.TemporaryDirectory() as tmp:
            db = Path(tmp) / "runs.sqlite3"
            record_event(db, "human_minutes", quantity=12)
            record_event(db, "human_minutes", quantity=8)
            record_event(db, "demo_booked")
            snapshot = metrics_snapshot(db)
            self.assertEqual(snapshot["human_minutes"], 20)
            self.assertEqual(snapshot["events"]["demo_booked"]["completed"]["records"], 1)
            self.assertIn("human_minutes", render_metrics(snapshot))

    def test_rejects_invalid_event(self):
        with tempfile.TemporaryDirectory() as tmp:
            db = Path(tmp) / "runs.sqlite3"
            with self.assertRaises(ValueError):
                record_event(db, "", quantity=1)
            with self.assertRaises(ValueError):
                record_event(db, "task", quantity=-1)


if __name__ == "__main__":
    unittest.main()
