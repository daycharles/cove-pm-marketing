we# Live Provider Activation Plan

Date: 2026-09-20  
Status: HubSpot connected; Zoho Mail bridge connected; Google Calendar authenticated in Chrome; hosted approval inbox live

## Provider selection

The first live stack is:

- **Zoho Mail** — inbound mailbox and approved outbound email for the Averion Compass domain.
- **Google Calendar** — availability and demo-event creation.
- **HubSpot** — company/lead records and approved activity logging.
- **Manual social scheduler** — social publishing remains manual until a specific provider is selected.

Outlook Calendar and Zoho CRM remain alternatives if the operating account uses Microsoft or Zoho.

## Activation gates

Before changing a connector from `manual` to `live`:

- confirm the account owner and intended workspace;
- connect the provider and verify the connection state;
- inspect requested scopes and remove anything unnecessary;
- confirm suppression/opt-out handling for email;
- confirm human approval immediately before send, publish, CRM mutation, or calendar creation;
- run a read-only smoke test;
- run one dry-run action with a stable idempotency key;
- perform one approved test action using a controlled internal record;
- verify the audit event and rollback/repair path.

## Current state

The local workbench remains intentionally approval-gated. The hosted Marketing OS now provides the
phone-friendly approval inbox and execution queue; it does not auto-send or auto-publish merely
because a draft was generated.

HubSpot itself is now authenticated and configured for the Averion Software workspace. Its
onboarding flow is set to **Generate leads**, kept **Zoho Mail** and **Zoho CRM** as the selected
tools, removed unrelated tools, and created the **Lead Pipeline Overview** dashboard. The dashboard
is available at:

`https://app-na2.hubspot.com/reports-dashboard/247461247/view/143471781`

This confirms the HubSpot workspace setup. A narrowly scoped `Averion Compass Marketing OS Service Key`
has also been created with `crm.objects.companies.read` and `crm.objects.companies.write`.
The local workbench is now connected and has passed a read-only company query against portal
`247461247`; CRM writes remain approval-gated.

The one-time local handoff was completed in the user-scoped environment (the secret is not stored
in this repository). For a fresh machine, run in PowerShell after copying the service key from
HubSpot:

```powershell
$env:COVE_HUBSPOT_PORTAL_ID = "247461247"
$env:COVE_HUBSPOT_PRIVATE_APP_TOKEN = "<paste locally; never paste into chat>"
```

For persistence, store the same values in the user's secret manager or user-scoped environment,
never in the repository, Markdown notes, SQLite, or shell history. The browser automation surface
cannot transfer the credential into the local PowerShell clipboard without exposing it.

The approved Zoho Mail bridge is connected to `info@averionsoftware.com`; inbound mailbox access
and drafting are available through the bridge. Approved outreach now enters the hosted execution
queue, where the user must explicitly confirm **Send via Zoho** before the provider call runs.
The local connector now models this as `zoho-mail-bridge` / `bridge`, so approved drafts are
queued for bridge review rather than sent directly by SMTP. No direct Zoho password is required
by the workbench.

For direct SMTP fallback only, a paid Zoho organization using a domain-based address uses the documented SMTP endpoint
`smtppro.zoho.com:465` over SSL (or port 587 with TLS), and the documented IMAP endpoint is
`imappro.zoho.com:993` over SSL. Zoho notes that IMAP must be enabled and that two-factor accounts
may require an application-specific password. Verify the exact datacenter settings in the Zoho
account before enabling the adapter. Network reachability to both documented endpoints has been
validated from the workbench environment. Direct SMTP remains disabled because the bridge is the
approved outbound path.

Google Calendar is authenticated in the Charles Day Chrome session as `daycharles@gmail.com`.
No native Calendar connector is installed for the local runner, so the current safe adapter is
`google-calendar-ui` in manual mode: availability may be inspected in the authenticated session,
but event creation remains human-approved and is not executed by the local runner.

## Daily phone-monitoring workflow

1. Open the private Marketing OS URL on the phone at the start of the day.
2. Leave the Approval inbox open; it refreshes automatically every minute and refreshes again when the tab becomes active.
3. Review new email and social drafts by opening their details.
4. Use **Changes** when copy or targeting needs revision; it requires confirmation and does not send anything.
5. Use **Approve** to move work into the controlled execution queue.
6. For approved outreach, use **Send via Zoho** only after the final recipient and message check.
7. Check for `Sent`, `Failed`, or `Execution adapter pending` states before leaving the queue.
8. Calendar/demo creation remains a separate manual confirmation in the authenticated Google Calendar session.

## Next activation sequence

1. Connect Gmail, Google Calendar, and HubSpot.
2. Confirm the connected account identities.
3. Verify read-only access first.
4. Test inbound Gmail classification.
5. Test Calendar availability read.
6. Test HubSpot company lookup/read.
7. Enable one approved action at a time.
8. Keep social manual until a provider and account are selected.
