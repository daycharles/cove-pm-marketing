import sqlite3
import tempfile
import unittest
from pathlib import Path

import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import content_factory


class ContentFactoryTests(unittest.TestCase):
    def test_source_update_becomes_approved_multichannel_package(self):
        with tempfile.TemporaryDirectory() as tmp:
            db = Path(tmp) / "runs.sqlite3"
            package_id = content_factory.create_package(
                db, "Maintenance workflow update", "property operators",
                ["Teams can keep requests, ownership, scheduling, and history in one workflow."],
                "Book a demo", ["https://covepm.averion.com/"],
            )
            with self.assertRaises(ValueError):
                content_factory.record_publication(db, package_id, "social", "https://example.com/post", "Reviewer")
            content_factory.approve_package(db, package_id, "Reviewer")
            publication_id = content_factory.record_publication(db, package_id, "social", "https://example.com/post", "Reviewer")
            self.assertEqual(publication_id, 1)
            snapshot = content_factory.content_snapshot(db)
            self.assertEqual(snapshot["packages"]["approved"], 1)
            self.assertEqual(snapshot["publications"]["social"], 1)

    def test_package_requires_facts_and_sources(self):
        with tempfile.TemporaryDirectory() as tmp:
            db = Path(tmp) / "runs.sqlite3"
            with self.assertRaises(ValueError):
                content_factory.create_package(db, "Title", "Audience", [], "Book a demo", ["https://example.com"])
            with self.assertRaises(ValueError):
                content_factory.create_package(db, "Title", "Audience", ["Fact"], "Book a demo", [])


if __name__ == "__main__":
    unittest.main()
