import json
import sqlite3
import tempfile
import unittest
from pathlib import Path

import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import lead_engine


class LeadEngineTests(unittest.TestCase):
    def test_missing_evidence_stays_in_review(self):
        result = lead_engine.qualify_candidate(
            {"company": "Example", "segment": "Multifamily"},
            {"model": "test"},
            mock=True,
        )
        self.assertEqual(result["disposition"], "review")
        self.assertEqual(result["fit_score"], 0)
        self.assertIn("source_urls", result["gaps"][0])

    def test_malformed_model_result_uses_conservative_fallback(self):
        candidate = {
            "company": "Example",
            "segment": "Affordable housing",
            "evidence": "Inspection and maintenance workflows are publicly described.",
            "source_urls": ["https://example.com"],
        }
        result = lead_engine.normalize_result(candidate, {"fit_score": 0, "disposition": "qualified|review"})
        self.assertIn(result["disposition"], {"qualified", "review"})
        self.assertGreater(result["fit_score"], 0)

    def test_mock_qualification_writes_report_and_lead(self):
        with tempfile.TemporaryDirectory() as tmp:
            base = Path(tmp)
            candidates = base / "candidates.json"
            candidates.write_text(json.dumps([{
                "company": "Example Property Group",
                "website": "https://example.com",
                "segment": "Multifamily",
                "portfolio_context": "Property operations team",
                "evidence": "Public materials mention maintenance work orders and resident communication.",
                "source_urls": ["https://example.com/operations"],
            }]), encoding="utf-8")
            config = base / "config.json"
            config.write_text(json.dumps({"model": "test"}), encoding="utf-8")
            db = base / "runs.sqlite3"
            output = base / "lead-report.md"
            result = lead_engine.run(candidates, config, db, output, mock=True)
            self.assertEqual(result["records"], 1)
            self.assertTrue(output.exists())
            conn = sqlite3.connect(db)
            try:
                row = conn.execute("SELECT company, disposition, fit_score FROM leads").fetchone()
            finally:
                conn.close()
            self.assertEqual(row[0], "Example Property Group")
            self.assertEqual(row[1], "qualified")
            self.assertGreaterEqual(row[2], 70)

    def test_approval_and_outcome_survive_requalification(self):
        with tempfile.TemporaryDirectory() as tmp:
            base = Path(tmp)
            candidates = base / "candidates.json"
            candidate = {
                "company": "Example Property Group",
                "website": "https://example.com",
                "segment": "Multifamily",
                "portfolio_context": "Property operations team",
                "evidence": "Public materials mention maintenance work orders and resident communication.",
                "source_urls": ["https://example.com/operations"],
            }
            candidates.write_text(json.dumps([candidate]), encoding="utf-8")
            config = base / "config.json"
            config.write_text(json.dumps({"model": "test"}), encoding="utf-8")
            db = base / "runs.sqlite3"
            output = base / "lead-report.md"

            lead_engine.run(candidates, config, db, output, mock=True)
            lead_id = "example property group|https://example.com"
            lead_engine.resolve_lead_gaps(db, lead_id, "Michael")
            approved = lead_engine.approve_lead(db, lead_id, "Michael", "Review operating model", "2026-09-20")
            self.assertEqual(approved.approval, "approved")
            outcome = lead_engine.record_lead_outcome(db, lead_id, "advance", "Good maintenance signal", "2026-09-20")
            self.assertEqual(outcome.outcome, "advance")

            lead_engine.run(candidates, config, db, output, mock=True)
            current = lead_engine.list_leads(db)[0]
            self.assertEqual(current.approval, "approved")
            self.assertEqual(current.outcome, "advance")
            self.assertIn("Lead Review Queue", lead_engine.render_queue(db))


if __name__ == "__main__":
    unittest.main()
