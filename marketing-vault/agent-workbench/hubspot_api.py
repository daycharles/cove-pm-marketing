"""Minimal, approval-gated HubSpot CRM API adapter for the CovePM workbench."""

from __future__ import annotations

import argparse
import json
import os
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from pilot_metrics import record_event


ROOT = Path(__file__).resolve().parent
DEFAULT_DB = ROOT / "data" / "runs.sqlite3"


@dataclass(frozen=True)
class HubSpotConfig:
    token_env: str = "COVE_HUBSPOT_PRIVATE_APP_TOKEN"
    portal_id: str = ""
    base_url: str = "https://api.hubapi.com"

    @classmethod
    def from_env(cls) -> "HubSpotConfig":
        return cls(
            token_env=os.getenv("COVE_HUBSPOT_TOKEN_ENV", cls.token_env),
            portal_id=os.getenv("COVE_HUBSPOT_PORTAL_ID", ""),
            base_url=os.getenv("COVE_HUBSPOT_BASE_URL", cls.base_url).rstrip("/"),
        )

    def token(self) -> str:
        return os.getenv(self.token_env, "")

    def validate(self) -> list[str]:
        errors: list[str] = []
        if not self.portal_id:
            errors.append("COVE_HUBSPOT_PORTAL_ID is not configured")
        if not self.token():
            errors.append(f"{self.token_env} is not configured")
        return errors


class HubSpotClient:
    def __init__(self, config: HubSpotConfig, db_path: Path = DEFAULT_DB):
        self.config = config
        self.db_path = db_path

    def _request(self, method: str, path: str, payload: dict[str, Any] | None = None) -> dict[str, Any]:
        errors = self.config.validate()
        if errors:
            raise RuntimeError("; ".join(errors))
        data = json.dumps(payload).encode("utf-8") if payload is not None else None
        request = urllib.request.Request(
            self.config.base_url + path,
            data=data,
            headers={"Authorization": f"Bearer {self.config.token()}", "Content-Type": "application/json"},
            method=method,
        )
        try:
            with urllib.request.urlopen(request, timeout=30) as response:
                return json.loads(response.read().decode("utf-8"))
        except urllib.error.HTTPError as exc:
            body = exc.read().decode("utf-8", errors="replace")
            raise RuntimeError(f"HubSpot API {exc.code}: {body[:500]}") from exc
        except urllib.error.URLError as exc:
            raise RuntimeError(f"HubSpot connection failed: {exc.reason}") from exc

    def health_check(self) -> dict[str, Any]:
        result = self._request("GET", "/crm/v3/objects/companies?limit=1&properties=name,domain,website")
        record_event(self.db_path, "hubspot_read", details={"operation": "health_check"})
        return {"status": "connected", "portal_id": self.config.portal_id, "sample_count": len(result.get("results", []))}

    def search_company_by_domain(self, domain: str) -> list[dict[str, Any]]:
        if not domain.strip():
            raise ValueError("domain is required")
        result = self._request(
            "POST",
            "/crm/v3/objects/companies/search",
            {
                "filterGroups": [{"filters": [{"propertyName": "domain", "operator": "EQ", "value": domain.strip()}]}],
                "properties": ["name", "domain", "website"],
                "limit": 10,
            },
        )
        record_event(self.db_path, "hubspot_read", details={"operation": "search_company_by_domain", "domain": domain.strip()})
        return list(result.get("results", []))

    def upsert_company(self, company: dict[str, str], *, approved: bool, idempotency_key: str) -> dict[str, Any]:
        if not approved:
            raise PermissionError("HubSpot company writes require explicit approval")
        if not idempotency_key.strip():
            raise ValueError("idempotency_key is required")
        name = company.get("name", "").strip()
        domain = company.get("domain", "").strip()
        if not name or not domain:
            raise ValueError("company name and domain are required")
        existing = self.search_company_by_domain(domain)
        properties = {"name": name, "domain": domain}
        if company.get("website"):
            properties["website"] = company["website"].strip()
        if existing:
            company_id = existing[0]["id"]
            result = self._request("PATCH", f"/crm/v3/objects/companies/{urllib.parse.quote(company_id)}", {"properties": properties})
            operation = "update_company"
        else:
            result = self._request("POST", "/crm/v3/objects/companies", {"properties": properties})
            operation = "create_company"
        record_event(self.db_path, "hubspot_write", details={"operation": operation, "idempotency_key": idempotency_key, "company_id": result.get("id")})
        return result


def main() -> int:
    parser = argparse.ArgumentParser(description="Run safe HubSpot CRM checks for CovePM")
    parser.add_argument("command", choices=["status", "health", "search-company"])
    parser.add_argument("--domain", default="")
    parser.add_argument("--db", type=Path, default=DEFAULT_DB)
    args = parser.parse_args()
    config = HubSpotConfig.from_env()
    if args.command == "status":
        print(json.dumps({"portal_id": config.portal_id, "token_configured": bool(config.token()), "validation": config.validate()}, indent=2))
        return 0
    client = HubSpotClient(config, args.db)
    if args.command == "health":
        print(json.dumps(client.health_check(), indent=2))
    else:
        if not args.domain:
            parser.error("--domain is required")
        print(json.dumps(client.search_company_by_domain(args.domain), indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
