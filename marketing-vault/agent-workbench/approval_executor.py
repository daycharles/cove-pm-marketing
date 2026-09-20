"""Execute approved local workbench actions from the hosted Marketing OS queue.

The hosted dashboard records approvals durably. This small local runner is the bridge back to
the local Python workbench; it never sends email and never publishes a website.
"""

from __future__ import annotations

import argparse
import json
import os
import sqlite3
import time
import urllib.error
import urllib.request
from pathlib import Path
from typing import Any

import control_room


ROOT = Path(__file__).resolve().parent
DEFAULT_DB = ROOT / "data" / "runs.sqlite3"
TASK_MAP = {
    "Maintenance-focused landing section": ROOT / "tasks" / "inbox" / "example-maintenance-page.md",
    "Pilot intake + measurement brief": ROOT / "tasks" / "inbox" / "pilot-intake.md",
    "Provisional pricing hypothesis": ROOT / "tasks" / "inbox" / "pilot-pricing-research.md",
    "Evidence-led website revisions": ROOT / "tasks" / "inbox" / "website-evidence-review.md",
}


class QueueClient:
    def __init__(self, base_url: str, token: str) -> None:
        self.base_url = base_url.rstrip("/")
        self.token = token

    def request(self, method: str, path: str, payload: dict[str, Any] | None = None) -> dict[str, Any]:
        body = json.dumps(payload).encode("utf-8") if payload is not None else None
        request = urllib.request.Request(
            self.base_url + path,
            data=body,
            headers={
                "Accept": "application/json",
                "Content-Type": "application/json",
                "OAI-Sites-Authorization": f"Bearer {self.token}",
            },
            method=method,
        )
        with urllib.request.urlopen(request, timeout=30) as response:
            return json.loads(response.read().decode("utf-8"))

    def approvals(self) -> list[dict[str, Any]]:
        return self.request("GET", "/api/approvals").get("approvals", [])

    def update(self, approval_id: int, status: str, result: str) -> dict[str, Any]:
        return self.request("PATCH", f"/api/approvals/{approval_id}", {"status": status, "result": result})


def execute_once(client: QueueClient, db_path: Path) -> list[dict[str, Any]]:
    outcomes: list[dict[str, Any]] = []
    for approval in client.approvals():
        if approval.get("decision") != "Approved" or approval.get("status") != "queued":
            continue
        approval_id = int(approval["id"])
        action = approval.get("action")
        if action == "publish":
            result = "Approved and recorded. Website deployment requires the controlled Sites publishing handoff."
            client.update(approval_id, "manual-execution-required", result)
            outcomes.append({"id": approval_id, "status": "manual-execution-required"})
            continue
        if action != "workbench":
            continue
        task_path = TASK_MAP.get(str(approval.get("task")))
        if not task_path or not task_path.exists():
            result = f"No local task mapping exists for {approval.get('task')!r}."
            client.update(approval_id, "failed", result)
            outcomes.append({"id": approval_id, "status": "failed", "result": result})
            continue
        with sqlite3.connect(db_path) as db:
            local_state = db.execute(
                "SELECT status, last_output_path FROM work_queue WHERE task_path = ?",
                (str(task_path.resolve()),),
            ).fetchone()
        if local_state and local_state[0] in {"needs-review", "awaiting-approval"} and local_state[1]:
            result = f"Approved existing artifact: {local_state[1]}"
            client.update(approval_id, "completed", result)
            outcomes.append({"id": approval_id, "status": "completed", "result": result})
            continue
        client.update(approval_id, "running", "Local workbench execution started.")
        try:
            control_room.scan_inbox(task_path.parent, db_path)
            result = control_room.run_next(db_path, mock=False)
            status = "completed" if result.get("status") == "completed" else "failed"
            detail = result.get("stdout") or result.get("stderr") or result.get("message") or status
            client.update(approval_id, status, str(detail)[:4000])
            outcomes.append({"id": approval_id, "status": status})
        except (OSError, urllib.error.URLError, RuntimeError) as exc:
            client.update(approval_id, "failed", str(exc))
            outcomes.append({"id": approval_id, "status": "failed", "result": str(exc)})
    return outcomes


def main() -> int:
    parser = argparse.ArgumentParser(description="Execute approved Marketing OS workbench actions")
    parser.add_argument("--once", action="store_true", help="Process the queue once and exit")
    parser.add_argument("--interval", type=int, default=60, help="Polling interval in seconds")
    parser.add_argument("--db", type=Path, default=DEFAULT_DB)
    parser.add_argument("--site-url", default=os.environ.get("COVEPM_SITE_URL", "https://cove-pm-marketing.daycharles.chatgpt.site"))
    parser.add_argument("--token", default=os.environ.get("COVEPM_SITE_TOKEN"))
    args = parser.parse_args()
    if not args.token:
        parser.error("Set COVEPM_SITE_TOKEN to the private Site access token before running.")
    client = QueueClient(args.site_url, args.token)
    while True:
        print(json.dumps({"outcomes": execute_once(client, args.db)}))
        if args.once:
            return 0
        time.sleep(max(5, args.interval))


if __name__ == "__main__":
    raise SystemExit(main())
