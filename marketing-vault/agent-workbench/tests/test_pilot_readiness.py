import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from pilot_readiness import CRITERIA, PilotReadiness, render_scorecard


class PilotReadinessTests(unittest.TestCase):
    def test_incomplete_high_score_is_not_pilot_ready(self):
        record = PilotReadiness("Example", {criterion: 2 for criterion in CRITERIA[:-1]})
        self.assertEqual(record.total, 14)
        self.assertFalse(record.complete)
        self.assertEqual(record.decision, "discovery-qualified")

    def test_complete_score_reaches_pilot_design_ready(self):
        record = PilotReadiness("Example")
        for criterion in CRITERIA:
            record.score(criterion, 2, "Confirmed in discovery")
        self.assertTrue(record.complete)
        self.assertEqual(record.total, 16)
        self.assertEqual(record.decision, "pilot-design ready")
        self.assertIn("16/16", render_scorecard(record))

    def test_invalid_criterion_or_score_is_rejected(self):
        with self.assertRaises(ValueError):
            PilotReadiness("Example", {"not_a_criterion": 2})
        record = PilotReadiness("Example")
        with self.assertRaises(ValueError):
            record.score("workflow_scope", 3)


if __name__ == "__main__":
    unittest.main()
