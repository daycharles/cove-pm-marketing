import unittest
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from lead_activation import LeadReview, queue_counts, render_queue_markdown


class LeadActivationTests(unittest.TestCase):
    def test_qualified_record_requires_evidence_and_resolved_gaps(self):
        record = LeadReview("Example PM", "https://example.com", "qualified", 8.5)
        with self.assertRaises(ValueError):
            record.approve("Michael")

        record.evidence_urls.append("https://example.com/about")
        record.evidence_gaps.append("confirm portfolio size")
        with self.assertRaises(ValueError):
            record.approve("Michael")

        record.evidence_gaps.clear()
        record.approve("Michael", "Review operating model", on="2026-09-20")
        self.assertEqual(record.approval, "approved")

    def test_outcome_requires_approval(self):
        record = LeadReview("Example PM", "https://example.com", "review", 6.0)
        with self.assertRaises(ValueError):
            record.record_outcome("unknown")
        record.approve("Michael", on="2026-09-20")
        record.record_outcome("advance", "Strong operating signal", on="2026-09-20")
        self.assertEqual(record.outcome, "advance")

    def test_queue_summary_keeps_dispositions_visible(self):
        records = [
            LeadReview("A", "https://a.test", "qualified", 8),
            LeadReview("B", "https://b.test", "review", 5),
            LeadReview("C", "https://c.test", "nurture", 3),
        ]
        self.assertEqual(queue_counts(records)["qualified"], 1)
        view = render_queue_markdown(records)
        self.assertIn("# Lead Review Queue", view)
        self.assertIn("| B | 5.0 | review | pending |", view)


if __name__ == "__main__":
    unittest.main()
