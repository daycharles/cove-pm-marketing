we# Live Provider Activation Plan

Date: 2026-09-20  
Status: awaiting account connections

## Provider selection

The first live stack is:

- **Zoho Mail** — inbound mailbox and approved outbound email for the CovePM domain.
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

The provider plugins are available but not connected in this workspace. No credentials or provider
environment variables were found locally. The live example config therefore intentionally fails
validation until the connections are confirmed.

HubSpot itself is now authenticated and configured for the Averion Software workspace. Its
onboarding flow is set to **Generate leads**, kept **Zoho Mail** and **Zoho CRM** as the selected
tools, removed unrelated tools, and created the **Lead Pipeline Overview** dashboard. The dashboard
is available at:

`https://app-na2.hubspot.com/reports-dashboard/247461247/view/143471781`

This confirms the HubSpot workspace setup, but it does not yet connect the local workbench to the
HubSpot API. That connection still requires the HubSpot app connector or an explicitly created
HubSpot private app with narrowly scoped credentials.

For a paid Zoho organization using a domain-based address, the documented SMTP endpoint is
`smtppro.zoho.com:465` over SSL (or port 587 with TLS), and the documented IMAP endpoint is
`imappro.zoho.com:993` over SSL. Zoho notes that IMAP must be enabled and that two-factor accounts
may require an application-specific password. Verify the exact datacenter settings in the Zoho
account before enabling the adapter.

## Next activation sequence

1. Connect Gmail, Google Calendar, and HubSpot.
2. Confirm the connected account identities.
3. Verify read-only access first.
4. Test inbound Gmail classification.
5. Test Calendar availability read.
6. Test HubSpot company lookup/read.
7. Enable one approved action at a time.
8. Keep social manual until a provider and account are selected.
