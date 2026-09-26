"""Local inbound coordinator for the Averion Compass marketing-team pilot."""

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
INTENTS = ("demo_request", "product_question", "pricing", "support", "complaint", "other")
ESCALATE = {"pricing", "support", "complaint", "other"}


def now_iso() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat()


def init_inbound_db(db_path: Path) -> None:
    control_room.init_control_room_db(db_path)
    with closing(sqlite3.connect(db_path)) as db:
        db.execute(
            """CREATE TABLE IF NOT EXISTS inbound_messages (
                message_id INTEGER PRIMARY KEY AUTOINCREMENT,
                source TEXT NOT NULL,
                sender_label TEXT NOT NULL,
                body TEXT NOT NULL,
                intent TEXT NOT NULL,
                status TEXT NOT NULL DEFAULT 'needs-review',
                created_at TEXT NOT NULL
            )"""
        )
        db.execute(
            """CREATE TABLE IF NOT EXISTS inbound_responses (
                response_id INTEGER PRIMARY KEY AUTOINCREMENT,
                message_id INTEGER NOT NULL,
                body TEXT NOT NULL,
                status TEXT NOT NULL DEFAULT 'awaiting-approval',
                reviewer TEXT NOT NULL DEFAULT '',
                created_at TEXT NOT NULL,
                decided_at TEXT NOT NULL DEFAULT ''
            )"""
        )
        db.commit()


def classify_intent(body: str) -> str:
    text = body.lower()
    if any(word in text for word in ("demo", "walkthrough", "see it", "book a time")):
        return "demo_request"
    if any(word in text for word in ("price", "pricing", "cost", "contract", "discount")):
        return "pricing"
    if any(word in text for word in ("broken", "not working", "support", "issue", "bug")):
        return "support"
    if any(word in text for word in ("complaint", "angry", "unacceptable", "refund")):
        return "complaint"
    if any(word in text for word in ("how does", "integration", "feature", "workflow")):
        return "product_question"
    return "other"


def intake_message(db_path: Path, source: str, sender_label: str, body: str) -> int:
    if not source.strip() or not sender_label.strip() or not body.strip():
        raise ValueError("source, sender_label, and body are required")
    init_inbound_db(db_path)
    intent = classify_intent(body)
    status = "escalate" if intent in ESCALATE else "needs-review"
    with closing(sqlite3.connect(db_path)) as db:
        cursor = db.execute(
            "INSERT INTO inbound_messages (source, sender_label, body, intent, status, created_at) VALUES (?, ?, ?, ?, ?, ?)",
            (source.strip(), sender_label.strip(), body.strip(), intent, status, now_iso()),
        )
        db.commit()
        message_id = int(cursor.lastrowid)
    record_event(db_path, "inbound_received", details={"message_id": message_id, "intent": intent})
    if status == "escalate":
        record_event(db_path, "inbound_escalated", details={"message_id": message_id, "intent": intent})
    return message_id


def draft_response(db_path: Path, message_id: int, body: str) -> int:
    if not body.strip():
        raise ValueError("response body is required")
    init_inbound_db(db_path)
    with closing(sqlite3.connect(db_path)) as db:
        row = db.execute("SELECT intent, status FROM inbound_messages WHERE message_id=?", (message_id,)).fetchone()
        if not row:
            raise KeyError(f"Unknown inbound message: {message_id}")
        if row[0] in ESCALATE:
            raise ValueError("escalated inbound messages require human handling before drafting")
        cursor = db.execute(
            "INSERT INTO inbound_responses (message_id, body, created_at) VALUES (?, ?, ?)",
            (message_id, body.strip(), now_iso()),
        )
        db.execute("UPDATE inbound_messages SET status='response-drafted' WHERE message_id=?", (message_id,))
        db.commit()
        response_id = int(cursor.lastrowid)
    record_event(db_path, "inbound_response_drafted", details={"message_id": message_id, "response_id": response_id})
    return response_id


def approve_response(db_path: Path, response_id: int, reviewer: str) -> dict[str, Any]:
    if not reviewer.strip():
        raise ValueError("reviewer is required")
    init_inbound_db(db_path)
    with closing(sqlite3.connect(db_path)) as db:
        row = db.execute("SELECT status, message_id FROM inbound_responses WHERE response_id=?", (response_id,)).fetchone()
        if not row:
            raise KeyError(f"Unknown response: {response_id}")
        if row[0] != "awaiting-approval":
            raise ValueError("only responses awaiting approval can be approved")
        db.execute("UPDATE inbound_responses SET status='approved', reviewer=?, decided_at=? WHERE response_id=?", (reviewer.strip(), now_iso(), response_id))
        db.execute("UPDATE inbound_messages SET status='approved-response' WHERE message_id=?", (row[1],))
        db.commit()
    record_event(db_path, "inbound_response_approved", details={"response_id": response_id, "reviewer": reviewer.strip()})
    return {"response_id": response_id, "status": "approved", "reviewer": reviewer.strip()}


def record_response_sent(db_path: Path, response_id: int, sender: str) -> dict[str, Any]:
    if not sender.strip():
        raise ValueError("sender is required")
    init_inbound_db(db_path)
    with closing(sqlite3.connect(db_path)) as db:
        row = db.execute("SELECT status, message_id FROM inbound_responses WHERE response_id=?", (response_id,)).fetchone()
        if not row:
            raise KeyError(f"Unknown response: {response_id}")
        if row[0] != "approved":
            raise ValueError("human approval is required before recording a response as sent")
        db.execute("UPDATE inbound_responses SET status='sent', decided_at=? WHERE response_id=?", (now_iso(), response_id))
        db.execute("UPDATE inbound_messages SET status='responded' WHERE message_id=?", (row[1],))
        db.commit()
    record_event(db_path, "inbound_response_sent", details={"response_id": response_id, "sender": sender.strip()})
    return {"response_id": response_id, "status": "sent"}


def inbound_snapshot(db_path: Path) -> dict[str, Any]:
    init_inbound_db(db_path)
    with closing(sqlite3.connect(db_path)) as db:
        messages = db.execute("SELECT status, COUNT(*) FROM inbound_messages GROUP BY status").fetchall()
        intents = db.execute("SELECT intent, COUNT(*) FROM inbound_messages GROUP BY intent").fetchall()
        responses = db.execute("SELECT status, COUNT(*) FROM inbound_responses GROUP BY status").fetchall()
    return {"messages": dict(messages), "intents": dict(intents), "responses": dict(responses)}


def render_inbound(snapshot: dict[str, Any]) -> str:
    lines = ["# Inbound Coordinator", "", "| Message status | Count |", "|---|---:|"]
    lines.extend(f"| {status} | {count} |" for status, count in snapshot.get("messages", {}).items())
    if not snapshot.get("messages"):
        lines.append("| none | 0 |")
    lines.extend(["", "## Intents", "", "| Intent | Count |", "|---|---:|"])
    lines.extend(f"| {intent} | {count} |" for intent, count in snapshot.get("intents", {}).items())
    lines.extend(["", "## Responses", "", "| Response status | Count |", "|---|---:|"])
    lines.extend(f"| {status} | {count} |" for status, count in snapshot.get("responses", {}).items())
    lines.extend(["", "Pricing, support, complaints, and unknown intents escalate to a human before drafting.", ""])
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description="Operate the local Averion Compass inbound coordinator")
    parser.add_argument("command", choices=["status", "intake", "draft", "approve", "record-sent"])
    parser.add_argument("--db", type=Path, default=DEFAULT_DB)
    parser.add_argument("--message-id", type=int)
    parser.add_argument("--response-id", type=int)
    parser.add_argument("--source")
    parser.add_argument("--sender-label")
    parser.add_argument("--body")
    parser.add_argument("--reviewer")
    args = parser.parse_args()
    if args.command == "status":
        print(render_inbound(inbound_snapshot(args.db)))
    elif args.command == "intake":
        if not args.source or not args.sender_label or not args.body:
            parser.error("--source, --sender-label, and --body are required")
        print(json.dumps({"message_id": intake_message(args.db, args.source, args.sender_label, args.body)}, indent=2))
    elif args.command == "draft":
        if args.message_id is None or not args.body:
            parser.error("--message-id and --body are required")
        print(json.dumps({"response_id": draft_response(args.db, args.message_id, args.body)}, indent=2))
    elif args.command == "approve":
        if args.response_id is None or not args.reviewer:
            parser.error("--response-id and --reviewer are required")
        print(json.dumps(approve_response(args.db, args.response_id, args.reviewer), indent=2))
    else:
        if args.response_id is None or not args.sender_label:
            parser.error("--response-id and --sender-label are required")
        print(json.dumps(record_response_sent(args.db, args.response_id, args.sender_label), indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
