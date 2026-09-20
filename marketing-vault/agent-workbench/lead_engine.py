"""Local-first company lead qualification for CovePM.

This workflow accepts supplied company-level candidates and approved evidence. It intentionally
stops at qualification and a recommended next action. It does not discover personal contacts,
scrape restricted data, send messages, or create commitments.
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
from lead_activation import LeadReview, render_queue_markdown
import run as workbench


ROOT = Path(__file__).resolve().parent
DEFAULT_DB = ROOT / "data" / "runs.sqlite3"
DEFAULT_OUTPUT = ROOT / "outputs" / "lead-qualification.md"


def now_iso() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat()


def init_lead_db(db_path: Path) -> None:
    control_room.init_control_room_db(db_path)
    with closing(sqlite3.connect(db_path)) as db:
        db.execute(
            """CREATE TABLE IF NOT EXISTS leads (
                lead_id TEXT PRIMARY KEY,
                company TEXT NOT NULL,
                website TEXT,
                segment TEXT,
                fit_score INTEGER,
                disposition TEXT NOT NULL,
                evidence_json TEXT NOT NULL,
                next_action TEXT NOT NULL,
                run_id TEXT NOT NULL,
                approval_required INTEGER NOT NULL DEFAULT 1,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            )"""
        )
        columns = {row[1] for row in db.execute("PRAGMA table_info(leads)")}
        if "source_system" not in columns:
            db.execute("ALTER TABLE leads ADD COLUMN source_system TEXT NOT NULL DEFAULT 'public-web'")
        if "source_url" not in columns:
            db.execute("ALTER TABLE leads ADD COLUMN source_url TEXT")
        migrations = {
            "approval": "ALTER TABLE leads ADD COLUMN approval TEXT NOT NULL DEFAULT 'pending'",
            "reviewer": "ALTER TABLE leads ADD COLUMN reviewer TEXT NOT NULL DEFAULT ''",
            "decision_date": "ALTER TABLE leads ADD COLUMN decision_date TEXT NOT NULL DEFAULT ''",
            "outcome": "ALTER TABLE leads ADD COLUMN outcome TEXT",
            "outcome_note": "ALTER TABLE leads ADD COLUMN outcome_note TEXT NOT NULL DEFAULT ''",
            "outcome_date": "ALTER TABLE leads ADD COLUMN outcome_date TEXT NOT NULL DEFAULT ''",
        }
        for column, statement in migrations.items():
            if column not in columns:
                db.execute(statement)
        db.commit()


def load_candidates(path: Path) -> list[dict[str, Any]]:
    if path.suffix.lower() == ".json":
        data = json.loads(path.read_text(encoding="utf-8"))
        if not isinstance(data, list):
            raise ValueError("Candidate JSON must contain an array of company records.")
        return [item for item in data if isinstance(item, dict)]
    candidates: list[dict[str, Any]] = []
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.strip() and not line.lstrip().startswith("#"):
            item = json.loads(line)
            if isinstance(item, dict):
                candidates.append(item)
    return candidates


def candidate_id(candidate: dict[str, Any]) -> str:
    company = str(candidate.get("company", "")).strip().lower()
    website = str(candidate.get("website", "")).strip().lower()
    return f"{company}|{website}"


def preflight(candidate: dict[str, Any]) -> list[str]:
    missing = []
    for field in ("company", "segment", "evidence"):
        if not candidate.get(field):
            missing.append(field)
    if not candidate.get("source_urls"):
        missing.append("source_urls")
    return missing


def mock_qualification(candidate: dict[str, Any]) -> dict[str, Any]:
    evidence = str(candidate.get("evidence", "")).lower()
    signal_words = ("inspection", "maintenance", "repair", "work order", "overdue", "resident", "turn", "vendor", "make-ready", "field", "procurement", "quality control")
    matched = [word for word in signal_words if word in evidence]
    score = min(95, 35 + len(matched) * 7 + (15 if candidate.get("portfolio_context") else 0))
    disposition = "qualified" if score >= 70 else "review"
    return {
        "fit_score": score,
        "disposition": disposition,
        "evidence": matched,
        "gaps": ["Confirm operational volume and buying authority before outreach."],
        "next_action": "Research the company-level maintenance workflow and identify an approved business contact path.",
        "message_angle": "Explore whether maintenance workflow visibility and resident communication are current operating priorities.",
    }


def normalize_result(candidate: dict[str, Any], result: dict[str, Any]) -> dict[str, Any]:
    """Reject malformed local-model decisions and fall back to conservative rules."""
    allowed = {"qualified", "review", "nurture", "disqualify"}
    try:
        score = int(result.get("fit_score", -1))
    except (TypeError, ValueError):
        score = -1
    disposition = str(result.get("disposition", "")).strip().lower()
    if score < 1 or score > 100 or disposition not in allowed:
        return mock_qualification(candidate)
    clean = {
        "fit_score": score,
        "disposition": disposition,
        "evidence": result.get("evidence", []) if isinstance(result.get("evidence", []), list) else [],
        "gaps": result.get("gaps", []) if isinstance(result.get("gaps", []), list) else [],
        "next_action": str(result.get("next_action", "Review company-level evidence.")),
        "message_angle": str(result.get("message_angle", "Keep the next step company-level and approval-gated.")),
    }
    if any(word in clean["message_angle"].lower() for word in ("automated compliance", "guarantee", "will reduce", "will improve")):
        clean["message_angle"] = "Explore whether this workflow is a current operational priority and what evidence a pilot would need."
    return clean


def make_prompt(candidate: dict[str, Any]) -> str:
    return f"""You are CovePM's local lead qualification agent.

Evaluate one company-level candidate using only the supplied record. Return JSON only:
{{
  "fit_score": 0,
  "disposition": "qualified|review|nurture|disqualify",
  "evidence": ["specific evidence from the record"],
  "gaps": ["missing evidence or qualification question"],
  "next_action": "one reversible company-level research action",
  "message_angle": "a non-send recommendation for a future approved message"
}}

Rules:
- Do not invent contacts, people, customers, portfolio size, software, pricing, benchmarks, or outcomes.
- Do not infer a personal email address or scrape personal data.
- A high score requires evidence of property-management fit, an operational maintenance signal,
  and a reachable business research path.
- If evidence is weak, choose review or nurture rather than qualified.
- Never recommend sending a message in this output; recommend research or an approval-gated next step.

CANDIDATE:
{json.dumps(candidate, indent=2)}
"""


def qualify_candidate(candidate: dict[str, Any], config: dict[str, Any], *, mock: bool = False) -> dict[str, Any]:
    missing = preflight(candidate)
    if missing:
        return {
            "fit_score": 0,
            "disposition": "review",
            "evidence": [],
            "gaps": [f"Missing required company-level fields: {', '.join(missing)}."],
            "next_action": "Add approved company-level evidence and source URLs before qualification.",
            "message_angle": "",
        }
    if mock:
        return mock_qualification(candidate)
    result = workbench.call_ollama(
        str(config["ollama_url"]),
        str(config.get("model", "qwen3:4b")),
        make_prompt(candidate),
        timeout=int(config.get("ollama_timeout_seconds", 600)),
        num_predict=int(config.get("lead_qualification_num_predict", 512)),
        think=bool(config.get("ollama_think", False)),
    )
    return normalize_result(candidate, result)


def save_lead(db_path: Path, candidate: dict[str, Any], result: dict[str, Any], run_id: str) -> None:
    init_lead_db(db_path)
    with closing(sqlite3.connect(db_path)) as db:
        db.execute(
            """INSERT INTO leads
               (lead_id, company, website, segment, fit_score, disposition, evidence_json,
                next_action, run_id, approval_required, created_at, updated_at, source_system, source_url,
                approval, reviewer, decision_date, outcome, outcome_note, outcome_date)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, 1, COALESCE((SELECT created_at FROM leads WHERE lead_id=?), ?), ?, ?, ?,
                       COALESCE((SELECT approval FROM leads WHERE lead_id=?), 'pending'),
                       COALESCE((SELECT reviewer FROM leads WHERE lead_id=?), ''),
                       COALESCE((SELECT decision_date FROM leads WHERE lead_id=?), ''),
                       (SELECT outcome FROM leads WHERE lead_id=?),
                       COALESCE((SELECT outcome_note FROM leads WHERE lead_id=?), ''),
                       COALESCE((SELECT outcome_date FROM leads WHERE lead_id=?), ''))
               ON CONFLICT(lead_id) DO UPDATE SET
                 company=excluded.company, website=excluded.website, segment=excluded.segment,
                 fit_score=excluded.fit_score, disposition=excluded.disposition, evidence_json=excluded.evidence_json,
                 next_action=excluded.next_action, run_id=excluded.run_id, updated_at=excluded.updated_at,
                 source_system=excluded.source_system, source_url=excluded.source_url""",
            (
                candidate_id(candidate),
                str(candidate.get("company", "")),
                str(candidate.get("website", "")),
                str(candidate.get("segment", "")),
                int(result.get("fit_score", 0)),
                str(result.get("disposition", "review")),
                json.dumps({"candidate": candidate, "qualification": result}),
                str(result.get("next_action", "Review candidate.")),
                run_id,
                candidate_id(candidate),
                now_iso(),
                now_iso(),
                str(candidate.get("source_system", "public-web")),
                str(candidate.get("notion_page_url", candidate.get("source_urls", [""])[0] if candidate.get("source_urls") else "")),
                candidate_id(candidate),
                candidate_id(candidate),
                candidate_id(candidate),
                candidate_id(candidate),
                candidate_id(candidate),
                candidate_id(candidate),
            ),
        )
        db.commit()


def _lead_from_row(row: sqlite3.Row) -> LeadReview:
    evidence = json.loads(row["evidence_json"] or "{}")
    candidate = evidence.get("candidate", {})
    result = evidence.get("qualification", {})
    return LeadReview(
        company=row["company"],
        website=row["website"] or "",
        disposition=row["disposition"],
        fit_score=float(row["fit_score"] or 0),
        evidence_urls=list(candidate.get("source_urls", [])),
        evidence_gaps=list(result.get("gaps", [])),
        proposed_action=row["next_action"] or "",
        approval=row["approval"] or "pending",
        reviewer=row["reviewer"] or "",
        decision_date=row["decision_date"] or "",
        outcome=row["outcome"],
        outcome_note=row["outcome_note"] or "",
        outcome_date=row["outcome_date"] or "",
    )


def list_leads(db_path: Path, *, disposition: str | None = None, approval: str | None = None) -> list[LeadReview]:
    init_lead_db(db_path)
    clauses: list[str] = []
    values: list[str] = []
    if disposition:
        clauses.append("disposition=?")
        values.append(disposition)
    if approval:
        clauses.append("approval=?")
        values.append(approval)
    where = f" WHERE {' AND '.join(clauses)}" if clauses else ""
    with closing(sqlite3.connect(db_path)) as db:
        db.row_factory = sqlite3.Row
        rows = db.execute(f"SELECT * FROM leads{where} ORDER BY fit_score DESC, updated_at DESC", values).fetchall()
    return [_lead_from_row(row) for row in rows]


def _update_review(db_path: Path, lead_id: str, review: LeadReview) -> None:
    with closing(sqlite3.connect(db_path)) as db:
        result = db.execute(
            """UPDATE leads SET next_action=?, approval=?, reviewer=?, decision_date=?, outcome=?,
               outcome_note=?, outcome_date=?, updated_at=? WHERE lead_id=?""",
            (review.proposed_action, review.approval, review.reviewer, review.decision_date,
             review.outcome, review.outcome_note, review.outcome_date, now_iso(), lead_id),
        )
        if result.rowcount != 1:
            raise KeyError(f"Unknown lead: {lead_id}")
        db.commit()


def resolve_lead_gaps(db_path: Path, lead_id: str, reviewer: str) -> LeadReview:
    init_lead_db(db_path)
    with closing(sqlite3.connect(db_path)) as db:
        db.row_factory = sqlite3.Row
        row = db.execute("SELECT * FROM leads WHERE lead_id=?", (lead_id,)).fetchone()
        if not row:
            raise KeyError(f"Unknown lead: {lead_id}")
        evidence = json.loads(row["evidence_json"] or "{}")
        result = evidence.get("qualification", {})
        result["gaps"] = []
        evidence["qualification"] = result
        db.execute(
            "UPDATE leads SET evidence_json=?, reviewer=?, updated_at=? WHERE lead_id=?",
            (json.dumps(evidence), reviewer.strip(), now_iso(), lead_id),
        )
        db.commit()
    reviews = list_leads(db_path)
    return next(review for review in reviews if f"{review.company.lower()}|{review.website.lower()}" == lead_id.lower())


def approve_lead(db_path: Path, lead_id: str, reviewer: str, action: str | None = None, on: str | None = None) -> LeadReview:
    reviews = list_leads(db_path)
    matches = [review for review in reviews if f"{review.company.lower()}|{review.website.lower()}" == lead_id.lower()]
    if not matches:
        raise KeyError(f"Unknown lead: {lead_id}")
    review = matches[0]
    review.approve(reviewer, action, on)
    _update_review(db_path, lead_id, review)
    return review


def record_lead_outcome(db_path: Path, lead_id: str, outcome: str, note: str = "", on: str | None = None) -> LeadReview:
    reviews = list_leads(db_path)
    matches = [review for review in reviews if f"{review.company.lower()}|{review.website.lower()}" == lead_id.lower()]
    if not matches:
        raise KeyError(f"Unknown lead: {lead_id}")
    review = matches[0]
    review.record_outcome(outcome, note, on)
    _update_review(db_path, lead_id, review)
    return review


def render_queue(db_path: Path) -> str:
    return render_queue_markdown(list_leads(db_path))


def render_report(records: list[dict[str, Any]], run_id: str) -> str:
    lines = [
        "# Lead Qualification Report",
        "",
        f"> Run: `{run_id}`",
        f"> Created: {now_iso()}",
        "> Scope: company-level qualification from supplied evidence; no contact discovery or sending.",
        "",
        "## Qualification queue",
        "",
        "| Company | Segment | Source | Fit | Disposition | Next action |",
        "| --- | --- | --- | ---: | --- | --- |",
    ]
    for record in records:
        lines.append(
            f"| {record['company']} | {record.get('segment', '')} | {record.get('source_system', 'public-web')} | {record['result'].get('fit_score', 0)} | "
            f"{record['result'].get('disposition', 'review')} | {record['result'].get('next_action', '')} |"
        )
    lines.extend(["", "## Evidence and gaps", ""])
    for record in records:
        result = record["result"]
        lines.extend([
            f"### {record['company']}",
            f"- Evidence: {', '.join(result.get('evidence', [])) or 'None returned'}",
            f"- Gaps: {', '.join(result.get('gaps', [])) or 'None returned'}",
            f"- Recommended message angle: {result.get('message_angle', '') or 'None; keep in research.'}",
            "",
        ])
    lines.extend([
        "## Approval boundary",
        "",
        "- [ ] Confirm the company evidence and source URLs.",
        "- [ ] Confirm fit and next action before any contact research.",
        "- [ ] Obtain separate approval for any recipient, message, channel, or send date.",
    ])
    return "\n".join(lines) + "\n"


def run(candidates_path: Path, config_path: Path, db_path: Path, output_path: Path, *, mock: bool = False) -> dict[str, Any]:
    config = workbench.load_config(config_path)
    run_id = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    records: list[dict[str, Any]] = []
    for candidate in load_candidates(candidates_path):
        result = qualify_candidate(candidate, config, mock=mock)
        save_lead(db_path, candidate, result, run_id)
        records.append({"company": candidate.get("company", ""), "segment": candidate.get("segment", ""), "source_system": candidate.get("source_system", "public-web"), "result": result})
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(render_report(records, run_id), encoding="utf-8")
    return {"run_id": run_id, "records": len(records), "output": str(output_path), "db": str(db_path)}


def main() -> int:
    parser = argparse.ArgumentParser(description="Qualify supplied company-level CovePM candidates locally")
    parser.add_argument("--candidates", type=Path)
    parser.add_argument("--config", type=Path, default=ROOT / "config.json")
    parser.add_argument("--db", type=Path, default=DEFAULT_DB)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--queue", action="store_true", help="Render the current approval-gated lead queue")
    parser.add_argument("--lead-id", help="Company|website identifier for a lead action")
    parser.add_argument("--reviewer", help="Human reviewer for a lead action")
    parser.add_argument("--resolve-gaps", action="store_true")
    parser.add_argument("--approve", action="store_true")
    parser.add_argument("--action", help="Approved next action")
    parser.add_argument("--outcome", choices=["advance", "nurture", "disqualify", "unknown"])
    parser.add_argument("--note", default="", help="Outcome note")
    parser.add_argument("--on", help="ISO date for a review or outcome")
    parser.add_argument("--mock", action="store_true")
    args = parser.parse_args()
    if args.queue:
        print(render_queue(args.db))
        return 0
    if args.lead_id and args.resolve_gaps:
        if not args.reviewer:
            parser.error("--reviewer is required with --resolve-gaps")
        print(json.dumps(resolve_lead_gaps(args.db, args.lead_id, args.reviewer).to_dict(), indent=2))
        return 0
    if args.lead_id and args.approve:
        if not args.reviewer:
            parser.error("--reviewer is required with --approve")
        print(json.dumps(approve_lead(args.db, args.lead_id, args.reviewer, args.action, args.on).to_dict(), indent=2))
        return 0
    if args.lead_id and args.outcome:
        print(json.dumps(record_lead_outcome(args.db, args.lead_id, args.outcome, args.note, args.on).to_dict(), indent=2))
        return 0
    if not args.candidates:
        parser.error("--candidates is required unless using --queue or a lead action")
    print(json.dumps(run(args.candidates, args.config, args.db, args.output, mock=args.mock), indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
