import tempfile
import unittest
from pathlib import Path

import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import reporting


class ReportingTests(unittest.TestCase):
    def test_empty_pilot_report_is_safe_and_actionable(self):
        with tempfile.TemporaryDirectory() as tmp:
            report = reporting.build_report(Path(tmp) / "runs.sqlite3")
            rendered = reporting.render_report(report)
            self.assertIn("Averion Compass Marketing Team Pilot Report", rendered)
            self.assertIn("What should be automated next", rendered)
            self.assertIsNone(report["funnel"]["approved_to_sent_pct"])


if __name__ == "__main__":
    unittest.main()
