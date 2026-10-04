import hashlib
import sys
import tempfile
import unittest
from datetime import date, datetime, timedelta, timezone
from pathlib import Path
from unittest.mock import patch

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

    def test_buffer_api_uses_custom_scheduled_mode_and_due_at(self):
        api = linkedin_buffer_runner.BufferApi("unit-test-token")
        queries = []
        api._graphql = lambda query: queries.append(query) or {"createPost": {"post": {"id": "buffer-1"}}}
        result = api.create_post(self.post, due_at="2026-10-05T13:00:00Z")
        self.assertEqual(result["id"], "buffer-1")
        self.assertIn("mode: customScheduled", queries[0])
        self.assertIn('dueAt: "2026-10-05T13:00:00Z"', queries[0])

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

    def test_custom_schedule_uses_explicit_due_at_and_verifies_readback(self):
        client = FakeBufferClient()
        run_now = datetime.now(linkedin_buffer_runner.NY)
        self.post["qa"]["history_checked_at"] = run_now.isoformat()
        run_day = run_now.date()
        due_time = (datetime.now(timezone.utc) + timedelta(days=2)).replace(
            hour=13, minute=0, second=0, microsecond=0
        )
        due_at = due_time.isoformat().replace("+00:00", "Z")
        result = linkedin_buffer_runner.run_cycle(
            client, {"posts": [self.post]}, "linkedin-channel", run_day, self.root,
            execute=True, target_due_at=due_at, media_check=lambda post: True,
        )
        self.assertEqual(result["status"], "scheduled_verified")
        self.assertEqual(result["dueAt"], due_at)
        self.assertEqual(client.create_calls, 1)
        self.assertEqual(client.last_due_at, due_at)

    def test_custom_schedule_skips_when_target_day_is_occupied(self):
        client = FakeBufferClient()
        run_now = datetime.now(linkedin_buffer_runner.NY)
        self.post["qa"]["history_checked_at"] = run_now.isoformat()
        target_local = run_now.replace(hour=9, minute=0, second=0, microsecond=0) + timedelta(days=1)
        due_at = target_local.astimezone(timezone.utc).isoformat().replace("+00:00", "Z")
        client.posts = [{
            "id": "already-scheduled", "text": "another post", "channelId": "linkedin-channel",
            "status": "scheduled", "dueAt": due_at,
        }]
        result = linkedin_buffer_runner.run_cycle(
            client, {"posts": [self.post]}, "linkedin-channel", run_now.date(), self.root,
            execute=False, target_due_at=due_at, media_check=lambda post: True,
        )
        self.assertEqual(result["status"], "skipped_no_eligible_post")
        self.assertEqual(client.create_calls, 0)

    def test_readback_accepts_equivalent_utc_due_at_format(self):
        readback = {
            "text": self.post["caption"],
            "channelId": "linkedin-channel",
            "status": "scheduled",
            "dueAt": "2026-10-02T13:00:00.000Z",
            "assets": [{"id": "asset-1", "mimeType": "image/png"}],
        }
        self.assertTrue(
            linkedin_buffer_runner._readback_matches(
                self.post, readback, "linkedin-channel", date(2026, 10, 2), "2026-10-02T13:00:00Z"
            )
        )

    def test_execute_fails_closed_when_readback_does_not_match(self):
        client = FakeBufferClient(mutate_readback=True)
        result = linkedin_buffer_runner.run_cycle(
            client, {"posts": [self.post]}, "linkedin-channel", date(2026, 10, 2), self.root,
            execute=True, media_check=lambda post: True,
        )
        self.assertEqual(result["status"], "readback_failed")
        self.assertEqual(client.create_calls, 1)

    def test_publish_now_requires_same_day_sent_readback_and_image(self):
        client = FakeBufferClient()
        result = linkedin_buffer_runner.run_cycle(
            client, {"posts": [self.post]}, "linkedin-channel", date(2026, 10, 2), self.root,
            execute=True, publish_now=True, media_check=lambda post: True,
        )
        self.assertEqual(result["status"], "published_verified")
        self.assertEqual(result["post_id"], "buffer-created-1")
        self.assertEqual(client.create_calls, 1)
        self.assertTrue(client.last_publish_now)

    def test_publish_now_polls_sending_status_without_repeating_mutation(self):
        client = FakeBufferClient(delay_publish=True)
        with patch.object(linkedin_buffer_runner.time, "sleep") as sleep:
            result = linkedin_buffer_runner.run_cycle(
                client, {"posts": [self.post]}, "linkedin-channel", date(2026, 10, 2), self.root,
                execute=True, publish_now=True, media_check=lambda post: True,
            )
        self.assertEqual(result["status"], "published_verified")
        self.assertEqual(client.create_calls, 1)
        self.assertEqual(sleep.call_count, 1)

    def test_publish_now_does_not_retry_or_claim_success_without_readback(self):
        client = FakeBufferClient(mutate_readback=True)
        result = linkedin_buffer_runner.run_cycle(
            client, {"posts": [self.post]}, "linkedin-channel", date(2026, 10, 2), self.root,
            execute=True, publish_now=True, media_check=lambda post: True,
        )
        self.assertEqual(result["status"], "publish_readback_failed")
        self.assertFalse(result["retry"])
        self.assertEqual(client.create_calls, 1)

    def test_publish_now_requires_execute_flag(self):
        client = FakeBufferClient()
        result = linkedin_buffer_runner.run_cycle(
            client, {"posts": [self.post]}, "linkedin-channel", date(2026, 10, 2), self.root,
            execute=False, publish_now=True, media_check=lambda post: True,
        )
        self.assertEqual(result["status"], "blocked_error")
        self.assertEqual(client.create_calls, 0)

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
    def __init__(self, mutate_readback=False, delay_publish=False):
        self.posts = []
        self.create_calls = 0
        self.mutate_readback = mutate_readback
        self.delay_publish = delay_publish
        self.post_readback_calls = 0
        self.last_publish_now = False
        self.last_due_at = None

    def verify_channel(self, channel_id):
        return channel_id == "linkedin-channel"

    def list_posts(self, channel_id):
        if self.create_calls and self.delay_publish:
            self.post_readback_calls += 1
            if self.post_readback_calls == 1:
                pending = dict(self.posts[0])
                pending["status"] = "sending"
                pending["sentAt"] = None
                return [pending]
        return list(self.posts)

    def create_post(self, post, *, publish_now=False, due_at=None):
        self.create_calls += 1
        self.last_publish_now = publish_now
        self.last_due_at = due_at
        caption = "tampered" if self.mutate_readback else post["caption"]
        created = {
            "id": "buffer-created-1",
            "text": caption,
            "channelId": post["channel_id"],
            "status": "sent" if publish_now else "scheduled",
            "dueAt": None if publish_now else due_at or "2026-10-02T13:00:00Z",
            "sentAt": "2026-10-02T13:00:00Z" if publish_now else None,
            "assets": [{"id": "asset-1", "mimeType": "image/png"}],
        }
        self.posts.append(created)
        return created


if __name__ == "__main__":
    unittest.main()
