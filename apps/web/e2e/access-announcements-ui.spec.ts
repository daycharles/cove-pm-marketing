import { expect, test, type Page } from "@playwright/test";

const password = process.env.PLAYWRIGHT_DEMO_PASSWORD ?? "DemoPassword!123";

async function signIn(page: Page, email: string) {
  await page.goto("/");
  await page.getByLabel("Organization slug").fill("averion-demo");
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill(password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page.getByRole("heading", { name: "Today" })).toBeVisible();
}

test("access denied explains the safe next step in day and night mode", async ({ page }) => {
  await signIn(page, "demo-resident@averion.example.test");
  await page.goto("/announcements");

  const denied = page.locator(".access-denied-card");
  await expect(denied).toBeVisible();
  await expect(denied).toContainText("You don’t have access to this page");
  await expect(denied).toContainText("contact your organization administrator");
  await expect(page.getByRole("link", { name: "Return to Work" })).toHaveAttribute("href", "/");

  const toggle = page.getByRole("button", { name: "Switch to night mode" });
  await toggle.click();
  await expect(page.locator("main.access-denied")).toHaveAttribute("data-theme", "dark");
  await expect(page.getByRole("button", { name: "Switch to day mode" })).toBeVisible();
  await expect(denied).toBeVisible();
});

test("announcement draft controls align wide and stack on narrow screens", async ({ page }) => {
  await signIn(page, "demo-admin@averion.example.test");
  await page.goto("/announcements");

  const form = page.locator("form.form-grid");
  const title = page.getByLabel("Announcement title");
  const message = page.getByLabel("Announcement body");
  const expiry = page.getByLabel("Announcement expiry");
  await expect(form).toBeVisible();
  await expect(title).toHaveCSS("min-height", "44px");
  await expect(expiry).toHaveCSS("min-height", "44px");

  await page.setViewportSize({ width: 520, height: 900 });
  const narrowTitle = await title.boundingBox();
  const narrowExpiry = await expiry.boundingBox();
  expect(narrowTitle?.x).toBeCloseTo(narrowExpiry?.x ?? -1, 0);

  await page.setViewportSize({ width: 1280, height: 900 });
  const wideTitle = await title.boundingBox();
  const wideExpiry = await expiry.boundingBox();
  const wideMessage = await message.boundingBox();
  expect(Math.abs((wideTitle?.y ?? -1) - (wideExpiry?.y ?? -1))).toBeLessThanOrEqual(1);
  expect(wideMessage?.width).toBeGreaterThan((wideTitle?.width ?? 0) + 100);
});
