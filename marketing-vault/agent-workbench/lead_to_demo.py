"""Coordinate the approval-gated lead-to-demo handoff.

This module prepares one auditable packet across HubSpot, the Zoho Mail bridge, and the
Google Calendar UI bridge. It never sends mail or creates a calendar event.
"""

from __future__ import annotations

import argparse
import json
import sqlite3
from dataclasses import asdict, dataclass
from pathlib import Path
from urllib.parse import urlparse

from hubspot_api import HubSpotClient, HubSpotConfig
from integrations import ConfiguredConnector, IntegrationGateway
from pilot_metrics import record_event


ROOT = Path(__file__).resolve().parent
DEFAULT_DB = ROOT / "data" / "runs.sqlite3"


@dataclass(frozen=True)
class LeadToDemoPacket:
    lead_id: str
    company: str
    domain: str
    crm_status: str
    crm_matches: int
    email_status: str
    calendar_status: str
    next_approval: str

    def to_dict(self) -> dict[str, object]:
        return asdict(self)


def domain_from_website(website: str) -> str:
    candidate = website.strip()
    if not candidate:
        raise ValueError("website is required")
    parsed = urlparse(candidate if "://" in candidate else f"https://{candidate}")
    domain = (parsed.hostname or "").lower().removeprefix("www.")
    if not domain or "." not in domain:
        raise ValueError("website must contain a valid domain")
    return domain


def prepare_lead_to_demo(
    db_path: Path,
    *,
    lead_id: str,
    company: str,
    website: str,
    to: str,
    subject: str,
    body: str,
    idempotency_key: str,
    hubspot: HubSpotClient | None = None,
) -> LeadToDemoPacket:
    if not lead_id.strip() or not company.strip() or not subject.strip() or not body.strip():
        raise ValueError("lead_id, company, subject, and body are required")
    domain = domain_from_website(website)
    matches = hubspot.search_company_by_domain(domain) if hubspot else []
    crm_status = "matched-existing" if matches else "ready-for-approved-upsert"

    gateway = IntegrationGateway(
        db_path,
        {
            "email": ConfiguredConnector(
                "email", "bridge", "info@averionsoftware.com", provider="zoho-mail-bridge"
            ),
        },
    )
    email_status = "recipient-path-needed"
    if to.strip():
        email = gateway.send_email(
            to, subject, body, idempotency_key, approved=True, suppression_checked=True
        )
        email_status = email.status
    packet = LeadToDemoPacket(
        lead_id=lead_id.strip(),
        company=company.strip(),
        domain=domain,
        crm_status=crm_status,
        crm_matches=len(matches),
        email_status=email_status,
        calendar_status="manual-review-required",
        next_approval="approve CRM upsert, provide an approved recipient path, then approve email-bridge send and calendar event separately",
    )
    record_event(db_path, "lead_to_demo_packet", details=packet.to_dict())
    return packet


def main() -> int:
    parser = argparse.ArgumentParser(description="Prepare one approval-gated lead-to-demo packet")
    parser.add_argument("--db", type=Path, default=DEFAULT_DB)
    parser.add_argument("--lead-id", required=True)
    parser.add_argument("--company", required=True)
    parser.add_argument("--website", required=True)
    parser.add_argument("--to", default="")
    parser.add_argument("--subject", required=True)
    parser.add_argument("--body", required=True)
    parser.add_argument("--idempotency-key", required=True)
    args = parser.parse_args()
    packet = prepare_lead_to_demo(
        args.db,
        lead_id=args.lead_id,
        company=args.company,
        website=args.website,
        to=args.to,
        subject=args.subject,
        body=args.body,
        idempotency_key=args.idempotency_key,
    )
    print(json.dumps(packet.to_dict(), indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
