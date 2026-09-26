"""Funnel and operating report for the local Averion Compass marketing-team pilot."""

from __future__ import annotations

import argparse
from pathlib import Path

import control_room
from content_factory import content_snapshot
from inbound import inbound_snapshot
from pipeline import pipeline_snapshot


ROOT = Path(__file__).resolve().parent
DEFAULT_DB = ROOT / "data" / "runs.sqlite3"
DEFAULT_OUTPUT = ROOT / "outputs" / "PILOT-REPORT.md"


def _count(mapping: dict, key: str) -> int:
    return int(mapping.get(key, 0))


def _rate(numerator: int, denominator: int) -> float | None:
    return round(numerator / denominator * 100, 1) if denominator else None


def build_report(db_path: Path) -> dict:
    metrics = control_room.metrics_snapshot(db_path)
    pipeline = pipeline_snapshot(db_path)
    content = content_snapshot(db_path)
    inbound = inbound_snapshot(db_path)
    outreach = pipeline["outreach"]
    demos = pipeline["demos"]
    messages = inbound["messages"]
    return {
        "metrics": metrics,
        "pipeline": pipeline,
        "content": content,
        "inbound": inbound,
        "funnel": {
            "draft_to_approved_pct": _rate(_count(outreach, "approved"), _count(outreach, "awaiting-send-approval") + _count(outreach, "approved") + _count(outreach, "sent")),
            "approved_to_sent_pct": _rate(_count(outreach, "sent"), _count(outreach, "approved") + _count(outreach, "sent")),
            "sent_to_scheduled_pct": _rate(_count(demos, "scheduled") + _count(demos, "held") + _count(demos, "no_show") + _count(demos, "cancelled"), _count(outreach, "sent")),
            "scheduled_to_held_pct": _rate(_count(demos, "held"), sum(_count(demos, status) for status in ("scheduled", "held", "no_show", "cancelled"))),
            "inbound_response_pct": _rate(_count(messages, "responded"), sum(messages.values())),
        },
    }


def render_report(report: dict) -> str:
    funnel = report["funnel"]
    pct = lambda value: f"{value:.1f}%" if value is not None else "n/a"
    lines = [
        "# Averion Compass Marketing Team Pilot Report",
        "",
        "## Funnel",
        "",
        "| Measure | Result |",
        "|---|---:|",
        f"| Outreach draft approval rate | {pct(funnel['draft_to_approved_pct'])} |",
        f"| Approved outreach recorded as sent | {pct(funnel['approved_to_sent_pct'])} |",
        f"| Sent outreach to demo scheduled | {pct(funnel['sent_to_scheduled_pct'])} |",
        f"| Scheduled demos held | {pct(funnel['scheduled_to_held_pct'])} |",
        f"| Inbound messages responded | {pct(funnel['inbound_response_pct'])} |",
        "",
        "## Current counts",
        "",
        f"- Pipeline: {report['pipeline']}",
        f"- Content: {report['content']}",
        f"- Inbound: {report['inbound']}",
        f"- Pilot events: {report['metrics'].get('total_records', 0)}",
        f"- Human minutes recorded: {report['metrics'].get('human_minutes', 0):g}",
        "",
        "## Review questions",
        "",
        "- Which funnel stage required the most human correction?",
        "- Which message or source produced the strongest qualified response?",
        "- What should be automated next without widening the approval boundary?",
        "",
    ]
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description="Render the local Averion Compass pilot report")
    parser.add_argument("--db", type=Path, default=DEFAULT_DB)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    args = parser.parse_args()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(render_report(build_report(args.db)), encoding="utf-8")
    print(args.output)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
