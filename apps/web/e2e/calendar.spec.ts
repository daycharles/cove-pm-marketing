import { expect, test } from "@playwright/test";

const password = process.env.PLAYWRIGHT_DEMO_PASSWORD ?? "DemoPassword!123";

test("calendar shows lifecycle events and supports view and property filters", async ({ page }) => {
  await page.goto("/");
  await page.getByLabel("Organization slug").fill("averion-demo");
  await page.getByLabel("Email").fill("demo-admin@averion.example.test");
  await page.getByLabel("Password").fill(password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page.getByRole("heading", { name: "Today" })).toBeVisible();

  await page.getByRole("link", { name: "Calendar" }).click();
  await expect(page.getByRole("heading", { name: "Lifecycle calendar" })).toBeVisible();
  await expect(page.getByRole("link", { name: /Wasp nest on the balcony/ }).first()).toBeVisible();

  await page.getByRole("combobox", { name: "View" }).selectOption("week");
  await expect(page.getByRole("combobox", { name: "View" })).toHaveValue("week");
  await page.getByRole("combobox", { name: "Property" }).selectOption({ label: "Harbor View Apartments" });
  await expect(page.getByRole("link", { name: /Wasp nest on the balcony/ }).first()).toBeVisible();
  await expect(page.getByRole("link", { name: /Furnace makes a loud noise Maple Court/ })).toHaveCount(0);
});
