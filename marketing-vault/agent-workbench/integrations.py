"""Approval-gated connector boundary for external marketing actions.

The default implementation is dry-run/manual. Provider-specific live connectors can be added
behind the same interface once credentials, scopes, suppression rules, and account ownership are
approved.
"""

from __future__ import annotations

import argparse
import json
import os
import sqlite3
import smtplib
import ssl
from contextlib import closing
from dataclasses import dataclass
from email.message import EmailMessage
from pathlib import Path
from typing import Any, Protocol

from pilot_metrics import record_event


ROOT = Path(__file__).resolve().parent
DEFAULT_CONFIG = ROOT / "integrations.example.json"
DEFAULT_DB = ROOT / "data" / "runs.sqlite3"
MODES = {"dry-run", "manual", "bridge", "live"}


@dataclass(frozen=True)
class ConnectorResult:
    connector: str
    action: str
    status: str
    idempotency_key: str
    details: dict[str, Any]

    def to_dict(self) -> dict[str, Any]:
        return {
            "connector": self.connector,
            "action": self.action,
            "status": self.status,
            "idempotency_key": self.idempotency_key,
            "details": self.details,
        }


class Connector(Protocol):
    name: str

    def execute(self, action: str, payload: dict[str, Any], idempotency_key: str, approved: bool) -> ConnectorResult:
        ...


@dataclass
class ConfiguredConnector:
    name: str
    mode: str = "manual"
    account: str = ""
    scopes: list[str] | None = None
    provider: str = "manual"
    host: str = ""
    port: int = 0
    username_env: str = ""
    password_env: str = ""
    from_address: str = ""

    def validate(self) -> list[str]:
        errors: list[str] = []
        if self.mode not in MODES:
            errors.append(f"{self.name}: mode must be one of {', '.join(sorted(MODES))}")
        if self.mode != "dry-run" and not self.account.strip():
            errors.append(f"{self.name}: account is required outside dry-run mode")
        if self.mode == "live" and not (self.name == "email" and self.provider == "zoho-mail"):
            errors.append(f"{self.name}: live provider adapter is not implemented")
        if self.mode == "bridge" and not (self.name == "email" and self.provider == "zoho-mail-bridge"):
            errors.append(f"{self.name}: bridge mode is only implemented for the Zoho Mail approval bridge")
        if self.name == "email" and self.provider == "zoho-mail" and self.mode == "live":
            if not self.host or not self.port or not self.username_env or not self.password_env or not self.from_address:
                errors.append("email: Zoho SMTP host, port, credential env names, and from_address are required")
            elif not os.getenv(self.username_env) or not os.getenv(self.password_env):
                errors.append("email: Zoho credential environment variables are not configured")
        return errors

    def execute(self, action: str, payload: dict[str, Any], idempotency_key: str, approved: bool) -> ConnectorResult:
        if not approved:
            raise PermissionError(f"{self.name} action requires explicit approval")
        errors = self.validate()
        if errors:
            raise ValueError("; ".join(errors))
        if self.name == "email" and self.provider == "zoho-mail" and self.mode == "live":
            message = EmailMessage()
            message["From"] = self.from_address
            message["To"] = payload["to"]
            message["Subject"] = payload["subject"]
            message.set_content(payload["body"])
            context = ssl.create_default_context()
            with smtplib.SMTP_SSL(self.host, self.port, context=context, timeout=30) as smtp:
                smtp.login(os.environ[self.username_env], os.environ[self.password_env])
                smtp.send_message(message)
            return ConnectorResult(self.name, action, "sent", idempotency_key, {"provider": self.provider, "to": payload["to"], "subject": payload["subject"]})
        if self.name == "email" and self.provider == "zoho-mail-bridge" and self.mode == "bridge":
            return ConnectorResult(
                self.name,
                action,
                "queued-for-approval",
                idempotency_key,
                {"provider": self.provider, "account": self.account, "to": payload["to"], "subject": payload["subject"]},
            )
        status = "dry-run" if self.mode == "dry-run" else "manual-review-required"
        return ConnectorResult(self.name, action, status, idempotency_key, {"account": self.account, "payload": payload})


def load_connectors(path: Path) -> dict[str, ConfiguredConnector]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise ValueError("integration config must be a JSON object")
    connectors: dict[str, ConfiguredConnector] = {}
    for name in ("email", "social", "crm", "calendar"):
        raw = payload.get(name, {})
        if not isinstance(raw, dict):
            raise ValueError(f"{name} configuration must be an object")
        connectors[name] = ConfiguredConnector(
            name=name,
            mode=str(raw.get("mode", "manual")),
            provider=str(raw.get("provider", "manual")),
            account=str(raw.get("account", "")),
            scopes=list(raw.get("scopes", [])),
            host=str(raw.get("host", "")),
            port=int(raw.get("port", 0)),
            username_env=str(raw.get("username_env", "")),
            password_env=str(raw.get("password_env", "")),
            from_address=str(raw.get("from_address", "")),
        )
    return connectors


def validate_connectors(connectors: dict[str, ConfiguredConnector]) -> list[str]:
    errors: list[str] = []
    for connector in connectors.values():
        errors.extend(connector.validate())
    return errors


class IntegrationGateway:
    def __init__(self, db_path: Path, connectors: dict[str, ConfiguredConnector]):
        self.db_path = db_path
        self.connectors = connectors
        self._init_action_db()

    def _init_action_db(self) -> None:
        self.db_path.parent.mkdir(parents=True, exist_ok=True)
        with closing(sqlite3.connect(self.db_path)) as db:
            db.execute(
                """CREATE TABLE IF NOT EXISTS integration_actions (
                    idempotency_key TEXT PRIMARY KEY,
                    connector TEXT NOT NULL,
                    action TEXT NOT NULL,
                    result_json TEXT NOT NULL
                )"""
            )
            db.commit()

    def _execute(self, connector_name: str, action: str, payload: dict[str, Any], idempotency_key: str, approved: bool) -> ConnectorResult:
        if connector_name not in self.connectors:
            raise KeyError(f"Unknown connector: {connector_name}")
        if not idempotency_key.strip():
            raise ValueError("idempotency_key is required")
        with closing(sqlite3.connect(self.db_path)) as db:
            row = db.execute("SELECT result_json FROM integration_actions WHERE idempotency_key=?", (idempotency_key,)).fetchone()
        if row:
            previous = json.loads(row[0])
            return ConnectorResult(previous["connector"], previous["action"], "already-recorded", idempotency_key, previous["details"])
        result = self.connectors[connector_name].execute(action, payload, idempotency_key, approved)
        with closing(sqlite3.connect(self.db_path)) as db:
            db.execute(
                "INSERT INTO integration_actions (idempotency_key, connector, action, result_json) VALUES (?, ?, ?, ?)",
                (idempotency_key, connector_name, action, json.dumps(result.to_dict())),
            )
            db.commit()
        record_event(self.db_path, "integration_action", status=result.status, details=result.to_dict())
        return result

    def send_email(self, to: str, subject: str, body: str, idempotency_key: str, *, approved: bool, suppression_checked: bool) -> ConnectorResult:
        if not to.strip() or not subject.strip() or not body.strip():
            raise ValueError("to, subject, and body are required")
        if not suppression_checked:
            raise PermissionError("suppression/opt-out check is required before email")
        return self._execute("email", "send", {"to": to, "subject": subject, "body": body}, idempotency_key, approved)

    def publish_social(self, channel: str, text: str, idempotency_key: str, *, approved: bool) -> ConnectorResult:
        if not channel.strip() or not text.strip():
            raise ValueError("channel and text are required")
        return self._execute("social", "publish", {"channel": channel, "text": text}, idempotency_key, approved)

    def upsert_crm_lead(self, lead: dict[str, Any], idempotency_key: str, *, approved: bool) -> ConnectorResult:
        if not lead.get("company") or not lead.get("website"):
            raise ValueError("CRM lead requires company and website")
        return self._execute("crm", "upsert-lead", lead, idempotency_key, approved)

    def schedule_calendar(self, event: dict[str, Any], idempotency_key: str, *, approved: bool) -> ConnectorResult:
        for field in ("title", "starts_at", "attendee_label"):
            if not event.get(field):
                raise ValueError(f"calendar event requires {field}")
        return self._execute("calendar", "create-event", event, idempotency_key, approved)


def render_status(connectors: dict[str, ConfiguredConnector]) -> str:
    lines = ["# Integration Status", "", "| Connector | Provider | Mode | Account | Validation |", "|---|---|---|---|---|"]
    for name, connector in connectors.items():
        errors = connector.validate()
        status = "ready" if not errors else "; ".join(errors)
        lines.append(f"| {name} | {connector.provider} | {connector.mode} | {connector.account or '—'} | {status} |")
    lines.extend(["", "Live provider adapters are intentionally disabled until credentials, scopes, suppression, and account ownership are approved.", ""])
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description="Inspect and exercise CovePM external integration boundaries")
    parser.add_argument("command", choices=["status", "email", "social", "crm", "calendar"])
    parser.add_argument("--config", type=Path, default=DEFAULT_CONFIG)
    parser.add_argument("--db", type=Path, default=DEFAULT_DB)
    parser.add_argument("--idempotency-key", default="manual-test")
    parser.add_argument("--approved", action="store_true")
    parser.add_argument("--suppression-checked", action="store_true")
    parser.add_argument("--to", default="")
    parser.add_argument("--subject", default="")
    parser.add_argument("--body", default="")
    parser.add_argument("--channel", default="")
    parser.add_argument("--company", default="")
    parser.add_argument("--website", default="")
    parser.add_argument("--starts-at", default="")
    parser.add_argument("--attendee-label", default="")
    parser.add_argument("--title", default="")
    args = parser.parse_args()
    connectors = load_connectors(args.config)
    if args.command == "status":
        print(render_status(connectors))
        return 0
    gateway = IntegrationGateway(args.db, connectors)
    if args.command == "email":
        result = gateway.send_email(args.to, args.subject, args.body, args.idempotency_key, approved=args.approved, suppression_checked=args.suppression_checked)
    elif args.command == "social":
        result = gateway.publish_social(args.channel, args.body, args.idempotency_key, approved=args.approved)
    elif args.command == "crm":
        result = gateway.upsert_crm_lead({"company": args.company, "website": args.website}, args.idempotency_key, approved=args.approved)
    else:
        result = gateway.schedule_calendar({"title": args.title, "starts_at": args.starts_at, "attendee_label": args.attendee_label}, args.idempotency_key, approved=args.approved)
    print(json.dumps(result.to_dict(), indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
