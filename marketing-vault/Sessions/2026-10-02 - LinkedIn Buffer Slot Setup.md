---
type: marketing-session
date: 2026-10-02
status: partial-setup-complete
segment: LinkedIn daily publishing pilot
objective: restore the single daily Buffer posting slot and identify remaining automation gates
approval_required: false
---

# 2026-10-02 - LinkedIn Buffer Slot Setup

## Completed

- Signed into the existing Buffer account using the saved vault login; confirmed the Averion Software LinkedIn Company Page is connected.
- Buffer home showed zero scheduled posts. Channel ID observed in Buffer: `6abffd55ea19ca0bde57f126`.
- Restored one daily 9:00 AM posting time in the channel's New York timezone. Readback showed Sunday through Saturday enabled at 09:00 AM; this is one slot per local calendar day, not a post or an automated runner.
- Updated [[Plans/LinkedIn Daily Publishing Pilot - 2026-10-02]] and [[Dashboard]].

## Still blocked

- Buffer API key has not been created. Buffer's current API docs direct users to Settings → API and recommend storing the key in an environment variable. The key is account-wide, so keep it private and out of vault notes/chat.
- The owner selected the existing GitHub Pages deployment on the public repository for image hosting and GitHub Actions as the intended runner. Updated `.github/workflows/pages.yml` to stage the reviewed AC-G005 image at `/assets/social/AC-G005-unit-review.png` under the configured `covepm.averionsoftware.com` host. Local staging verification confirmed the image is a 1024 × 1024 PNG and the workflow copy target exists. The URL is not live yet; no commit or deployment was made.
- The exact draft `hermes-linkedin-signoff-20261002` remains unscheduled. Its uniqueness check still needs comparison against the actual LinkedIn/Buffer history, including the September 29 topic noted in the content ledger.
- Fail-closed selection and Buffer runner logic now exists in `agent-workbench/linkedin_buffer_runner.py`, with an empty manifest until exact content passes independent QA. A manual-only owner-restricted GitHub Actions workflow defaults to read-only and uploads a 90-day audit artifact. The targeted unit tests pass. No recurring schedule is enabled, and the live Buffer API path has not been exercised.

## Next action

Then deploy and verify the public image URL, and have the owner provision a Buffer personal API key directly as a GitHub Actions secret named `BUFFER_API_KEY` (never paste it into chat or commit it). Buffer documents that a personal key can access all organizations and channels on the account, so review that account-wide scope first. After the exact post passes current-history QA, dispatch the owner-only action in read-only mode; only after authenticated media/queue readback succeeds should a scheduling test and daily trigger be considered. Keep the recurring schedule disabled until then.