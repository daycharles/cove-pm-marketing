"""Deterministic pilot-readiness scoring for approved discovery conversations."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Mapping


CRITERIA = (
    "workflow_scope",
    "active_pain",
    "baseline_metric",
    "operational_owner",
    "decision_path",
    "data_questions",
    "bounded_scope",
    "success_criteria",
)


@dataclass
class PilotReadiness:
    account: str
    scores: dict[str, int] = field(default_factory=dict)
    notes: dict[str, str] = field(default_factory=dict)
    approved_next_action: str = ""

    def __post_init__(self) -> None:
        unknown = set(self.scores) - set(CRITERIA)
        if unknown:
            raise ValueError(f"Unknown readiness criteria: {', '.join(sorted(unknown))}")
        for criterion, score in self.scores.items():
            if score not in (0, 1, 2):
                raise ValueError(f"{criterion} must be scored 0, 1, or 2")

    @property
    def total(self) -> int:
        return sum(self.scores.values())

    @property
    def decision(self) -> str:
        if self.total >= 12 and len(self.scores) == len(CRITERIA):
            return "pilot-design ready"
        if self.total >= 7:
            return "discovery-qualified"
        return "research / nurture"

    @property
    def complete(self) -> bool:
        return len(self.scores) == len(CRITERIA)

    def score(self, criterion: str, value: int, note: str = "") -> None:
        if criterion not in CRITERIA:
            raise ValueError(f"Unknown readiness criterion: {criterion}")
        if value not in (0, 1, 2):
            raise ValueError("Readiness scores must be 0, 1, or 2")
        self.scores[criterion] = value
        if note:
            self.notes[criterion] = note.strip()

    def to_dict(self) -> dict:
        return {
            "account": self.account,
            "scores": dict(self.scores),
            "notes": dict(self.notes),
            "total": self.total,
            "decision": self.decision,
            "complete": self.complete,
            "approved_next_action": self.approved_next_action,
        }


def from_scores(account: str, scores: Mapping[str, int], notes: Mapping[str, str] | None = None) -> PilotReadiness:
    return PilotReadiness(account, dict(scores), dict(notes or {}))


def render_scorecard(record: PilotReadiness) -> str:
    lines = [
        f"# Pilot Readiness — {record.account}",
        "",
        f"**Score:** {record.total}/16  ",
        f"**Decision:** {record.decision}  ",
        f"**Complete:** {'yes' if record.complete else 'no'}",
        "",
        "| Criterion | Score | Note |",
        "|---|---:|---|",
    ]
    for criterion in CRITERIA:
        lines.append(f"| {criterion.replace('_', ' ')} | {record.scores.get(criterion, '—')} | {record.notes.get(criterion, '—')} |")
    lines.extend([
        "",
        "## Guardrail",
        "",
        "A pilot-design-ready score does not approve pricing, discounts, price locks, implementation timelines, security claims, or any agreement.",
        "",
    ])
    return "\n".join(lines)
