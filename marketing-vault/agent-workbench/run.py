"""Minimal local-first CovePM marketing workflow.

One task in, one Ollama call, deterministic QA, one Markdown artifact out.
No network calls are made except the local Ollama endpoint.
"""

from __future__ import annotations

import argparse
import json
import re
import sqlite3
import sys
import urllib.error
import urllib.request
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parent
DEFAULT_CONFIG = ROOT / "config.json"


@dataclass
class Task:
    path: Path
    objective: str
    audience: str
    approval_required: bool
    instructions: str
    market_research: bool = False


def now_iso() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat()


def slugify(value: str) -> str:
    slug = re.sub(r"[^a-z0-9]+", "-", value.lower()).strip("-")
    return slug[:80] or "marketing-task"


def parse_frontmatter(text: str) -> tuple[dict[str, str], str]:
    if not text.startswith("---"):
        return {}, text.strip()
    parts = text.split("---", 2)
    if len(parts) != 3:
        return {}, text.strip()
    fields: dict[str, str] = {}
    for line in parts[1].splitlines():
        if ":" not in line:
            continue
        key, value = line.split(":", 1)
        fields[key.strip()] = value.strip().strip('"\'')
    return fields, parts[2].strip()


def load_task(path: Path) -> Task:
    raw = path.read_text(encoding="utf-8")
    fields, instructions = parse_frontmatter(raw)
    objective = fields.get("objective", path.stem.replace("-", " "))
    audience = fields.get("audience", "CovePM marketing audience")
    approval = fields.get("approval_required", "true").lower() not in {"false", "no", "0"}
    market_research = fields.get("market_research", "false").lower() in {"true", "yes", "1"}
    return Task(path, objective, audience, approval, instructions, market_research)


def load_config(path: Path) -> dict[str, Any]:
    if not path.exists():
        example = ROOT / "config.example.json"
        return json.loads(example.read_text(encoding="utf-8"))
    return json.loads(path.read_text(encoding="utf-8"))


def resolve_config_path(value: str) -> Path:
    path = Path(value)
    return path if path.is_absolute() else (ROOT / path).resolve()


def load_context(config: dict[str, Any]) -> list[tuple[str, str]]:
    context: list[tuple[str, str]] = []
    for item in config.get("context_files", []):
        path = resolve_config_path(item)
        if path.exists():
            context.append((str(path), path.read_text(encoding="utf-8")))
    return context


def review_website(config: dict[str, Any]) -> dict[str, Any]:
    """Run a cheap, deterministic evidence audit before spending model tokens."""
    site_path = resolve_config_path(str(config.get("website_path", "../index.html")))
    if not site_path.exists():
        return {"site": str(site_path), "status": "not-found", "strengths": [], "gaps": ["Website file was not found."], "recommendations": []}
    raw = site_path.read_text(encoding="utf-8")
    text = re.sub(r"<script.*?</script>|<style.*?</style>", " ", raw, flags=re.IGNORECASE | re.DOTALL)
    text = re.sub(r"<[^>]+>", " ", text)
    text = re.sub(r"\s+", " ", text).strip()
    lowered = text.lower()
    gaps: list[str] = []
    recommendations: list[str] = []
    checks = [
        ("hard-coded dashboard metrics", ["↓ 18%", "open work", "needs attention"], "Replace illustrative metrics and customer-like labels with clearly marked workflow examples or measured pilot results."),
        ("unverified pricing", ["$149", "$2.50", "clear pricing"], "Remove or verify public pricing before publishing; use a scoped pilot CTA until pricing and packaging are approved."),
        ("native mobile implication", ["mobile apps", "cove in your pocket"], "Describe the verified responsive web experience unless native mobile release evidence exists."),
        ("unqualified autopilot claims", ["autopilot connects", "asks before it acts"], "Tie automation language to shipped capabilities and show the approval/audit boundary explicitly."),
    ]
    findings: list[dict[str, str]] = []
    for label, phrases, recommendation in checks:
        matched = [phrase for phrase in phrases if phrase.lower() in lowered]
        if matched:
            gaps.append(f"{label}: matched {', '.join(matched)}.")
            recommendations.append(recommendation)
            findings.append({"area": label, "matches": ", ".join(matched), "recommendation": recommendation})
    strengths = []
    for phrase, strength in [
        ("maintenance", "Maintenance is a clear initial wedge."),
        ("resident", "Resident communication is part of the story."),
        ("audit", "The page attempts to communicate evidence and control."),
    ]:
        if phrase in lowered:
            strengths.append(strength)
    if not gaps:
        gaps.append("No deterministic website gaps matched; perform human product review before publishing.")
    return {"site": str(site_path), "status": "reviewed", "strengths": strengths, "gaps": gaps, "findings": findings, "recommendations": recommendations}


def make_prompt(task: Task, context: list[tuple[str, str]], research: dict[str, Any] | None = None) -> str:
    source_text = "\n\n".join(f"SOURCE: {name}\n{body}" for name, body in context)
    research_text = json.dumps(research or {}, indent=2)
    return f"""You are the local CovePM marketing workbench.

Create one evidence-aware draft from the task and approved context below.
Return JSON only with this exact shape:
{{
  "brief": "one short paragraph",
  "draft": "the proposed asset in plain Markdown",
  "claims": [{{"claim": "...", "source": "source filename or none", "status": "supported|needs_review"}}],
  "questions": ["missing facts or review questions"],
  "approval_required": true
}}

Rules:
- Do not invent numbers, customers, integrations, certifications, pricing, guarantees, or outcomes.
- Do not add numerical targets, prices, durations, percentage reductions, benchmarks, or customer results unless the exact value appears in APPROVED CONTEXT. If proof is missing, write it as a question or measurement plan.
- Only name a source when the source text directly supports the claim; otherwise use source "none" and status "needs_review".
- Use the requested audience and the CovePM maintenance-speed positioning.
- Include either "Book a demo" or "Start a pilot" as the CTA.
- If a claim is not directly supported by the sources, mark it needs_review.
- Keep the draft under 180 words, return no more than 5 claims and 5 questions, and keep the JSON complete.

TASK OBJECTIVE: {task.objective}
AUDIENCE: {task.audience}
TASK INSTRUCTIONS:
{task.instructions}

APPROVED CONTEXT:
{source_text}

PREVIOUS RESEARCH FINDINGS:
{research_text}
"""


def make_market_research_prompt(task: Task, context: list[tuple[str, str]]) -> str:
    source_text = "\n\n".join(f"SOURCE: {name}\n{body}" for name, body in context)
    return f"""You are CovePM's local market and competitor research agent.

Use only the approved local sources below. Do not claim current competitor features, market sizes,
benchmarks, prices, or trends unless the exact source supports them. If competitor sources are not
provided, say so clearly and return a research plan instead of guessing.

Return JSON only with this exact shape:
{{
  "decision": "the marketing decision this research should inform",
  "buyer_language": ["phrases supported by sources"],
  "competitor_findings": [{{"competitor_or_category": "...", "finding": "...", "source": "...", "status": "supported|needs_review"}}],
  "market_gaps": ["missing evidence or source gap"],
  "next_research": ["specific low-cost research action"]
}}

Task: {task.objective}
Audience: {task.audience}
Approved local sources:
{source_text}
"""


def mock_market_research(task: Task) -> dict[str, Any]:
    return {
        "decision": f"Choose the strongest evidence-led message for {task.audience}.",
        "buyer_language": ["time to assignment", "overdue work", "resident communication"],
        "competitor_findings": [],
        "market_gaps": ["No allowlisted competitor sources are available in the local vault."],
        "next_research": ["Add two or three official competitor product pages or public customer-language sources before making comparative claims."],
    }


def call_ollama(url: str, model: str, prompt: str, *, timeout: int = 600, num_predict: int = 512, think: bool = False) -> dict[str, Any]:
    for attempt in range(2):
        retry_note = "\nReturn only complete valid JSON. Do not truncate or wrap it in Markdown." if attempt else ""
        payload = {
            "model": model,
            "messages": [{"role": "user", "content": prompt + retry_note}],
            "stream": False,
            "format": "json",
            "think": think,
            "options": {"temperature": 0.2, "num_predict": num_predict},
        }
        request = urllib.request.Request(url, data=json.dumps(payload).encode("utf-8"), headers={"Content-Type": "application/json"}, method="POST")
        with urllib.request.urlopen(request, timeout=timeout) as response:
            body = json.loads(response.read().decode("utf-8"))
        content = body.get("message", {}).get("content", "")
        if not content:
            raise RuntimeError("Ollama returned no message content")
        try:
            return json.loads(content)
        except json.JSONDecodeError as exc:
            if attempt == 1:
                raise RuntimeError(f"Ollama returned invalid JSON after one retry: {exc}") from exc
    raise RuntimeError("Ollama response retry failed")


def mock_response(task: Task) -> dict[str, Any]:
    return {
        "brief": f"Draft a concise maintenance-focused section for {task.audience}, grounded in the local CovePM positioning.",
        "draft": "CovePM gives property teams one accountable workflow for maintenance: every request has an owner, next action, schedule, history, and resident-communication context.\n\n**Book a demo** to see the workflow.",
        "claims": [{"claim": "CovePM provides an accountable maintenance workflow.", "source": "Strategy/Positioning.md", "status": "supported"}],
        "questions": ["Which pilot metric should be shown as proof for this audience?"],
        "approval_required": True,
    }


def run_qa(result: dict[str, Any], task: Task, context: list[tuple[str, str]] | None = None) -> list[str]:
    failures: list[str] = []
    draft = str(result.get("draft", ""))
    claims = result.get("claims", [])
    approved_text = "\n".join(body for _, body in (context or [])).lower()
    approved_sources = {Path(name).name.lower() for name, _ in (context or [])}
    if not draft.strip():
        failures.append("Draft is empty.")
    if not re.search(r"book a demo|start a pilot", draft, re.IGNORECASE):
        failures.append("Draft is missing the required CTA: Book a demo or Start a pilot.")
    if not isinstance(claims, list) or not claims:
        failures.append("No claims/source review list was returned.")
    if any(str(item.get("status", "")).lower() == "needs_review" for item in claims if isinstance(item, dict)):
        failures.append("One or more claims need evidence review.")
    if context:
        for item in claims if isinstance(claims, list) else []:
            if not isinstance(item, dict):
                continue
            source = str(item.get("source", "none")).strip().lower()
            if source not in {"", "none", "unknown"} and Path(source).name not in approved_sources:
                failures.append(f"Claim cites a source outside approved context: {source}.")
        claim_text = json.dumps(claims)
        numeric_tokens = re.findall(r"(?<![A-Za-z])(?:\$\s?\d[\d,.]*|\d[\d,.]*\s?%|\d[\d,.]*\s?(?:day|days|hour|hours|month|months|year|years)|\d[\d,.]*\+)", draft + " " + claim_text, flags=re.IGNORECASE)
        for token in numeric_tokens:
            if token.lower().replace(" ", "") not in approved_text.replace(" ", ""):
                failures.append(f"Draft contains an unsupported numeric claim or target: {token}.")
    banned = ["guaranteed", "guarantee", "50% reduction", "save 30%", "price lock", "discount"]
    for phrase in banned:
        if phrase in draft.lower():
            failures.append(f"Draft contains gated or unsupported language: {phrase}.")
    if task.approval_required and not result.get("approval_required", True):
        failures.append("Task requires approval but the model marked approval as unnecessary.")
    return failures


def run_research_qa(research: dict[str, Any], context: list[tuple[str, str]]) -> list[str]:
    failures: list[str] = []
    market = research.get("market", {}) if isinstance(research, dict) else {}
    approved_text = "\n".join(body for _, body in context).lower().replace(" ", "")
    approved_sources = {Path(name).name.lower() for name, _ in context}
    for finding in market.get("competitor_findings", []) if isinstance(market, dict) else []:
        if not isinstance(finding, dict):
            continue
        source = str(finding.get("source", "none")).strip().lower()
        if source not in {"", "none", "unknown"} and Path(source).name not in approved_sources:
            failures.append(f"Research cites a source outside approved context: {source}.")
        numeric_tokens = re.findall(r"(?<![A-Za-z])(?:\$\s?\d[\d,.]*|\d[\d,.]*\s?%|\d[\d,.]*\s?(?:day|days|hour|hours|month|months|year|years)|\d[\d,.]*\+)", json.dumps(finding), flags=re.IGNORECASE)
        for token in numeric_tokens:
            if token.lower().replace(" ", "") not in approved_text:
                failures.append(f"Research contains an unsupported market number: {token}.")
    return failures


def init_db(path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    db = sqlite3.connect(path)
    try:
        db.execute("""CREATE TABLE IF NOT EXISTS runs (
            run_id TEXT PRIMARY KEY,
            started_at TEXT NOT NULL,
            finished_at TEXT,
            task_path TEXT NOT NULL,
            model TEXT NOT NULL,
            status TEXT NOT NULL,
            output_path TEXT,
            qa_json TEXT,
            error TEXT
        )""")
        db.commit()
    finally:
        db.close()


def save_run(db_path: Path, values: tuple[Any, ...]) -> None:
    db = sqlite3.connect(db_path)
    try:
        db.execute("INSERT OR REPLACE INTO runs VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)", values)
        db.commit()
    finally:
        db.close()


def render_output(task: Task, result: dict[str, Any], qa: list[str], run_id: str, model: str, research: dict[str, Any] | None = None) -> str:
    status = "needs-review" if qa else "ready-for-human-review"
    claims = result.get("claims", [])
    questions = result.get("questions", [])
    claim_lines = []
    for item in claims if isinstance(claims, list) else []:
        if isinstance(item, dict):
            claim_lines.append(f"- {item.get('claim', '')} — source: {item.get('source', 'none')} — status: {item.get('status', 'needs_review')}")
    question_lines = [f"- {q}" for q in questions] if isinstance(questions, list) else []
    qa_lines = [f"- {item}" for item in qa] or ["- Passed deterministic checks."]
    research_text = "- No pre-draft research was run."
    if research:
        website = research.get("website", research)
        market = research.get("market", {})
        gaps = website.get("gaps", [])
        recommendations = website.get("recommendations", [])
        research_text = f"**Audited source:** `{website.get('site', 'local sources')}`\n\n**Website strengths**\n" + "\n".join(f"- {item}" for item in website.get("strengths", []))
        research_text += "\n\n**Website gaps**\n" + "\n".join(f"- {item}" for item in gaps)
        research_text += "\n\n**Website recommendations**\n" + "\n".join(f"- {item}" for item in recommendations)
        research_text += "\n\n**Market/competitor findings**\n" + "\n".join(f"- {item.get('competitor_or_category')}: {item.get('finding')} — source: {item.get('source', 'none')} — status: {item.get('status', 'needs_review')}" for item in market.get("competitor_findings", []))
        research_text += "\n\n**Market gaps and next research**\n" + "\n".join(f"- {item}" for item in market.get("market_gaps", []) + market.get("next_research", []))
    return f"""# AI Draft - {task.objective}

---
type: ai-draft
status: {status}
run_id: {run_id}
model: {model}
task: {task.path.as_posix()}
approval_required: true
---

## Research findings

{research_text}

## Brief

{result.get('brief', '')}

## Draft

{result.get('draft', '')}

## Claims and sources

{chr(10).join(claim_lines) or '- No claims returned.'}

## Questions and gaps

{chr(10).join(question_lines) or '- None returned.'}

## Deterministic QA

{chr(10).join(qa_lines)}

## Human approval

- [ ] Review the claims and sources.
- [ ] Confirm the draft does not add unsupported product, pricing, integration, or outcome claims.
- [ ] Approve before publishing, outreach, pricing, or commitments.
"""


def main() -> int:
    parser = argparse.ArgumentParser(description="Run the minimal local CovePM marketing workflow")
    parser.add_argument("--task", type=Path, required=True)
    parser.add_argument("--config", type=Path, default=DEFAULT_CONFIG)
    parser.add_argument("--mock", action="store_true", help="Skip Ollama and produce a deterministic test result")
    args = parser.parse_args()

    task_path = args.task if args.task.is_absolute() else (ROOT / args.task).resolve()
    config = load_config(args.config if args.config.is_absolute() else (ROOT / args.config).resolve())
    task = load_task(task_path)
    context = load_context(config)
    run_id = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    started = now_iso()
    model = str(config.get("model", "qwen3:4b"))
    output_dir = resolve_config_path(str(config.get("output_dir", "outputs")))
    db_path = resolve_config_path(str(config.get("data_dir", "data"))) / "runs.sqlite3"
    init_db(db_path)

    try:
        result = mock_response(task) if args.mock else call_ollama(
            str(config["ollama_url"]),
            model,
            make_prompt(task, context),
            timeout=int(config.get("ollama_timeout_seconds", 600)),
            num_predict=int(config.get("ollama_num_predict", 256)),
            think=bool(config.get("ollama_think", False)),
        )
        qa = run_qa(result, task, context)
        output_dir.mkdir(parents=True, exist_ok=True)
        output_path = output_dir / f"{run_id}-{slugify(task.objective)}.md"
        output_path.write_text(render_output(task, result, qa, run_id, model), encoding="utf-8")
        status = "needs-review" if qa else "ready-for-human-review"
        save_run(db_path, (run_id, started, now_iso(), str(task.path), model, status, str(output_path), json.dumps(qa), None))
        print(json.dumps({"run_id": run_id, "status": status, "output": str(output_path), "qa": qa}, indent=2))
        return 0
    except (OSError, urllib.error.URLError, json.JSONDecodeError, RuntimeError, KeyError) as exc:
        save_run(db_path, (run_id, started, now_iso(), str(task.path), model, "error", None, "[]", str(exc)))
        print(f"Workflow failed: {exc}", file=sys.stderr)
        print("Run with --mock to verify the local pipeline, or confirm Ollama is running and the model is installed.", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
