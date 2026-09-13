import { expect, test } from "@playwright/test";

const password = process.env.PLAYWRIGHT_DEMO_PASSWORD ?? "DemoPassword!123";

test("billing workspace loads its operator module and selected lease ledger", async ({ page }) => {
  await page.goto("/");
  await page.getByLabel("Organization slug").fill("averion-demo");
  await page.getByLabel("Email").fill("demo-admin@averion.example.test");
  await page.getByLabel("Password").fill(password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page.getByRole("heading", { name: "Work" })).toBeVisible();

  await page.goto("/billing");
  await expect(page.getByRole("heading", { name: "Billing" })).toBeVisible();
  await expect(page.getByLabel("Lease")).toBeVisible();
  await expect(page.getByRole("heading", { name: "Portfolio snapshot" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Lease ledger" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Payment operations" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Controls & collections" })).toBeVisible();

  const lease = page.getByLabel("Lease");
  await lease.selectOption({ index: 1 });
  await expect(page.getByText("Generate through today")).toBeVisible();
  await expect(page.getByRole("heading", { name: "Credits" })).toBeVisible();
});
