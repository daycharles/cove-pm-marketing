"""Approval-gated lead-to-demo workflow for the local CovePM pilot.

This module prepares and records outreach; it never sends email or changes an external calendar.
"""

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


def now_iso() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat()


def init_pipeline_db(db_path: Path) -> None:
    control_room.init_control_room_db(db_path)
    with closing(sqlite3.connect(db_path)) as db:
        db.execute(
            """CREATE TABLE IF NOT EXISTS outreach_drafts (
                draft_id INTEGER PRIMARY KEY AUTOINCREMENT,
                lead_id TEXT NOT NULL,
                recipient_label TEXT NOT NULL,
                subject TEXT NOT NULL,
                body TEXT NOT NULL,
                source_urls_json TEXT NOT NULL,
                status TEXT NOT NULL DEFAULT 'awaiting-send-approval',
                reviewer TEXT NOT NULL DEFAULT '',
                created_at TEXT NOT NULL,
                decided_at TEXT NOT NULL DEFAULT ''
            )"""
        )
        db.execute(
            """CREATE TABLE IF NOT EXISTS demos (
                demo_id INTEGER PRIMARY KEY AUTOINCREMENT,
                lead_id TEXT NOT NULL,
                draft_id INTEGER NOT NULL,
                status TEXT NOT NULL DEFAULT 'requested',
                scheduled_at TEXT NOT NULL DEFAULT '',
                outcome_note TEXT NOT NULL DEFAULT '',
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            )"""
        )
        db.commit()


def _lead(db: sqlite3.Connection, lead_id: str) -> sqlite3.Row:
    db.row_factory = sqlite3.Row
    row = db.execute("SELECT * FROM leads WHERE lead_id=?", (lead_id,)).fetchone()
    if not row:
        raise KeyError(f"Unknown lead: {lead_id}")
    return row


def create_outreach_draft(
    db_path: Path,
    lead_id: str,
    recipient_label: str,
    subject: str,
    body: str,
    source_urls: list[str],
) -> int:
    if not recipient_label.strip() or not subject.strip() or not body.strip():
        raise ValueError("recipient_label, subject, and body are required")
    if not source_urls:
        raise ValueError("at least one evidence source URL is required")
    init_pipeline_db(db_path)
    with closing(sqlite3.connect(db_path)) as db:
        lead = _lead(db, lead_id)
        if lead["approval"] != "approved":
            raise ValueError("lead must be human-approved before an outreach draft is created")
        if lead["outcome"] == "disqualify":
            raise ValueError("disqualified leads cannot receive outreach drafts")
        cursor = db.execute(
            """INSERT INTO outreach_drafts
               (lead_id, recipient_label, subject, body, source_urls_json, created_at)
               VALUES (?, ?, ?, ?, ?, ?)""",
            (lead_id, recipient_label.strip(), subject.strip(), body.strip(), json.dumps(source_urls), now_iso()),
        )
        db.commit()
        draft_id = int(cursor.lastrowid)
    record_event(db_path, "outreach_draft", details={"lead_id": lead_id, "draft_id": draft_id})
    return draft_id


def approve_outreach(db_path: Path, draft_id: int, reviewer: str) -> dict[str, Any]:
    if not reviewer.strip():
        raise ValueError("reviewer is required")
    init_pipeline_db(db_path)
    with closing(sqlite3.connect(db_path)) as db:
        row = db.execute("SELECT * FROM outreach_drafts WHERE draft_id=?", (draft_id,)).fetchone()
        if not row:
            raise KeyError(f"Unknown outreach draft: {draft_id}")
        if row[6] != "awaiting-send-approval":
            raise ValueError("only drafts awaiting send approval can be approved")
        db.execute("UPDATE outreach_drafts SET status='approved', reviewer=?, decided_at=? WHERE draft_id=?", (reviewer.strip(), now_iso(), draft_id))
        db.commit()
    record_event(db_path, "outreach_approved", details={"draft_id": draft_id, "reviewer": reviewer.strip()})
    return {"draft_id": draft_id, "status": "approved", "reviewer": reviewer.strip()}


def record_send(db_path: Path, draft_id: int, sender: str) -> dict[str, Any]:
    if not sender.strip():
        raise ValueError("sender is required")
    init_pipeline_db(db_path)
    with closing(sqlite3.connect(db_path)) as db:
        row = db.execute("SELECT status FROM outreach_drafts WHERE draft_id=?", (draft_id,)).fetchone()
        if not row:
            raise KeyError(f"Unknown outreach draft: {draft_id}")
        if row[0] != "approved":
            raise ValueError("a human-approved draft is required before recording a send")
        db.execute("UPDATE outreach_drafts SET status='sent', decided_at=? WHERE draft_id=?", (now_iso(), draft_id))
        db.commit()
    record_event(db_path, "outreach_sent", details={"draft_id": draft_id, "sender": sender.strip()})
    return {"draft_id": draft_id, "status": "sent"}


def schedule_demo(db_path: Path, lead_id: str, draft_id: int, scheduled_at: str) -> int:
    if not scheduled_at.strip():
        raise ValueError("scheduled_at is required")
    init_pipeline_db(db_path)
    with closing(sqlite3.connect(db_path)) as db:
        draft = db.execute("SELECT status, lead_id FROM outreach_drafts WHERE draft_id=?", (draft_id,)).fetchone()
        if not draft:
            raise KeyError(f"Unknown outreach draft: {draft_id}")
        if draft[1] != lead_id:
            raise ValueError("draft and lead do not match")
        if draft[0] != "sent":
            raise ValueError("a sent outreach draft is required before scheduling a demo")
        cursor = db.execute(
            "INSERT INTO demos (lead_id, draft_id, status, scheduled_at, created_at, updated_at) VALUES (?, ?, 'scheduled', ?, ?, ?)",
            (lead_id, draft_id, scheduled_at.strip(), now_iso(), now_iso()),
        )
        db.commit()
        demo_id = int(cursor.lastrowid)
    record_event(db_path, "demo_scheduled", details={"lead_id": lead_id, "draft_id": draft_id, "demo_id": demo_id})
    return demo_id


def record_demo_outcome(db_path: Path, demo_id: int, status: str, note: str = "") -> dict[str, Any]:
    if status not in {"held", "no_show", "cancelled"}:
        raise ValueError("demo status must be held, no_show, or cancelled")
    init_pipeline_db(db_path)
    with closing(sqlite3.connect(db_path)) as db:
        row = db.execute("SELECT status FROM demos WHERE demo_id=?", (demo_id,)).fetchone()
        if not row:
            raise KeyError(f"Unknown demo: {demo_id}")
        if row[0] != "scheduled":
            raise ValueError("only scheduled demos can receive an outcome")
        db.execute("UPDATE demos SET status=?, outcome_note=?, updated_at=? WHERE demo_id=?", (status, note.strip(), now_iso(), demo_id))
        db.commit()
    record_event(db_path, "demo_outcome", details={"demo_id": demo_id, "status": status})
    return {"demo_id": demo_id, "status": status, "outcome_note": note.strip()}


def pipeline_snapshot(db_path: Path) -> dict[str, Any]:
    init_pipeline_db(db_path)
    with closing(sqlite3.connect(db_path)) as db:
        drafts = db.execute("SELECT status, COUNT(*) FROM outreach_drafts GROUP BY status").fetchall()
        demos = db.execute("SELECT status, COUNT(*) FROM demos GROUP BY status").fetchall()
    return {"outreach": dict(drafts), "demos": dict(demos)}


def render_pipeline(snapshot: dict[str, Any]) -> str:
    lines = ["# Lead-to-Demo Pipeline", "", "## Outreach", "", "| Status | Count |", "|---|---:|"]
    lines.extend(f"| {status} | {count} |" for status, count in snapshot.get("outreach", {}).items())
    if not snapshot.get("outreach"):
        lines.append("| none | 0 |")
    lines.extend(["", "## Demos", "", "| Status | Count |", "|---|---:|"])
    lines.extend(f"| {status} | {count} |" for status, count in snapshot.get("demos", {}).items())
    if not snapshot.get("demos"):
        lines.append("| none | 0 |")
    lines.extend(["", "Outbound sending and calendar changes remain external actions; this local workflow only records approved actions and outcomes.", ""])
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description="Operate the local approval-gated CovePM lead-to-demo pipeline")
    parser.add_argument("command", choices=["status", "create-draft", "approve-outreach", "record-send", "schedule-demo", "demo-outcome"])
    parser.add_argument("--db", type=Path, default=DEFAULT_DB)
    parser.add_argument("--draft-id", type=int)
    parser.add_argument("--demo-id", type=int)
    parser.add_argument("--lead-id")
    parser.add_argument("--reviewer")
    parser.add_argument("--sender")
    parser.add_argument("--scheduled-at")
    parser.add_argument("--status")
    parser.add_argument("--note", default="")
    parser.add_argument("--recipient-label")
    parser.add_argument("--subject")
    parser.add_argument("--body")
    parser.add_argument("--source-url", action="append", default=[])
    args = parser.parse_args()
    if args.command == "status":
        print(render_pipeline(pipeline_snapshot(args.db)))
    elif args.command == "create-draft":
        required = (args.lead_id, args.recipient_label, args.subject, args.body)
        if not all(required) or not args.source_url:
            parser.error("--lead-id, --recipient-label, --subject, --body, and at least one --source-url are required")
        print(json.dumps({"draft_id": create_outreach_draft(args.db, args.lead_id, args.recipient_label, args.subject, args.body, args.source_url)}, indent=2))
    elif args.command == "approve-outreach":
        if args.draft_id is None or not args.reviewer:
            parser.error("--draft-id and --reviewer are required")
        print(json.dumps(approve_outreach(args.db, args.draft_id, args.reviewer), indent=2))
    elif args.command == "record-send":
        if args.draft_id is None or not args.sender:
            parser.error("--draft-id and --sender are required")
        print(json.dumps(record_send(args.db, args.draft_id, args.sender), indent=2))
    elif args.command == "schedule-demo":
        if args.draft_id is None or not args.lead_id or not args.scheduled_at:
            parser.error("--draft-id, --lead-id, and --scheduled-at are required")
        print(json.dumps({"demo_id": schedule_demo(args.db, args.lead_id, args.draft_id, args.scheduled_at)}, indent=2))
    else:
        if args.demo_id is None or not args.status:
            parser.error("--demo-id and --status are required")
        print(json.dumps(record_demo_outcome(args.db, args.demo_id, args.status, args.note), indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
