import os
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from hubspot_api import HubSpotClient, HubSpotConfig


class HubSpotApiTests(unittest.TestCase):
    def test_status_does_not_require_token(self):
        with patch.dict(os.environ, {}, clear=True):
            config = HubSpotConfig.from_env()
            self.assertFalse(config.token())
            self.assertTrue(config.validate())

    def test_company_write_requires_approval(self):
        with tempfile.TemporaryDirectory() as tmp:
            config = HubSpotConfig(portal_id="247461247")
            client = HubSpotClient(config, Path(tmp) / "runs.sqlite3")
            with self.assertRaises(PermissionError):
                client.upsert_company({"name": "Example", "domain": "example.com"}, approved=False, idempotency_key="company-1")

    def test_health_check_requires_credentials(self):
        with tempfile.TemporaryDirectory() as tmp:
            client = HubSpotClient(HubSpotConfig(portal_id="247461247"), Path(tmp) / "runs.sqlite3")
            with self.assertRaises(RuntimeError):
                client.health_check()


if __name__ == "__main__":
    unittest.main()
