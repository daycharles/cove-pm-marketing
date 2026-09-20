"""Validation and onboarding records for an external CovePM marketing-team pilot."""

from __future__ import annotations

import argparse
import json
from dataclasses import asdict, dataclass, field
from datetime import date
from pathlib import Path


ALLOWED_CHANNELS = {"website", "email", "social", "inbound", "calendar"}


@dataclass
class PilotConfig:
    account: str
    segment: str
    workflow: str
    baseline_metric: str
    success_metric: str
    review_date: str
    allowed_channels: list[str] = field(default_factory=list)
    approved_facts: list[str] = field(default_factory=list)
    escalation_owner: str = ""
    data_boundary: str = "Customer records remain isolated from CovePM internal records."

    def validate(self) -> list[str]:
        errors: list[str] = []
        required = {
            "account": self.account,
            "segment": self.segment,
            "workflow": self.workflow,
            "baseline_metric": self.baseline_metric,
            "success_metric": self.success_metric,
            "review_date": self.review_date,
            "escalation_owner": self.escalation_owner,
        }
        errors.extend(f"{key} is required" for key, value in required.items() if not value.strip())
        invalid_channels = sorted(set(self.allowed_channels) - ALLOWED_CHANNELS)
        if invalid_channels:
            errors.append(f"unsupported channels: {', '.join(invalid_channels)}")
        try:
            date.fromisoformat(self.review_date)
        except ValueError:
            errors.append("review_date must be YYYY-MM-DD")
        if not self.approved_facts:
            errors.append("at least one approved fact is required")
        if "email" in self.allowed_channels and "inbound" not in self.allowed_channels:
            errors.append("email pilots must include inbound handling for replies and opt-outs")
        return errors

    def require_valid(self) -> None:
        errors = self.validate()
        if errors:
            raise ValueError("; ".join(errors))

    def to_dict(self) -> dict:
        return asdict(self)


def load_config(path: Path) -> PilotConfig:
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise ValueError("pilot config must be a JSON object")
    config = PilotConfig(**payload)
    config.require_valid()
    return config


def render_onboarding(config: PilotConfig) -> str:
    config.require_valid()
    lines = [
        f"# External Pilot — {config.account}",
        "",
        f"- Segment: {config.segment}",
        f"- Workflow: {config.workflow}",
        f"- Baseline: {config.baseline_metric}",
        f"- Success metric: {config.success_metric}",
        f"- Review date: {config.review_date}",
        f"- Escalation owner: {config.escalation_owner}",
        f"- Data boundary: {config.data_boundary}",
        "",
        "## Allowed channels",
        "",
    ]
    lines.extend(f"- {channel}" for channel in config.allowed_channels)
    lines.extend(["", "## Approved facts", ""])
    lines.extend(f"- {fact}" for fact in config.approved_facts)
    lines.extend([
        "",
        "## Launch gates",
        "",
        "- [ ] Customer owner and escalation path confirmed",
        "- [ ] Workflow boundary and baseline recorded",
        "- [ ] Approved facts and brand guidance loaded",
        "- [ ] Data boundary reviewed",
        "- [ ] Human approval required for sends, publishing, pricing, commitments, and sensitive replies",
        "- [ ] Review date scheduled",
    ])
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate an external CovePM pilot configuration")
    parser.add_argument("config", type=Path)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    config = load_config(args.config)
    rendered = render_onboarding(config)
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(rendered, encoding="utf-8")
        print(json.dumps({"valid": True, "output": str(args.output)}, indent=2))
    else:
        print(rendered)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
