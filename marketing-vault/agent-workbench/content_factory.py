"""Local content-package factory for the Averion Compass marketing-team pilot."""

from __future__ import annotations

import argparse
import json
import sqlite3
from contextlib import closing
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

import control_room
from pilot_metrics import record_event


ROOT = Path(__file__).resolve().parent
DEFAULT_DB = ROOT / "data" / "runs.sqlite3"
CHANNELS = ("website", "email", "social", "sales")


def now_iso() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat()


def init_content_db(db_path: Path) -> None:
    control_room.init_control_room_db(db_path)
    with closing(sqlite3.connect(db_path)) as db:
        db.execute(
            """CREATE TABLE IF NOT EXISTS content_packages (
                package_id INTEGER PRIMARY KEY AUTOINCREMENT,
                source_title TEXT NOT NULL,
                audience TEXT NOT NULL,
                cta TEXT NOT NULL,
                facts_json TEXT NOT NULL,
                source_urls_json TEXT NOT NULL,
                variants_json TEXT NOT NULL,
                status TEXT NOT NULL DEFAULT 'awaiting-content-approval',
                reviewer TEXT NOT NULL DEFAULT '',
                created_at TEXT NOT NULL,
                approved_at TEXT NOT NULL DEFAULT ''
            )"""
        )
        db.execute(
            """CREATE TABLE IF NOT EXISTS content_publications (
                publication_id INTEGER PRIMARY KEY AUTOINCREMENT,
                package_id INTEGER NOT NULL,
                channel TEXT NOT NULL,
                published_url TEXT NOT NULL,
                published_at TEXT NOT NULL,
                recorded_by TEXT NOT NULL
            )"""
        )
        db.commit()


def _clean_facts(facts: list[str]) -> list[str]:
    cleaned = [fact.strip() for fact in facts if fact.strip()]
    if not cleaned:
        raise ValueError("at least one approved fact is required")
    return cleaned


def build_variants(source_title: str, audience: str, facts: list[str], cta: str) -> dict[str, str]:
    fact_text = " ".join(facts)
    first_fact = facts[0]
    return {
        "website": f"## {source_title}\n\nFor {audience}: {fact_text}\n\n**{cta}**",
        "email": f"Subject: {source_title}\n\nTeams responsible for {audience.lower()} may care about this: {fact_text}\n\n{cta}.",
        "social": f"{source_title}: {first_fact} {cta}.",
        "sales": f"Talking point — {source_title}: {fact_text} Next step: {cta}.",
    }


def create_package(
    db_path: Path,
    source_title: str,
    audience: str,
    facts: list[str],
    cta: str,
    source_urls: list[str],
) -> int:
    if not source_title.strip() or not audience.strip() or not cta.strip():
        raise ValueError("source_title, audience, and cta are required")
    if not source_urls or any(not url.strip() for url in source_urls):
        raise ValueError("at least one source URL is required")
    facts = _clean_facts(facts)
    init_content_db(db_path)
    variants = build_variants(source_title.strip(), audience.strip(), facts, cta.strip())
    with closing(sqlite3.connect(db_path)) as db:
        cursor = db.execute(
            """INSERT INTO content_packages
               (source_title, audience, cta, facts_json, source_urls_json, variants_json, created_at)
               VALUES (?, ?, ?, ?, ?, ?, ?)""",
            (source_title.strip(), audience.strip(), cta.strip(), json.dumps(facts), json.dumps(source_urls), json.dumps(variants), now_iso()),
        )
        db.commit()
        package_id = int(cursor.lastrowid)
    record_event(db_path, "content_package_created", details={"package_id": package_id, "channels": list(CHANNELS)})
    return package_id


def approve_package(db_path: Path, package_id: int, reviewer: str) -> dict[str, Any]:
    if not reviewer.strip():
        raise ValueError("reviewer is required")
    init_content_db(db_path)
    with closing(sqlite3.connect(db_path)) as db:
        row = db.execute("SELECT status FROM content_packages WHERE package_id=?", (package_id,)).fetchone()
        if not row:
            raise KeyError(f"Unknown content package: {package_id}")
        if row[0] != "awaiting-content-approval":
            raise ValueError("only packages awaiting approval can be approved")
        db.execute("UPDATE content_packages SET status='approved', reviewer=?, approved_at=? WHERE package_id=?", (reviewer.strip(), now_iso(), package_id))
        db.commit()
    record_event(db_path, "content_package_approved", details={"package_id": package_id, "reviewer": reviewer.strip()})
    return {"package_id": package_id, "status": "approved", "reviewer": reviewer.strip()}


def record_publication(db_path: Path, package_id: int, channel: str, published_url: str, recorded_by: str) -> int:
    if channel not in CHANNELS:
        raise ValueError(f"channel must be one of: {', '.join(CHANNELS)}")
    if not published_url.strip() or not recorded_by.strip():
        raise ValueError("published_url and recorded_by are required")
    init_content_db(db_path)
    with closing(sqlite3.connect(db_path)) as db:
        row = db.execute("SELECT status FROM content_packages WHERE package_id=?", (package_id,)).fetchone()
        if not row:
            raise KeyError(f"Unknown content package: {package_id}")
        if row[0] != "approved":
            raise ValueError("content must be approved before recording publication")
        cursor = db.execute(
            "INSERT INTO content_publications (package_id, channel, published_url, published_at, recorded_by) VALUES (?, ?, ?, ?, ?)",
            (package_id, channel, published_url.strip(), now_iso(), recorded_by.strip()),
        )
        db.commit()
        publication_id = int(cursor.lastrowid)
    record_event(db_path, "content_published", details={"package_id": package_id, "channel": channel, "publication_id": publication_id})
    return publication_id


def content_snapshot(db_path: Path) -> dict[str, Any]:
    init_content_db(db_path)
    with closing(sqlite3.connect(db_path)) as db:
        packages = db.execute("SELECT status, COUNT(*) FROM content_packages GROUP BY status").fetchall()
        publications = db.execute("SELECT channel, COUNT(*) FROM content_publications GROUP BY channel").fetchall()
    return {"packages": dict(packages), "publications": dict(publications)}


def render_content(snapshot: dict[str, Any]) -> str:
    lines = ["# Content Factory", "", "## Packages", "", "| Status | Count |", "|---|---:|"]
    lines.extend(f"| {status} | {count} |" for status, count in snapshot.get("packages", {}).items())
    if not snapshot.get("packages"):
        lines.append("| none | 0 |")
    lines.extend(["", "## Publications", "", "| Channel | Count |", "|---|---:|"])
    lines.extend(f"| {channel} | {count} |" for channel, count in snapshot.get("publications", {}).items())
    if not snapshot.get("publications"):
        lines.append("| none | 0 |")
    lines.extend(["", "Publishing remains manual until a connector is approved and configured.", ""])
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description="Operate the local Averion Compass content factory")
    parser.add_argument("command", choices=["status", "create", "approve", "publish"])
    parser.add_argument("--db", type=Path, default=DEFAULT_DB)
    parser.add_argument("--package-id", type=int)
    parser.add_argument("--title")
    parser.add_argument("--audience")
    parser.add_argument("--fact", action="append", default=[])
    parser.add_argument("--cta")
    parser.add_argument("--source-url", action="append", default=[])
    parser.add_argument("--reviewer")
    parser.add_argument("--channel")
    parser.add_argument("--published-url")
    parser.add_argument("--recorded-by")
    args = parser.parse_args()
    if args.command == "status":
        print(render_content(content_snapshot(args.db)))
    elif args.command == "create":
        if not all((args.title, args.audience, args.cta)) or not args.fact or not args.source_url:
            parser.error("--title, --audience, --fact, --cta, and --source-url are required")
        print(json.dumps({"package_id": create_package(args.db, args.title, args.audience, args.fact, args.cta, args.source_url)}, indent=2))
    elif args.command == "approve":
        if args.package_id is None or not args.reviewer:
            parser.error("--package-id and --reviewer are required")
        print(json.dumps(approve_package(args.db, args.package_id, args.reviewer), indent=2))
    else:
        if args.package_id is None or not args.channel or not args.published_url or not args.recorded_by:
            parser.error("--package-id, --channel, --published-url, and --recorded-by are required")
        print(json.dumps({"publication_id": record_publication(args.db, args.package_id, args.channel, args.published_url, args.recorded_by)}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
