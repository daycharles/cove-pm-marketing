"""Fail-closed selection gates for the bounded LinkedIn Buffer pilot."""

from __future__ import annotations

import hashlib
import json
import os
import urllib.error
import urllib.request
from argparse import ArgumentParser
from datetime import date, datetime, timezone
from pathlib import Path
from typing import Any
from urllib.parse import urlparse
from zoneinfo import ZoneInfo


NY = ZoneInfo("America/New_York")
ALLOWED_MEDIA_HOST = "covepm.averionsoftware.com"
ALLOWED_MEDIA_PATH = "/assets/social/"
MAX_QUEUE_SIZE = 10


def _sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def _normalize_text(text: str) -> str:
    return " ".join(text.casefold().split())


def _local_date(value: str | None) -> date | None:
    if not value:
        return None
    try:
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    except (TypeError, ValueError):
        return None
    if parsed.tzinfo is None:
        return None
    return parsed.astimezone(NY).date()


def _valid_post(post: dict[str, Any], channel_id: str, run_day: date, media_root: Path) -> bool:
    if post.get("status") != "ready" or post.get("channel_id") != channel_id:
        return False
    post_id = post.get("post_id")
    caption = post.get("caption")
    asset_id = post.get("asset_id")
    if not all(isinstance(value, str) and value.strip() for value in (post_id, caption, asset_id)):
        return False
    if not post.get("source_urls") or any(
        not isinstance(url, str) or urlparse(url).scheme != "https" for url in post["source_urls"]
    ):
        return False

    qa = post.get("qa") or {}
    if (
        qa.get("status") != "pass"
        or qa.get("uniqueness_check") != "pass"
        or not qa.get("reviewer")
        or not qa.get("reviewed_at")
        or not qa.get("history_checked_at")
        or _local_date(qa.get("history_checked_at")) != run_day
        or qa.get("caption_sha256") != _sha256(caption.encode("utf-8"))
    ):
        return False

    asset_path = post.get("asset_path")
    asset_url = post.get("asset_url")
    if not isinstance(asset_path, str) or not isinstance(asset_url, str):
        return False
    media_root = media_root.resolve()
    local_path = (media_root / asset_path).resolve()
    if not local_path.is_relative_to(media_root) or not local_path.is_file():
        return False
    if qa.get("asset_sha256") != _sha256(local_path.read_bytes()):
        return False

    parsed_url = urlparse(asset_url)
    return (
        parsed_url.scheme == "https"
        and parsed_url.hostname == ALLOWED_MEDIA_HOST
        and parsed_url.path.startswith(ALLOWED_MEDIA_PATH)
        and not parsed_url.query
        and not parsed_url.fragment
    )


def select_candidate(
    manifest: dict[str, Any],
    existing_posts: list[dict[str, Any]],
    channel_id: str,
    run_day: date,
    media_root: Path,
) -> dict[str, Any] | None:
    """Return the first eligible exact artifact, otherwise fail closed with None."""
    if not isinstance(manifest, dict) or not isinstance(manifest.get("posts"), list):
        return None

    scheduled_count = sum(post.get("status") in {"scheduled", "sending"} for post in existing_posts if isinstance(post, dict))
    if scheduled_count >= MAX_QUEUE_SIZE:
        return None

    occupied_day = any(
        _local_date(post.get("dueAt") or post.get("sentAt")) == run_day
        for post in existing_posts
        if isinstance(post, dict)
    )
    if occupied_day:
        return None

    seen_texts = {
        _normalize_text(post["text"])
        for post in existing_posts
        if isinstance(post, dict) and isinstance(post.get("text"), str)
    }
    for post in manifest["posts"]:
        if not isinstance(post, dict) or not _valid_post(post, channel_id, run_day, media_root):
            continue
        if _normalize_text(post["caption"]) in seen_texts:
            continue
        return post
    return None


def run_cycle(
    client: Any,
    manifest: dict[str, Any],
    channel_id: str,
    run_day: date,
    media_root: Path,
    *,
    execute: bool = False,
    publish_now: bool = False,
    media_check: Any = None,
) -> dict[str, Any]:
    """Run one fail-closed cycle; mutation only occurs with execute=True."""
    if publish_now and not execute:
        return {"status": "blocked_error", "reason": "publish_now requires execute=True", "channel_id": channel_id}
    if not client.verify_channel(channel_id):
        return {"status": "blocked_channel_mismatch", "channel_id": channel_id}
    existing = client.list_posts(channel_id)
    post = select_candidate(manifest, existing, channel_id, run_day, media_root)
    if post is None:
        return {"status": "skipped_no_eligible_post", "channel_id": channel_id}
    try:
        media_ok = (media_check or verify_media)(post)
    except Exception as exc:
        return {"status": "skipped_media_check_failed", "reason": str(exc), "post_id": post["post_id"]}
    if not media_ok:
        return {"status": "skipped_media_check_failed", "post_id": post["post_id"]}
    if not execute:
        return {
            "status": "dry_run_ready",
            "post_id": post["post_id"],
            "caption": post["caption"],
            "asset_id": post["asset_id"],
            "asset_url": post["asset_url"],
            "source_urls": post["source_urls"],
            "qa": post["qa"],
        }

    created = client.create_post(post, publish_now=publish_now)
    post_id = created.get("id")
    if not post_id:
        return {"status": "readback_failed", "reason": "Buffer response omitted post ID", "post_id": None}
    readback = next((item for item in client.list_posts(channel_id) if item.get("id") == post_id), None)
    if publish_now:
        if _publish_readback_matches(post, readback, channel_id, run_day):
            return {
                "status": "published_verified",
                "post_id": post_id,
                "sentAt": readback["sentAt"],
                "caption": post["caption"],
                "asset_id": post["asset_id"],
                "asset_url": post["asset_url"],
                "source_urls": post["source_urls"],
                "qa": post["qa"],
                "readback": readback,
            }
        return {
            "status": "publish_readback_failed",
            "post_id": post_id,
            "caption": post["caption"],
            "asset_id": post["asset_id"],
            "asset_url": post["asset_url"],
            "source_urls": post["source_urls"],
            "qa": post["qa"],
            "readback": readback,
            "retry": False,
        }
    if not _readback_matches(post, readback, channel_id, run_day):
        return {
            "status": "readback_failed",
            "post_id": post_id,
            "caption": post["caption"],
            "asset_id": post["asset_id"],
            "asset_url": post["asset_url"],
            "source_urls": post["source_urls"],
            "qa": post["qa"],
            "readback": readback,
        }
    return {
        "status": "scheduled_verified",
        "post_id": post_id,
        "dueAt": readback["dueAt"],
        "caption": post["caption"],
        "asset_id": post["asset_id"],
        "asset_url": post["asset_url"],
        "source_urls": post["source_urls"],
        "qa": post["qa"],
        "readback": readback,
    }


def _readback_matches(post: dict[str, Any], readback: dict[str, Any] | None, channel_id: str, run_day: date) -> bool:
    if not readback:
        return False
    if (
        readback.get("text") != post["caption"]
        or readback.get("channelId") != channel_id
        or readback.get("status") != "scheduled"
        or _local_date(readback.get("dueAt")) != run_day
    ):
        return False
    assets = readback.get("assets") or []
    return len(assets) == 1 and assets[0].get("mimeType", "").startswith("image/")


def _publish_readback_matches(post: dict[str, Any], readback: dict[str, Any] | None, channel_id: str, run_day: date) -> bool:
    if not readback:
        return False
    if (
        readback.get("text") != post["caption"]
        or readback.get("channelId") != channel_id
        or readback.get("status") != "sent"
        or _local_date(readback.get("sentAt")) != run_day
    ):
        return False
    assets = readback.get("assets") or []
    return len(assets) == 1 and assets[0].get("mimeType", "").startswith("image/")


class _NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req: Any, fp: Any, code: int, msg: str, headers: Any, newurl: str) -> None:
        return None


def verify_media(post: dict[str, Any]) -> bool:
    """Verify the direct public image URL serves the exact QA'd local bytes."""
    media_path = Path(post["asset_path"])
    expected_hash = post["qa"]["asset_sha256"]
    root = Path(__file__).resolve().parent
    local_path = media_path if media_path.is_absolute() else root / media_path
    local_bytes = local_path.read_bytes()
    request = urllib.request.Request(post["asset_url"], headers={"User-Agent": "Averion-LinkedIn-Pilot/1.0"})
    opener = urllib.request.build_opener(_NoRedirect)
    try:
        with opener.open(request, timeout=20) as response:
            content_type = response.headers.get_content_type()
            remote_bytes = response.read(12_000_001)
    except (urllib.error.URLError, TimeoutError) as exc:
        raise RuntimeError("public media URL could not be fetched directly") from exc
    if len(remote_bytes) > 12_000_000 or content_type != "image/png":
        return False
    return _sha256(local_bytes) == expected_hash == _sha256(remote_bytes)


class BufferApi:
    """Minimal Buffer GraphQL client; never logs its bearer token or response headers."""

    endpoint = "https://api.buffer.com"

    def __init__(self, api_key: str) -> None:
        if not api_key:
            raise ValueError("BUFFER_API_KEY is required")
        self._api_key = api_key
        self._organization_id: str | None = None

    def _graphql(self, query: str) -> dict[str, Any]:
        body = json.dumps({"query": query}).encode("utf-8")
        request = urllib.request.Request(
            self.endpoint,
            data=body,
            headers={"Content-Type": "application/json", "Authorization": f"Bearer {self._api_key}"},
            method="POST",
        )
        try:
            with urllib.request.urlopen(request, timeout=30) as response:
                payload = json.loads(response.read().decode("utf-8"))
        except (urllib.error.URLError, TimeoutError, json.JSONDecodeError) as exc:
            raise RuntimeError("Buffer API request failed") from exc
        if payload.get("errors"):
            raise RuntimeError("Buffer API returned a GraphQL error")
        return payload.get("data") or {}

    def verify_channel(self, channel_id: str) -> bool:
        organizations = self._graphql("query { account { organizations { id name } } }")
        matches: list[str] = []
        for organization in organizations.get("account", {}).get("organizations", []):
            org_id = organization["id"]
            query = (
                "query { channels(input: { organizationId: " + json.dumps(org_id)
                + " }) { id name service } }"
            )
            channels = self._graphql(query).get("channels", [])
            for channel in channels:
                if channel.get("id") == channel_id and channel.get("service") == "linkedin":
                    matches.append(org_id)
        self._organization_id = matches[0] if len(matches) == 1 else None
        return self._organization_id is not None

    def list_posts(self, channel_id: str) -> list[dict[str, Any]]:
        if not self._organization_id:
            raise RuntimeError("target LinkedIn channel was not verified")
        all_posts: list[dict[str, Any]] = []
        after: str | None = None
        while True:
            after_literal = "null" if after is None else json.dumps(after)
            query = f"""query {{
              posts(first: 100, after: {after_literal}, input: {{
                organizationId: {json.dumps(self._organization_id)}
                filter: {{
                  channelIds: [{json.dumps(channel_id)}]
                  status: [draft, needs_approval, scheduled, sending, sent, error]
                }}
              }}) {{
                edges {{ node {{ id text status dueAt sentAt channelId assets {{ id mimeType }} }} }}
                pageInfo {{ hasNextPage endCursor }}
              }}
            }}"""
            data = self._graphql(query).get("posts") or {}
            all_posts.extend(edge.get("node", {}) for edge in data.get("edges", []))
            page_info = data.get("pageInfo") or {}
            if not page_info.get("hasNextPage"):
                return all_posts
            after = page_info.get("endCursor")
            if not after:
                raise RuntimeError("Buffer returned an incomplete pagination cursor")

    def create_post(self, post: dict[str, Any], *, publish_now: bool = False) -> dict[str, Any]:
        mode = "shareNow" if publish_now else "addToQueue"
        query = f"""mutation {{
          createPost(input: {{
            text: {json.dumps(post['caption'])}
            channelId: {json.dumps(post['channel_id'])}
            schedulingType: automatic
            mode: {mode}
            assets: [{{ image: {{ url: {json.dumps(post['asset_url'])} }} }}]
          }}) {{
            ... on PostActionSuccess {{
              post {{ id text status dueAt channelId assets {{ id mimeType }} }}
            }}
            ... on MutationError {{ message }}
          }}
        }}"""
        result = self._graphql(query).get("createPost") or {}
        if result.get("message"):
            raise RuntimeError("Buffer rejected the post mutation")
        return result.get("post") or {}


def main() -> int:
    parser = ArgumentParser(description="Fail-closed LinkedIn Buffer queue runner")
    parser.add_argument("--manifest", type=Path, default=Path(__file__).with_name("linkedin_posts.json"))
    parser.add_argument("--channel-id", default=os.environ.get("BUFFER_CHANNEL_ID", "6abffd55ea19ca0bde57f126"))
    parser.add_argument("--execute", action="store_true", help="Schedule the eligible post; default is read-only dry run")
    parser.add_argument("--publish-now", action="store_true", help="Publish the eligible post immediately (requires --execute)")
    parser.add_argument("--audit", type=Path, help="Write a JSON audit record to this path")
    args = parser.parse_args()
    started_at = datetime.now(timezone.utc).isoformat()
    run_day = datetime.now(NY).date()
    try:
        api = BufferApi(os.environ.get("BUFFER_API_KEY", ""))
        manifest = json.loads(args.manifest.read_text(encoding="utf-8"))
        outcome = run_cycle(api, manifest, args.channel_id, run_day, Path(__file__).resolve().parent, execute=args.execute, publish_now=args.publish_now)
    except Exception as exc:
        outcome = {"status": "blocked_error", "reason": str(exc)}
    outcome.setdefault("channel_id", args.channel_id)
    outcome.setdefault("run_day", run_day.isoformat())
    outcome.setdefault("started_at_utc", started_at)
    audit_path = args.audit or (Path(__file__).resolve().parent / "outputs" / "linkedin-buffer-audit.json")
    audit_path.parent.mkdir(parents=True, exist_ok=True)
    audit_path.write_text(json.dumps(outcome, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(json.dumps(outcome, ensure_ascii=False))
    return 0 if outcome.get("status") in {"dry_run_ready", "scheduled_verified", "published_verified", "skipped_no_eligible_post", "skipped_media_check_failed"} else 1


if __name__ == "__main__":
    raise SystemExit(main())
