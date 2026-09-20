# Local lead qualification

This folder is for supplied, company-level candidate research. The workflow is intentionally
company-first and approval-gated.

Each candidate should include:

- `company`;
- optional public `website`;
- `segment`;
- `evidence` describing a verifiable operational signal;
- `source_urls` containing the approved public sources; and
- optional `portfolio_context`.

Do not put personal contact details, private customer data, credentials, or scraped contact lists
here. The workflow produces fit scoring, qualification gaps, and a recommended next research
action. It does not send messages or discover personal contacts.

Example command:

```powershell
python lead_engine.py --candidates leads/candidates.json --mock
```

The result is written to `outputs/lead-qualification.md` and the structured record is stored in
`data/runs.sqlite3`.
