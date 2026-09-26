import json
import tempfile
import unittest
from pathlib import Path

import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from pilot_config import PilotConfig, load_config, render_onboarding


class PilotConfigTests(unittest.TestCase):
    def test_valid_config_renders_launch_gates(self):
        config = PilotConfig("Example PM", "regional operators", "inbound-to-demo", "response time", "demos booked", "2026-10-01", ["inbound", "email"], ["Averion Compass supports accountable maintenance workflows."], "Owner")
        self.assertEqual(config.validate(), [])
        self.assertIn("Launch gates", render_onboarding(config))

    def test_email_requires_inbound_and_unknown_channels_fail(self):
        config = PilotConfig("Example PM", "operators", "workflow", "baseline", "success", "2026-10-01", ["email"], ["Fact"], "Owner")
        self.assertTrue(any("email pilots must include inbound handling" in error for error in config.validate()))
        config.allowed_channels.append("sms")
        self.assertTrue(any("unsupported channels: sms" in error for error in config.validate()))

    def test_json_loader_validates(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "pilot.json"
            path.write_text(json.dumps({"account": "Example", "segment": "operators", "workflow": "demo", "baseline_metric": "time", "success_metric": "demos", "review_date": "2026-10-01", "allowed_channels": ["inbound"], "approved_facts": ["Fact"], "escalation_owner": "Owner"}), encoding="utf-8")
            self.assertEqual(load_config(path).account, "Example")


if __name__ == "__main__":
    unittest.main()
