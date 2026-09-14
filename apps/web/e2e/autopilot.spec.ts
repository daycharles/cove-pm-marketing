import { expect, test, type Locator, type Page } from "@playwright/test";

const password = process.env.PLAYWRIGHT_DEMO_PASSWORD ?? "DemoPassword!123";
const organizationSlug = "averion-demo";
const email = "demo-admin@averion.example.test";

// CPM-8.06: the Autopilot daily brief — trigger an analysis run, filter to the signal our fixture
// trips, open its evidence drawer, dismiss it, then reopen it from the Dismissed filter. The
// fixture reuses the same "critical, unassigned work item" attention.spec.ts creates: it trips
// AttentionReason.UnassignedEmergency, which WorkSignalRules.MapSignalType maps onto
// SignalTypes.SlaRisk (src/PropFlow.Domain/Autopilot/WorkSignalRules.cs), so a real Autopilot run
// against it is a real, non-mocked finding rather than a seeded fixture the assertions can't see
// through.
test("the Autopilot brief surfaces a run's finding, its evidence, and its decision controls", async ({
  page,
}) => {
  const runId = `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;

  await page.goto("/");
  await page.getByLabel("Organization slug").fill(organizationSlug);
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill(password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page.getByRole("heading", { name: "Today" })).toBeVisible();

  const title = `E2E autopilot ${runId} burst pipe`;
  const { workId } = await page.evaluate(
    async ({ title }) => {
      const json = async (path: string, init?: RequestInit) => {
        const response = await fetch(path, {
          credentials: "same-origin",
          cache: "no-store",
          ...init,
        });
        if (!response.ok) throw new Error(`${init?.method ?? "GET"} ${path} → ${response.status}`);
        return response.status === 204 ? null : response.json();
      };
      const { token } = (await json("/api/auth/csrf")) as { token: string };
      const property = ((await json("/api/properties/")) as { id: string }[])[0];
      const body = (await json("/api/work/", {
        method: "POST",
        headers: { "Content-Type": "application/json", "X-CSRF-TOKEN": token },
        body: JSON.stringify({ title, propertyId: property.id, priority: "Critical" }),
      })) as { item: { id: string } };
      return { workId: body.item.id };
    },
    { title },
  );

  await page.getByRole("link", { name: "Autopilot" }).click();
  await expect(page.getByRole("heading", { name: "Autopilot daily brief" })).toBeVisible();

  await page.getByRole("button", { name: "Run analysis now" }).click();
  await expect(page.getByText(/Analysis complete/)).toBeVisible();

  await page.getByLabel("Signal").selectOption("SlaRisk");

  const workHref = `/work/${workId}`;
  const row = await findRowLinkingTo(page, workHref);
  if (!row) throw new Error(`No SlaRisk Autopilot finding links to ${workHref}`);
  await expect(row).toBeVisible();

  // Evidence drawer: WorkSignalRules-derived findings never carry a dollar estimate (see
  // EfSignalCatalog.EvaluateWorkSignalsAsync — ImpactEstimate.EstimatedAmount is always null for
  // this signal), so the UI must say so honestly rather than showing $0 or hiding the line.
  await expect(row.getByText("no dollar estimate available for this finding.")).toBeVisible();
  // Scoped to the calculation-inputs block (the first .autopilot-evidence list) — "Status" also
  // appears, unrelatedly, as the collapsed row's own status column label.
  const inputsList = row.locator(".autopilot-evidence").first();
  await expect(inputsList.getByText("Status", { exact: true })).toBeVisible();
  await expect(inputsList.getByText("Priority", { exact: true })).toBeVisible();
  const subjectLink = row.getByRole("link", { name: /WorkItem \(/ }).first();
  await expect(subjectLink).toContainText("WorkItem");

  // Dismiss it, with a reason typed into the row before opening the confirm modal — the reason
  // field lives on the row (it doubles as the snooze form), not in the modal itself.
  await row.getByLabel("Reason (used if you dismiss)").fill("Handled outside Autopilot");
  await row.getByRole("button", { name: "Dismiss" }).click();
  await page.getByRole("dialog").getByRole("button", { name: "Confirm" }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);

  // Gone from the default (workable) view — the server excludes Dismissed/Resolved unless a
  // status is explicitly requested (AutopilotEndpoints.cs's /brief handler).
  await expect
    .poll(async () => findRowLinkingTo(page, workHref, false).then((r) => r !== null))
    .toBe(false);

  // Reachable, and reopenable, from the Dismissed filter.
  await page.getByLabel("Status").selectOption("Dismissed");
  const dismissedRow = await findRowLinkingTo(page, workHref);
  if (!dismissedRow) throw new Error(`No Dismissed Autopilot finding links to ${workHref}`);
  await expect(dismissedRow.getByText("Dismissed")).toBeVisible();
  await dismissedRow.getByRole("button", { name: "Reopen" }).click();
  await expect
    .poll(async () => findRowLinkingTo(page, workHref).then((r) => r !== null))
    .toBe(false);

  await page.getByLabel("Status").selectOption("");
  const reopenedRow = await findRowLinkingTo(page, workHref);
  if (!reopenedRow) throw new Error(`No workable Autopilot finding links to ${workHref}`);
  await expect(reopenedRow.getByText("New", { exact: true })).toBeVisible();
});

// CPM-8.09: the recommendation/action-confirmation flow added to the same finding row. A single
// demo identity can create a recommendation but can never approve its own (AutopilotRecommendation
// .Decide's separation-of-duties guard — the same rule AutopilotActionProposal enforces), so this
// test proves that real domain guard surfaces as a real error in the UI rather than exercising the
// full approve → propose → execute chain, which needs a second, differently-authenticated actor
// that Playwright's single demo-admin session cannot provide. GovernedActionEndpointsTests
// (tests/PropFlow.IntegrationTests) already covers that deeper chain end to end, actor swap
// included.
test("a recommendation can be created from a finding, and approving your own is refused", async ({
  page,
}) => {
  const runId = `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;

  await page.goto("/");
  await page.getByLabel("Organization slug").fill(organizationSlug);
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill(password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page.getByRole("heading", { name: "Today" })).toBeVisible();

  const title = `E2E recommendation ${runId} burst pipe`;
  await page.evaluate(
    async ({ title }) => {
      const json = async (path: string, init?: RequestInit) => {
        const response = await fetch(path, {
          credentials: "same-origin",
          cache: "no-store",
          ...init,
        });
        if (!response.ok) throw new Error(`${init?.method ?? "GET"} ${path} → ${response.status}`);
        return response.status === 204 ? null : response.json();
      };
      const { token } = (await json("/api/auth/csrf")) as { token: string };
      const property = ((await json("/api/properties/")) as { id: string }[])[0];
      await json("/api/work/", {
        method: "POST",
        headers: { "Content-Type": "application/json", "X-CSRF-TOKEN": token },
        body: JSON.stringify({ title, propertyId: property.id, priority: "Critical" }),
      });
    },
    { title },
  );

  await page.getByRole("link", { name: "Autopilot" }).click();
  await page.getByRole("button", { name: "Run analysis now" }).click();
  await expect(page.getByText(/Analysis complete/)).toBeVisible();
  await page.getByLabel("Signal").selectOption("SlaRisk");

  const row = page
    .locator(".autopilot-row", { hasText: "Critical priority with no vendor or staff assigned." })
    .first();
  await row.locator(".autopilot-row-toggle").click();
  await expect(row.getByRole("heading", { name: "Recommendations" })).toBeVisible();

  const recommendationText = `Assign a vendor — E2E ${runId}`;
  await row.getByLabel("New recommendation").fill(recommendationText);
  await row.getByRole("button", { name: "Add recommendation" }).click();

  const card = row.locator(".autopilot-recommendation-card", { hasText: recommendationText });
  await expect(card).toBeVisible();
  await expect(card.getByText("Proposed", { exact: true })).toBeVisible();

  await card.getByRole("button", { name: "Approve" }).click();
  await expect(
    card.getByText("The account that proposed a recommendation cannot decide it."),
  ).toBeVisible();
  // Refused, not silently swallowed — the card is still Proposed, not Approved.
  await expect(card.getByText("Proposed", { exact: true })).toBeVisible();
});

// Expands each visible finding row until one whose evidence links back to `workHref`, returning
// it expanded — or null if none does. There is no GET /api/autopilot/findings/{id}; evidence is
// only visible once a row is expanded in the UI, so this is the same tradeoff the page itself
// makes, not a test-only workaround. Locators are re-queried by index each call rather than
// cached, so this is also safe to call again after the list has changed underneath it.
async function findRowLinkingTo(
  page: Page,
  workHref: string,
  paginate = true,
): Promise<Locator | null> {
  const workId = workHref.split("/").pop()!;
  for (let pageIndex = 0; pageIndex < 20; pageIndex += 1) {
    const rows = page.locator(".autopilot-row");
    const count = await rows.count();
    for (let index = 0; index < count; index += 1) {
      const row = rows.nth(index);
      await row.locator(".autopilot-row-toggle").click();
      if (
        (await row.locator(`a[href*="${workId}"]`).count()) ||
        (await row.getByText(workId.slice(0, 8), { exact: false }).count())
      )
        return row;
      await row.locator(".autopilot-row-toggle").click();
    }
    const next = page.getByRole("button", { name: "Next", exact: true });
    if (!paginate || (await next.isDisabled())) return null;
    await Promise.all([
      page.waitForResponse((response) => response.url().includes("/api/autopilot/brief")),
      next.click(),
    ]);
  }
  return null;
}
