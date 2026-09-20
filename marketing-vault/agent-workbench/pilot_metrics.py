"""Small local event ledger for measuring the CovePM marketing-team pilot."""

from __future__ import annotations

import json
import sqlite3
from contextlib import closing
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


def now_iso() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat()


def init_metrics_db(db_path: Path) -> None:
    db_path.parent.mkdir(parents=True, exist_ok=True)
    with closing(sqlite3.connect(db_path)) as db:
        db.execute(
            """CREATE TABLE IF NOT EXISTS pilot_events (
                event_id INTEGER PRIMARY KEY AUTOINCREMENT,
                event_type TEXT NOT NULL,
                status TEXT NOT NULL,
                quantity REAL NOT NULL DEFAULT 1,
                details_json TEXT NOT NULL DEFAULT '{}',
                occurred_at TEXT NOT NULL
            )"""
        )
        db.commit()


def record_event(
    db_path: Path,
    event_type: str,
    *,
    status: str = "completed",
    quantity: float = 1,
    details: dict[str, Any] | None = None,
    occurred_at: str | None = None,
) -> int:
    if not event_type.strip():
        raise ValueError("event_type is required")
    if quantity < 0:
        raise ValueError("quantity must be non-negative")
    init_metrics_db(db_path)
    with closing(sqlite3.connect(db_path)) as db:
        cursor = insert_event(db, event_type, status=status, quantity=quantity, details=details, occurred_at=occurred_at)
        db.commit()
        return int(cursor.lastrowid)


def insert_event(
    db: sqlite3.Connection,
    event_type: str,
    *,
    status: str = "completed",
    quantity: float = 1,
    details: dict[str, Any] | None = None,
    occurred_at: str | None = None,
) -> sqlite3.Cursor:
    """Insert an event using an existing connection/transaction."""
    return db.execute(
        """INSERT INTO pilot_events
           (event_type, status, quantity, details_json, occurred_at)
           VALUES (?, ?, ?, ?, ?)""",
        (event_type.strip(), status.strip() or "completed", quantity, json.dumps(details or {}), occurred_at or now_iso()),
    )


def metrics_snapshot(db_path: Path) -> dict[str, Any]:
    init_metrics_db(db_path)
    with closing(sqlite3.connect(db_path)) as db:
        rows = db.execute(
            """SELECT event_type, status, SUM(quantity), COUNT(*)
               FROM pilot_events GROUP BY event_type, status
               ORDER BY event_type, status"""
        ).fetchall()
    events: dict[str, dict[str, float]] = {}
    for event_type, status, quantity, count in rows:
        events.setdefault(event_type, {})[status] = {
            "quantity": float(quantity),
            "records": int(count),
        }
    return {
        "events": events,
        "human_minutes": sum(
            values.get("completed", {}).get("quantity", 0)
            for event_type, values in events.items()
            if event_type == "human_minutes"
        ),
        "total_records": sum(item["records"] for statuses in events.values() for item in statuses.values()),
    }


def render_metrics(snapshot: dict[str, Any]) -> str:
    lines = [
        "# Pilot Metrics",
        "",
        f"> Human minutes recorded: {snapshot.get('human_minutes', 0):g}",
        f"> Event records: {snapshot.get('total_records', 0)}",
        "",
        "| Event | Status | Quantity | Records |",
        "|---|---|---:|---:|",
    ]
    for event_type, statuses in snapshot.get("events", {}).items():
        for status, values in statuses.items():
            lines.append(f"| {event_type} | {status} | {values['quantity']:g} | {values['records']} |")
    if not snapshot.get("events"):
        lines.append("| — | — | 0 | 0 |")
    return "\n".join(lines) + "\n"
