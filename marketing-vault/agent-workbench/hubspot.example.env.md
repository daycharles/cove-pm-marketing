# HubSpot API configuration

The local adapter uses a HubSpot private-app access token only when explicitly configured.

```powershell
$env:COVE_HUBSPOT_PORTAL_ID = "247461247"
$env:COVE_HUBSPOT_PRIVATE_APP_TOKEN = "<store outside the repository>"
```

Required private-app scopes for the current adapter:

- `crm.objects.companies.read`
- `crm.objects.companies.write` (only for approved company upserts)
- `crm.objects.notes.write` (reserved for approved activity logging)

Start with read-only health checks:

```powershell
python hubspot_api.py status
python hubspot_api.py health
python hubspot_api.py search-company --domain example.com
```

Never commit the token or put it in a task, Markdown note, SQLite record, or shell history.
