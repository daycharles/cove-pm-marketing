import hashlib
import sys
import tempfile
import unittest
from datetime import date
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import linkedin_buffer_runner


class LinkedInBufferRunnerTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.asset_path = self.root / "AC-G005-unit-review.png"
        self.asset_bytes = b"fake-png-for-unit-test"
        self.asset_path.write_bytes(self.asset_bytes)
        caption = "A distinct, evidence-backed LinkedIn post."
        self.post = {
            "post_id": "pilot-post-001",
            "status": "ready",
            "channel_id": "linkedin-channel",
            "caption": caption,
            "asset_id": "AC-G005",
            "asset_path": self.asset_path.name,
            "asset_url": "https://covepm.averionsoftware.com/assets/social/AC-G005-unit-review.png",
            "source_urls": ["https://averionsoftware.com/products/compass/"],
            "qa": {
                "status": "pass",
                "reviewer": "independent-reviewer",
                "reviewed_at": "2026-10-02T12:00:00-04:00",
                "caption_sha256": hashlib.sha256(caption.encode()).hexdigest(),
                "asset_sha256": hashlib.sha256(self.asset_bytes).hexdigest(),
                "uniqueness_check": "pass",
                "history_checked_at": "2026-10-02T12:00:00-04:00",
            },
        }

    def tearDown(self):
        self.temp.cleanup()

    def test_selects_exact_qa_passed_post_for_target_channel(self):
        selected = linkedin_buffer_runner.select_candidate(
            {"posts": [self.post]}, [], "linkedin-channel", date(2026, 10, 2), self.root
        )
        self.assertEqual(selected["post_id"], "pilot-post-001")

    def test_skips_when_exact_caption_hash_no_longer_matches_qa(self):
        self.post["caption"] += " changed"
        selected = linkedin_buffer_runner.select_candidate(
            {"posts": [self.post]}, [], "linkedin-channel", date(2026, 10, 2), self.root
        )
        self.assertIsNone(selected)

    def test_skips_when_history_qa_is_stale_for_the_run_day(self):
        self.post["qa"]["history_checked_at"] = "2026-10-01T12:00:00-04:00"
        selected = linkedin_buffer_runner.select_candidate(
            {"posts": [self.post]}, [], "linkedin-channel", date(2026, 10, 2), self.root
        )
        self.assertIsNone(selected)

    def test_skips_post_not_marked_ready(self):
        self.post["status"] = "blocked"
        selected = linkedin_buffer_runner.select_candidate(
            {"posts": [self.post]}, [], "linkedin-channel", date(2026, 10, 2), self.root
        )
        self.assertIsNone(selected)

    def test_skips_when_matching_caption_already_exists_in_buffer(self):
        existing = [{"text": self.post["caption"], "status": "sent"}]
        selected = linkedin_buffer_runner.select_candidate(
            {"posts": [self.post]}, existing, "linkedin-channel", date(2026, 10, 2), self.root
        )
        self.assertIsNone(selected)

    def test_skips_when_a_post_already_occupies_the_local_day(self):
        existing = [{"text": "different copy", "status": "scheduled", "dueAt": "2026-10-02T13:00:00Z"}]
        selected = linkedin_buffer_runner.select_candidate(
            {"posts": [self.post]}, existing, "linkedin-channel", date(2026, 10, 2), self.root
        )
        self.assertIsNone(selected)

    def test_skips_when_queue_already_at_free_plan_limit(self):
        existing = [
            {"text": f"queued post {index}", "status": "scheduled", "dueAt": f"2026-10-{3 + index:02d}T13:00:00Z"}
            for index in range(10)
        ]
        selected = linkedin_buffer_runner.select_candidate(
            {"posts": [self.post]}, existing, "linkedin-channel", date(2026, 10, 2), self.root
        )
        self.assertIsNone(selected)

    def test_skips_when_public_image_url_is_not_https(self):
        self.post["asset_url"] = "http://covepm.averionsoftware.com/image.png"
        selected = linkedin_buffer_runner.select_candidate(
            {"posts": [self.post]}, [], "linkedin-channel", date(2026, 10, 2), self.root
        )
        self.assertIsNone(selected)

    def test_skips_when_image_bytes_changed_after_qa(self):
        self.asset_path.write_bytes(b"changed")
        selected = linkedin_buffer_runner.select_candidate(
            {"posts": [self.post]}, [], "linkedin-channel", date(2026, 10, 2), self.root
        )
        self.assertIsNone(selected)


    def test_dry_run_never_creates_a_buffer_post(self):
        client = FakeBufferClient()
        result = linkedin_buffer_runner.run_cycle(
            client, {"posts": [self.post]}, "linkedin-channel", date(2026, 10, 2), self.root,
            execute=False, media_check=lambda post: True,
        )
        self.assertEqual(result["status"], "dry_run_ready")
        self.assertEqual(client.create_calls, 0)

    def test_execute_schedules_only_after_readback_matches_exact_payload(self):
        client = FakeBufferClient()
        result = linkedin_buffer_runner.run_cycle(
            client, {"posts": [self.post]}, "linkedin-channel", date(2026, 10, 2), self.root,
            execute=True, media_check=lambda post: True,
        )
        self.assertEqual(result["status"], "scheduled_verified")
        self.assertEqual(result["post_id"], "buffer-created-1")
        self.assertEqual(client.create_calls, 1)

    def test_execute_fails_closed_when_readback_does_not_match(self):
        client = FakeBufferClient(mutate_readback=True)
        result = linkedin_buffer_runner.run_cycle(
            client, {"posts": [self.post]}, "linkedin-channel", date(2026, 10, 2), self.root,
            execute=True, media_check=lambda post: True,
        )
        self.assertEqual(result["status"], "readback_failed")
        self.assertEqual(client.create_calls, 1)

    def test_skips_when_media_preflight_fails(self):
        client = FakeBufferClient()
        result = linkedin_buffer_runner.run_cycle(
            client, {"posts": [self.post]}, "linkedin-channel", date(2026, 10, 2), self.root,
            execute=True, media_check=lambda post: False,
        )
        self.assertEqual(result["status"], "skipped_media_check_failed")
        self.assertEqual(client.create_calls, 0)

    def test_skips_when_no_qa_passed_candidate_exists(self):
        client = FakeBufferClient()
        self.post["status"] = "blocked"
        result = linkedin_buffer_runner.run_cycle(
            client, {"posts": [self.post]}, "linkedin-channel", date(2026, 10, 2), self.root,
            execute=True, media_check=lambda post: True,
        )
        self.assertEqual(result["status"], "skipped_no_eligible_post")
        self.assertEqual(client.create_calls, 0)


class FakeBufferClient:
    def __init__(self, mutate_readback=False):
        self.posts = []
        self.create_calls = 0
        self.mutate_readback = mutate_readback

    def verify_channel(self, channel_id):
        return channel_id == "linkedin-channel"

    def list_posts(self, channel_id):
        return list(self.posts)

    def create_post(self, post):
        self.create_calls += 1
        caption = "tampered" if self.mutate_readback else post["caption"]
        created = {
            "id": "buffer-created-1",
            "text": caption,
            "channelId": post["channel_id"],
            "status": "scheduled",
            "dueAt": "2026-10-02T13:00:00Z",
            "assets": [{"id": "asset-1", "mimeType": "image/png"}],
        }
        self.posts.append(created)
        return created


if __name__ == "__main__":
    unittest.main()
