"""Keep the approval-gated LinkedIn batch recurring without creating duplicates."""

from __future__ import annotations

import argparse
import json
from datetime import date
from pathlib import Path


ROOT = Path(__file__).resolve().parent
INBOX = ROOT / "tasks" / "inbox"
TEMPLATE = INBOX / "linkedin-daily-content-and-engagement.md"


def ensure_today() -> dict[str, object]:
    today = date.today().strftime("%Y%m%d")
    dated = INBOX / f"linkedin-daily-content-{today}.md"
    existing = sorted(INBOX.glob(f"linkedin-daily-content-{today}*.md"))
    if existing:
        return {"created": False, "path": str(existing[0]), "reason": "today's batch already exists"}
    if not TEMPLATE.exists():
        return {"created": False, "path": "", "reason": "canonical LinkedIn task is missing"}
    text = TEMPLATE.read_text(encoding="utf-8")
    text = text.replace("priority: 50", "priority: 90", 1)
    text = text.replace(
        "objective: Prepare the next CovePM LinkedIn daily batch",
        f"objective: Prepare the CovePM LinkedIn daily batch for {date.today().isoformat()}",
        1,
    )
    text = text.replace(
        "---\n\nCreate the next daily LinkedIn batch",
        f"---\n\ngenerated_for: {date.today().isoformat()}\n\nCreate the next daily LinkedIn batch",
        1,
    )
    dated.write_text(text, encoding="utf-8")
    return {"created": True, "path": str(dated), "reason": "created today's recurring batch"}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("command", choices=["ensure"])
    args = parser.parse_args()
    print(json.dumps(ensure_today()))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
