import tempfile
import unittest
from pathlib import Path

import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from integrations import ConfiguredConnector, IntegrationGateway, load_connectors, validate_connectors


class IntegrationTests(unittest.TestCase):
    def test_dry_run_requires_approval_and_records_safe_action(self):
        with tempfile.TemporaryDirectory() as tmp:
            db = Path(tmp) / "runs.sqlite3"
            gateway = IntegrationGateway(db, {"email": ConfiguredConnector("email", "dry-run")})
            with self.assertRaises(PermissionError):
                gateway.send_email("a@example.com", "Subject", "Body", "key-1", approved=False, suppression_checked=True)
            result = gateway.send_email("a@example.com", "Subject", "Body", "key-1", approved=True, suppression_checked=True)
            self.assertEqual(result.status, "dry-run")
            retry = gateway.send_email("a@example.com", "Subject", "Body", "key-1", approved=True, suppression_checked=True)
            self.assertEqual(retry.status, "already-recorded")

    def test_email_requires_suppression_check(self):
        with tempfile.TemporaryDirectory() as tmp:
            gateway = IntegrationGateway(Path(tmp) / "runs.sqlite3", {"email": ConfiguredConnector("email", "manual", "mailbox")})
            with self.assertRaises(PermissionError):
                gateway.send_email("a@example.com", "Subject", "Body", "key-1", approved=True, suppression_checked=False)

    def test_live_mode_is_not_enabled_by_accident(self):
        connector = ConfiguredConnector("social", "live", "social-account")
        self.assertTrue(any("not implemented" in error for error in validate_connectors({"social": connector})))

    def test_zoho_live_requires_credentials_but_is_supported(self):
        connector = ConfiguredConnector(
            "email", "live", "your@domain.com", provider="zoho-mail",
            host="smtppro.zoho.com", port=465,
            username_env="COVE_ZOHO_MAIL_USERNAME", password_env="COVE_ZOHO_MAIL_APP_PASSWORD",
            from_address="your@domain.com",
        )
        errors = validate_connectors({"email": connector})
        self.assertFalse(any("not implemented" in error for error in errors))
        self.assertTrue(any("credential environment variables" in error for error in errors))

    def test_zoho_bridge_queues_after_approval(self):
        connector = ConfiguredConnector(
            "email", "bridge", "info@averionsoftware.com", provider="zoho-mail-bridge"
        )
        with tempfile.TemporaryDirectory() as tmp:
            gateway = IntegrationGateway(Path(tmp) / "runs.sqlite3", {"email": connector})
            result = gateway.send_email(
                "prospect@example.com", "Demo", "Approved draft", "bridge-1",
                approved=True, suppression_checked=True,
            )
        self.assertEqual(result.status, "queued-for-approval")

    def test_example_config_loads(self):
        config = load_connectors(Path(__file__).resolve().parents[1] / "integrations.example.json")
        self.assertEqual(set(config), {"email", "social", "crm", "calendar"})


if __name__ == "__main__":
    unittest.main()
