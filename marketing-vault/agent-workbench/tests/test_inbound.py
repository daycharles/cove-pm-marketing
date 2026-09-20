import tempfile
import unittest
from pathlib import Path

import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import inbound


class InboundTests(unittest.TestCase):
    def test_demo_request_can_be_drafted_and_approved(self):
        with tempfile.TemporaryDirectory() as tmp:
            db = Path(tmp) / "runs.sqlite3"
            message_id = inbound.intake_message(db, "website", "prospect", "I would like to book a demo.")
            response_id = inbound.draft_response(db, message_id, "Thanks — we can help coordinate a demo.")
            with self.assertRaises(ValueError):
                inbound.record_response_sent(db, response_id, "Reviewer")
            inbound.approve_response(db, response_id, "Reviewer")
            inbound.record_response_sent(db, response_id, "Reviewer")
            self.assertEqual(inbound.inbound_snapshot(db)["messages"]["responded"], 1)

    def test_sensitive_intents_escalate(self):
        with tempfile.TemporaryDirectory() as tmp:
            db = Path(tmp) / "runs.sqlite3"
            message_id = inbound.intake_message(db, "email", "prospect", "What is your pricing and contract cost?")
            self.assertEqual(inbound.inbound_snapshot(db)["messages"]["escalate"], 1)
            with self.assertRaises(ValueError):
                inbound.draft_response(db, message_id, "Here is a price.")


if __name__ == "__main__":
    unittest.main()
