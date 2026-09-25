"""Local control room for the CovePM marketing workbench.

The control room is deliberately local-first: it discovers Markdown tasks, keeps a durable
queue/action view in the existing SQLite ledger, and exposes a small CLI for status and one-at-a-
time execution. It does not publish, send, or modify external systems.
"""

from __future__ import annotations

import argparse
import json
import sqlite3
import subprocess
import sys
from contextlib import closing
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

import run as workbench
from pilot_metrics import init_metrics_db, insert_event, metrics_snapshot, record_event, render_metrics


ROOT = Path(__file__).resolve().parent
DEFAULT_INBOX = ROOT / "tasks" / "inbox"
DEFAULT_DB = ROOT / "data" / "runs.sqlite3"
DEFAULT_CONTROL_ROOM = ROOT / "outputs" / "CONTROL-ROOM.md"
DEFAULT_WEEKLY_OUTPUT = ROOT / "outputs" / "WEEKLY-REVIEW.md"


def now_iso() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat()


def task_id(path: Path) -> str:
    return path.resolve().as_posix()


def init_control_room_db(db_path: Path) -> None:
    workbench.init_db(db_path)
    init_metrics_db(db_path)
    db_path.parent.mkdir(parents=True, exist_ok=True)
    with closing(sqlite3.connect(db_path)) as db:
        db.execute(
            """CREATE TABLE IF NOT EXISTS work_queue (
                task_id TEXT PRIMARY KEY,
                task_path TEXT NOT NULL,
                objective TEXT NOT NULL,
                audience TEXT NOT NULL,
                workflow TEXT NOT NULL,
                priority INTEGER NOT NULL DEFAULT 50,
                approval_required INTEGER NOT NULL DEFAULT 1,
                status TEXT NOT NULL DEFAULT 'queued',
                last_run_id TEXT,
                last_output_path TEXT,
                last_error TEXT,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            )"""
        )
        db.execute(
            """CREATE TABLE IF NOT EXISTS action_ledger (
                action_id INTEGER PRIMARY KEY AUTOINCREMENT,
                task_id TEXT NOT NULL,
                run_id TEXT,
                action_type TEXT NOT NULL,
                status TEXT NOT NULL,
                details_json TEXT NOT NULL,
                created_at TEXT NOT NULL
            )"""
        )
        db.execute(
            """CREATE TABLE IF NOT EXISTS approval_queue (
                approval_id INTEGER PRIMARY KEY AUTOINCREMENT,
                task_id TEXT NOT NULL,
                run_id TEXT,
                requested_action TEXT NOT NULL,
                status TEXT NOT NULL DEFAULT 'pending',
                requested_at TEXT NOT NULL,
                resolved_at TEXT,
                reviewer TEXT,
                decision TEXT
            )"""
        )
        db.commit()


def _frontmatter(path: Path) -> dict[str, str]:
    fields, _ = workbench.parse_frontmatter(path.read_text(encoding="utf-8"))
    return fields


def scan_inbox(inbox: Path, db_path: Path) -> list[dict[str, Any]]:
    init_control_room_db(db_path)
    discovered: list[dict[str, Any]] = []
    with closing(sqlite3.connect(db_path)) as db:
        for path in sorted(inbox.glob("*.md")):
            fields = _frontmatter(path)
            identifier = task_id(path)
            now = now_iso()
            objective = fields.get("objective", path.stem.replace("-", " "))
            audience = fields.get("audience", "Averion Software marketing audience")
            workflow = fields.get("workflow", "content-draft")
            try:
                priority = int(fields.get("priority", "50"))
            except ValueError:
                priority = 50
            approval_required = fields.get("approval_required", "true").lower() not in {"false", "no", "0"}
            db.execute(
                """INSERT INTO work_queue
                   (task_id, task_path, objective, audience, workflow, priority,
                    approval_required, status, created_at, updated_at)
                   VALUES (?, ?, ?, ?, ?, ?, ?, 'queued', ?, ?)
                   ON CONFLICT(task_id) DO UPDATE SET
                     objective=excluded.objective,
                     audience=excluded.audience,
                     workflow=excluded.workflow,
                     priority=excluded.priority,
                     approval_required=excluded.approval_required,
                     updated_at=excluded.updated_at""",
                (identifier, str(path.resolve()), objective, audience, workflow, priority, int(approval_required), now, now),
            )
            discovered.append({"task_id": identifier, "objective": objective, "status": "queued"})
        db.commit()
    return discovered


def sync_runs(db_path: Path) -> int:
    init_control_room_db(db_path)
    updated = 0
    with closing(sqlite3.connect(db_path)) as db:
        runs = db.execute(
            "SELECT run_id, task_path, status, output_path, error FROM runs ORDER BY started_at"
        ).fetchall()
        for run_id, task_path, run_status, output_path, error in runs:
            mapped = {
                "ready-for-human-review": "awaiting-approval",
                "needs-review": "queued",
                "error": "blocked",
            }.get(run_status, run_status)
            if run_status == "needs-review":
                attempts = db.execute(
                    "SELECT COUNT(*) FROM runs WHERE task_path=? AND status='needs-review'", (task_path,)
                ).fetchone()[0]
                if attempts >= 3:
                    mapped = "blocked"
            result = db.execute(
                """UPDATE work_queue
                   SET status=?, last_run_id=?, last_output_path=?, last_error=?, updated_at=?
                   WHERE task_path=?""",
                (mapped, run_id, output_path, error, now_iso(), task_path),
            )
            if result.rowcount:
                updated += result.rowcount
                if mapped == "awaiting-approval":
                    exists = db.execute(
                        "SELECT 1 FROM approval_queue WHERE run_id=? AND status='pending'", (run_id,)
                    ).fetchone()
                    if not exists:
                        requested_action = (
                            "Review generated artifact and resolve QA warnings before publishing or outreach"
                            if mapped == "needs-review"
                            else "Review generated artifact before publishing or outreach"
                        )
                        db.execute(
                            """INSERT INTO approval_queue
                               (task_id, run_id, requested_action, requested_at)
                               SELECT task_id, ?, ?, ?
                               FROM work_queue WHERE task_path=?""",
                            (run_id, requested_action, now_iso(), task_path),
                        )
                        insert_event(db, "approval_requested", details={"run_id": run_id, "task_path": task_path})
        db.commit()
    return updated


def queue_snapshot(db_path: Path) -> dict[str, Any]:
    init_control_room_db(db_path)
    with closing(sqlite3.connect(db_path)) as db:
        db.row_factory = sqlite3.Row
        tasks = [dict(row) for row in db.execute(
            """SELECT task_id, objective, workflow, priority, status, last_run_id,
                      last_output_path, last_error
               FROM work_queue ORDER BY priority DESC, updated_at ASC"""
        )]
        approvals = [dict(row) for row in db.execute(
            """SELECT approval_id, task_id, run_id, requested_action, status, requested_at
               FROM approval_queue WHERE status='pending' ORDER BY requested_at"""
        )]
        lead_queue: list[dict[str, Any]] = []
        try:
            lead_queue = [dict(row) for row in db.execute(
                """SELECT lead_id, company, fit_score, disposition, approval, next_action,
                          outcome, updated_at
                   FROM leads ORDER BY fit_score DESC, updated_at DESC"""
            )]
        except sqlite3.OperationalError:
            # Lead qualification is optional; an empty database should still have a usable control room.
            lead_queue = []
    counts: dict[str, int] = {}
    for task in tasks:
        counts[task["status"]] = counts.get(task["status"], 0) + 1
    lead_counts: dict[str, int] = {}
    for lead in lead_queue:
        key = lead["approval"] if lead["approval"] != "approved" else (lead["outcome"] or "approved")
        lead_counts[key] = lead_counts.get(key, 0) + 1
    return {"counts": counts, "tasks": tasks, "pending_approvals": approvals, "lead_queue": lead_queue, "lead_counts": lead_counts}


def next_task(db_path: Path) -> dict[str, Any] | None:
    with closing(sqlite3.connect(db_path)) as db:
        db.row_factory = sqlite3.Row
        row = db.execute(
            """SELECT * FROM work_queue
               WHERE status='queued'
               ORDER BY priority DESC, updated_at ASC LIMIT 1"""
        ).fetchone()
    return dict(row) if row else None


def run_next(db_path: Path, *, mock: bool = False, config_path: Path | None = None) -> dict[str, Any]:
    task = next_task(db_path)
    if not task:
        return {"status": "idle", "message": "No queued task is ready."}
    command = [sys.executable, str(ROOT / "run.py"), "--task", task["task_path"]]
    if config_path:
        command.extend(["--config", str(config_path)])
    if mock:
        command.append("--mock")
    if task.get("last_run_id"):
        with closing(sqlite3.connect(db_path)) as db:
            prior = db.execute(
                "SELECT qa_json, output_path FROM runs WHERE run_id=?", (task["last_run_id"],)
            ).fetchone()
        if prior:
            qa_json, output_path = prior
            feedback_parts = [f"Previous QA findings: {qa_json or 'none'}"]
            if output_path and Path(output_path).exists():
                prior_text = Path(output_path).read_text(encoding="utf-8")
                feedback_parts.append("Previous draft artifact:\n" + prior_text[-6000:])
            command.extend(["--revision-feedback", "\n\n".join(feedback_parts)])
    process = subprocess.run(command, cwd=ROOT, capture_output=True, text=True, check=False)
    with closing(sqlite3.connect(db_path)) as db:
        db.execute(
            "INSERT INTO action_ledger (task_id, action_type, status, details_json, created_at) VALUES (?, ?, ?, ?, ?)",
            (task["task_id"], "local-run", "completed" if process.returncode == 0 else "failed", json.dumps({"stdout": process.stdout, "stderr": process.stderr}), now_iso()),
        )
        db.commit()
    record_event(
        db_path,
        "workbench_run",
        status="completed" if process.returncode == 0 else "failed",
        details={"task_id": task["task_id"], "workflow": task["workflow"]},
    )
    sync_runs(db_path)
    return {"status": "completed" if process.returncode == 0 else "failed", "task": task["objective"], "stdout": process.stdout, "stderr": process.stderr}


def render_control_room(snapshot: dict[str, Any]) -> str:
    counts = snapshot["counts"]
    count_text = ", ".join(f"{key}: {value}" for key, value in sorted(counts.items())) or "empty"
    lines = [
        "# Local Marketing Control Room",
        "",
        f"> Updated: {now_iso()}",
        f"> Queue: {count_text}",
        "",
        "## Work queue",
        "",
        "| Priority | Status | Workflow | Objective | Output |",
        "| ---: | --- | --- | --- | --- |",
    ]
    for task in snapshot["tasks"]:
        output = Path(task["last_output_path"]).name if task.get("last_output_path") else "—"
        lines.append(f"| {task['priority']} | {task['status']} | {task['workflow']} | {task['objective']} | {output} |")
    if not snapshot["tasks"]:
        lines.append("| — | — | — | No tasks discovered | — |")
    lines.extend(["", "## Pending approvals", ""])
    if snapshot["pending_approvals"]:
        for approval in snapshot["pending_approvals"]:
            lines.append(f"- **#{approval['approval_id']}** {approval['requested_action']} — run `{approval['run_id']}`")
    else:
        lines.append("- None")
    lines.extend(["", "## Lead review queue", ""])
    lead_counts = snapshot.get("lead_counts", {})
    if lead_counts:
        lines.append("> " + ", ".join(f"{key}: {value}" for key, value in sorted(lead_counts.items())))
        lines.extend([
            "",
            "| Company | Fit | Disposition | Approval | Outcome | Next action |",
            "|---|---:|---|---|---|---|",
        ])
        for lead in snapshot["lead_queue"]:
            lines.append(
                f"| {lead['company']} | {lead['fit_score']} | {lead['disposition']} | "
                f"{lead['approval']} | {lead['outcome'] or '—'} | {lead['next_action']} |"
            )
    else:
        lines.append("- No company leads have been qualified yet.")
    lines.extend([
        "",
        "## Operating boundary",
        "",
        "Local research, drafting, QA, and queue management may run automatically. Publishing, outreach, pricing, commitments, and sensitive-data transmission remain approval-gated.",
        "",
    ])
    return "\n".join(lines)


def weekly_review_snapshot(db_path: Path) -> dict[str, Any]:
    # These imports stay local to avoid circular imports: each subsystem uses the control-room DB initializer.
    from content_factory import content_snapshot
    from inbound import inbound_snapshot
    from pipeline import pipeline_snapshot

    snapshot = queue_snapshot(db_path)
    leads = snapshot.get("lead_queue", [])
    return {
        "counts": snapshot.get("counts", {}),
        "lead_counts": snapshot.get("lead_counts", {}),
        "lead_queue": leads,
        "pending_approvals": snapshot.get("pending_approvals", []),
        "lead_outcomes": {
            outcome: sum(1 for lead in leads if lead.get("outcome") == outcome)
            for outcome in ("advance", "nurture", "disqualify", "unknown")
        },
        "undecided_leads": sum(1 for lead in leads if lead.get("approval") == "pending"),
        "pilot_metrics": metrics_snapshot(db_path),
        "pipeline": pipeline_snapshot(db_path),
        "content": content_snapshot(db_path),
        "inbound": inbound_snapshot(db_path),
    }


def render_weekly_review(review: dict[str, Any]) -> str:
    lines = [
        "# CovePM Marketing Weekly Review",
        "",
        f"> Generated: {now_iso()}",
        "> Scope: local workbench queue, company-level lead qualification, and human approval state.",
        "",
        "## Executive view",
        "",
        "| Area | Status |",
        "|---|---|",
        f"| Work queue | {', '.join(f'{key}: {value}' for key, value in sorted(review['counts'].items())) or 'empty'} |",
        f"| Lead queue | {', '.join(f'{key}: {value}' for key, value in sorted(review['lead_counts'].items())) or 'empty'} |",
        f"| Undecided leads | {review['undecided_leads']} |",
        f"| Pending approvals | {len(review['pending_approvals'])} |",
        "",
        "## Lead outcomes",
        "",
        "| Outcome | Count |",
        "|---|---:|",
    ]
    lines.extend(f"| {key} | {value} |" for key, value in review["lead_outcomes"].items())
    lines.extend(["", "## Recommended review order", ""])
    pending = [lead for lead in review["lead_queue"] if lead.get("approval") == "pending"]
    if pending:
        for lead in pending:
            lines.append(
                f"- **{lead['company']}** — fit {lead['fit_score']}, {lead['disposition']}; "
                f"next action: {lead['next_action']}"
            )
    else:
        lines.append("- No pending lead decisions.")
    lines.extend([
        "",
        "## Pilot metrics",
        "",
        render_metrics(review["pilot_metrics"]).split("\n", 4)[4] if review.get("pilot_metrics", {}).get("events") else "- No pilot events recorded yet.",
        "",
        "## Team subsystem status",
        "",
        f"- Lead-to-demo pipeline: {review['pipeline']}",
        f"- Content factory: {review['content']}",
        f"- Inbound coordinator: {review['inbound']}",
        "",
        "## Operating decisions",
        "",
        "- [ ] Review evidence and approve, reject, or resolve gaps for pending leads.",
        "- [ ] Record an outcome for every approved lead, including `unknown` where no result is available.",
        "- [ ] Keep the next research action company-level and reversible.",
        "- [ ] Do not send outreach, publish, or make commercial commitments from this report.",
        "",
    ])
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description="Operate the local CovePM marketing control room")
    parser.add_argument("command", choices=["scan", "sync", "status", "dashboard", "weekly-review", "pilot-metrics", "record-event", "run-next"])
    parser.add_argument("--inbox", type=Path, default=DEFAULT_INBOX)
    parser.add_argument("--db", type=Path, default=DEFAULT_DB)
    parser.add_argument("--output", type=Path, default=DEFAULT_CONTROL_ROOM)
    parser.add_argument("--weekly-output", type=Path, default=DEFAULT_WEEKLY_OUTPUT)
    parser.add_argument("--config", type=Path, default=None)
    parser.add_argument("--mock", action="store_true")
    parser.add_argument("--event-type", default="")
    parser.add_argument("--event-status", default="completed")
    parser.add_argument("--quantity", type=float, default=1)
    args = parser.parse_args()

    if args.command == "scan":
        print(json.dumps({"discovered": scan_inbox(args.inbox, args.db)}, indent=2))
    elif args.command == "sync":
        print(json.dumps({"updated": sync_runs(args.db)}, indent=2))
    elif args.command == "status":
        sync_runs(args.db)
        print(json.dumps(queue_snapshot(args.db), indent=2))
    elif args.command == "dashboard":
        scan_inbox(args.inbox, args.db)
        sync_runs(args.db)
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(render_control_room(queue_snapshot(args.db)), encoding="utf-8")
        print(json.dumps({"output": str(args.output)}, indent=2))
    elif args.command == "weekly-review":
        scan_inbox(args.inbox, args.db)
        sync_runs(args.db)
        args.weekly_output.parent.mkdir(parents=True, exist_ok=True)
        args.weekly_output.write_text(render_weekly_review(weekly_review_snapshot(args.db)), encoding="utf-8")
        print(json.dumps({"output": str(args.weekly_output)}, indent=2))
    elif args.command == "pilot-metrics":
        print(json.dumps(metrics_snapshot(args.db), indent=2))
    elif args.command == "record-event":
        event_id = record_event(args.db, args.event_type, status=args.event_status, quantity=args.quantity)
        print(json.dumps({"event_id": event_id}, indent=2))
    else:
        scan_inbox(args.inbox, args.db)
        print(json.dumps(run_next(args.db, mock=args.mock, config_path=args.config), indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
