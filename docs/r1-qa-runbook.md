# R1 release-candidate QA runbook

Working source for #177.

1. Start from a clean database, apply migrations, configure the runtime role, and seed the
   Tidewater demo using `docs/RELEASE-TESTING.md`.
2. Record OS, browser, viewport, commit/build version, and database seed result.
3. Exercise login, work lifecycle, bulk actions, resident-message mocks, technician On The Way,
   assets/repeat-repair, attention, global search, and integration health.
4. Attach API, web, integration, and Playwright results to #177; record every failure with a
   reproduction step and severity.

