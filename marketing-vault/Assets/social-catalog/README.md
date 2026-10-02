# Social media asset catalog

Averion Compass · Averion Software · Created September 30, 2026

Every social post must include a relevant image. Start with [the visual catalog](CATALOG.md) or [catalog.json](catalog.json). Paths in the manifest are relative to this folder. The images are saved locally alongside the catalog. Asset readiness does not authorize publishing: keep the existing individual post approval workflow.

## Choose an image

1. Match the post's topic to `tags` and `suggested_use`. Filter to `status: ready` for routine selection.
2. Prefer an actual application screenshot for a specific interface claim. Generated people and settings illustrate an operating situation; they do not prove product behavior or customer results.
3. Alternate office, field, resident, and product imagery. Do not repeat an asset in consecutive posts; prefer at least 14 days between uses. Check the usage ledger and queued drafts before choosing.
4. Attach the actual local image file, using the publishing connector's media upload workflow. A local path is not a public media URL. Never schedule a post with an empty or failed attachment.
5. Include `asset_id`, `asset_path`, `alt_text`, `visual_type`, and any required illustration disclosure in the draft artifact. Show the exact image in the approval preview. Replacing the image after approval requires review of the new preview.
6. Log reserved, scheduled, and published usage in [usage.csv](usage.csv), preserving older entries. Confirm the platform accepted the media before marking a post scheduled.

## Image treatment

- Generated people are fictional. Describe these scenes as illustrative; do not name them as employees, residents, customers, or endorsers. Use “Illustrative scene created with AI” in the caption for generated scenes. Alt text should also identify the illustration.
- The product-in-context image shows the public Compass marketing webpage. It is not an application screenshot or evidence of tablet/mobile app functionality.
- Preserve authentic screenshots as supplied. Do not regenerate UI text, metrics, charts, or controls. A future crop or redaction must be a separate derivative with its source recorded and reviewed.
- Full-page website captures are reference images; prepare an appropriate reviewed crop before using them in a feed. They are intentionally excluded from routine `ready` selection.
- The dashboard reference has an unknown original capture date and named records. It remains on hold until product confirms current UI and that every visible record is synthetic or cleared for marketing.
- Generated square assets can be used as supplied. Do not stretch them. Review any platform crop at preview size; keep faces, hands, and device content intact.
- No image implies guaranteed savings, faster repairs, actual customer outcomes, or capabilities absent from approved product facts.

## Maintenance

The posting agent owns selection and usage logging. The marketing owner reviews rotation weekly. The product owner reviews screenshot freshness before feature posts and after visible product changes. New assets need a stable ID, file, dimensions, source, generation prompt where applicable, tags, alt text, status, restrictions, and visual review. Never overwrite an existing ID's image; issue a new version.

The initial collection uses existing vault screenshots and the built-in image generation tool. Exact prompts are in [prompts.json](prompts.json). Live website retrieval failed on September 30, so website captures retain their documented September 24 date rather than being described as newly verified.
