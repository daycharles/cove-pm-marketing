"""Approval-gated company lead queue primitives.

This module is deliberately side-effect free. The existing lead engine can use it
without changing its evidence collection or scoring behavior.
"""

from __future__ import annotations

from dataclasses import asdict, dataclass, field
from datetime import date
from typing import Iterable, Literal

Disposition = Literal["qualified", "review", "nurture", "disqualify"]
Decision = Literal["pending", "approved", "rejected"]
Outcome = Literal["advance", "nurture", "disqualify", "unknown"]


@dataclass
class LeadReview:
    company: str
    website: str
    disposition: Disposition
    fit_score: float
    evidence_urls: list[str] = field(default_factory=list)
    evidence_gaps: list[str] = field(default_factory=list)
    proposed_action: str = ""
    approval: Decision = "pending"
    reviewer: str = ""
    decision_date: str = ""
    outcome: Outcome | None = None
    outcome_note: str = ""
    outcome_date: str = ""

    def approve(self, reviewer: str, action: str | None = None, on: str | None = None) -> None:
        if not reviewer.strip():
            raise ValueError("reviewer is required")
        if self.disposition == "qualified" and not self.evidence_urls:
            raise ValueError("qualified leads require at least one evidence URL")
        if self.evidence_gaps:
            raise ValueError("resolve evidence gaps before approval")
        self.approval = "approved"
        self.reviewer = reviewer.strip()
        if action is not None:
            self.proposed_action = action.strip()
        self.decision_date = on or date.today().isoformat()

    def resolve_gaps(self, reviewer: str) -> None:
        if not reviewer.strip():
            raise ValueError("reviewer is required")
        self.evidence_gaps.clear()
        self.reviewer = reviewer.strip()

    def reject(self, reviewer: str, on: str | None = None) -> None:
        if not reviewer.strip():
            raise ValueError("reviewer is required")
        self.approval = "rejected"
        self.reviewer = reviewer.strip()
        self.decision_date = on or date.today().isoformat()

    def record_outcome(self, outcome: Outcome, note: str = "", on: str | None = None) -> None:
        if self.approval != "approved":
            raise ValueError("record an outcome only after approval")
        self.outcome = outcome
        self.outcome_note = note.strip()
        self.outcome_date = on or date.today().isoformat()

    def to_dict(self) -> dict:
        return asdict(self)


def queue_counts(records: Iterable[LeadReview]) -> dict[str, int]:
    counts = {key: 0 for key in ("qualified", "review", "nurture", "disqualify")}
    for record in records:
        counts[record.disposition if record.disposition in counts else "review"] += 1
    return counts


def render_queue_markdown(records: Iterable[LeadReview]) -> str:
    rows = list(records)
    counts = queue_counts(rows)
    lines = [
        "# Lead Review Queue",
        "",
        "| Disposition | Count |",
        "|---|---:|",
    ]
    lines.extend(f"| {key} | {counts[key]} |" for key in counts)
    lines.extend(["", "| Company | Score | Disposition | Approval | Evidence gaps | Next action |", "|---|---:|---|---|---|---|"])
    for record in rows:
        gaps = "; ".join(record.evidence_gaps) or "—"
        action = record.proposed_action or "—"
        lines.append(
            f"| {record.company} | {record.fit_score:.1f} | {record.disposition} | "
            f"{record.approval} | {gaps} | {action} |"
        )
    return "\n".join(lines) + "\n"
