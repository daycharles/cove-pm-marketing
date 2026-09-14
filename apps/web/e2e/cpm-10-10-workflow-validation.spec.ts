import { expect, test } from "@playwright/test";

const password = process.env.PLAYWRIGHT_DEMO_PASSWORD ?? "DemoPassword!123";

// CPM-10.10: smoke the complete Emily operator workspace against the deterministic demo seed.
// The deeper mutation and authorization assertions stay in the workflow-specific specs; this
// pass catches a missing route, broken navigation link, or a page that cannot render its seeded
// source data after the M10 slice is integrated.
test("Emily's M10 operator workspace destinations render from seeded data", async ({ page }) => {
  await page.goto("/");
  await page.getByLabel("Organization slug").fill("averion-demo");
  await page.getByLabel("Email").fill("demo-admin@averion.example.test");
  await page.getByLabel("Password").fill(password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page.getByRole("heading", { name: "Today" })).toBeVisible();

  const destinations = [
    ["/work", "Work queue"],
    ["/residents", "Residents"],
    ["/calendar", "Lifecycle calendar"],
    ["/inspections", "Inspections & make-ready"],
    ["/procurement", "Procurement workspace"],
    ["/reports", "Reports hub"],
    ["/leasing/leases", "Lease lifecycle"],
    ["/properties", "Properties"],
    ["/attention", "Needs your attention"],
  ] as const;

  for (const [route, heading] of destinations) {
    await page.goto(route);
    await expect(page.getByRole("heading", { name: heading, exact: true })).toBeVisible();
  }
});
