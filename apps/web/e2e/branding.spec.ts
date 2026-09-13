import { expect, test } from "@playwright/test";

async function expectTransparentLogo(page: import("@playwright/test").Page, selector: string) {
  const logo = page.locator(selector);
  await expect(logo).toBeVisible();
  await expect(logo).toHaveAttribute("src", /\/brand\/cove-logo-(light|dark)\.png$/);
  await expect
    .poll(() => logo.evaluate((element) => (element as HTMLImageElement).naturalWidth))
    .toBeGreaterThan(0);
  await expect
    .poll(() => logo.evaluate((element) => (element as HTMLImageElement).naturalHeight))
    .toBeGreaterThan(0);
  await expect(logo).toHaveCSS("background-color", "rgba(0, 0, 0, 0)");

  const cornerAlpha = await logo.evaluate((element) => {
    const image = element as HTMLImageElement;
    const canvas = document.createElement("canvas");
    canvas.width = image.naturalWidth;
    canvas.height = image.naturalHeight;
    const context = canvas.getContext("2d");
    if (!context) throw new Error("Canvas 2D context unavailable");
    context.drawImage(image, 0, 0);
    return context.getImageData(0, 0, 1, 1).data[3];
  });
  expect(cornerAlpha).toBe(0);
}

test("Cove login keeps transparent, crisp logo variants in both themes", async ({ page }) => {
  await page.goto("/");
  await expectTransparentLogo(page, ".login-brand");
  await expect(page.locator(".login-brand")).toHaveJSProperty("naturalWidth", 660);

  await page.getByRole("button", { name: "Switch to night mode" }).click();
  await expect(page.locator(".login-brand")).toHaveAttribute("src", /cove-logo-dark\.png$/);
  await expectTransparentLogo(page, ".login-brand");
  await expect(page.locator(".login-brand")).toHaveJSProperty("naturalWidth", 634);
  await expect(page.locator(".login")).toHaveAttribute("data-theme", "dark");
});

test("Cove login logo remains transparent at a mobile viewport", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/");
  await expectTransparentLogo(page, ".login-brand");
  await expect(page.locator(".login-brand")).toBeVisible();
});
