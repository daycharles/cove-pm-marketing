import { expect, test } from "@playwright/test";

const session = {
  userId: "sidebar-test-user",
  organizationId: "sidebar-test-org",
  role: "Manager",
  capabilities: [
    "Work.Read",
    "Work.Update",
    "Work.AssignVendor",
    "Work.AssignEmployee",
    "Work.ManageAttachments",
    "Communications.SendMessage",
    "Leasing.Manage",
  ],
};

test("primary navigation changes routes without runtime errors or dead clicks", async ({
  page,
}) => {
  const runtimeErrors: string[] = [];
  page.on("pageerror", (error) => runtimeErrors.push(error.message));
  page.on("console", (message) => {
    if (message.type() === "error") runtimeErrors.push(message.text());
  });

  await page.route("**/api/**", async (route) => {
    const pathname = new URL(route.request().url()).pathname;
    if (pathname === "/api/session") {
      await route.fulfill({ contentType: "application/json", body: JSON.stringify(session) });
      return;
    }
    if (pathname === "/api/attention") {
      await route.fulfill({
        contentType: "application/json",
        body: JSON.stringify({ items: [], criticalCount: 0, warningCount: 0 }),
      });
      return;
    }
    await route.fulfill({ contentType: "application/json", body: "[]" });
  });

  await page.goto("/properties");
  const nav = page.getByRole("navigation", { name: "Primary navigation" });
  await expect(nav).toBeVisible();

  const links = await nav.getByRole("link").evaluateAll((elements) =>
    elements.map((element) => ({
      href: element.getAttribute("href"),
      label: element.textContent,
    })),
  );
  for (const { href, label } of links) {
    if (!href || !label) continue;
    await page.goto("/properties");
    await expect(nav).toBeVisible();
    await nav.getByRole("link", { name: label }).click();
    await expect(page).toHaveURL(new RegExp(`${href.replace(/[.*+?^${}()|[\\]\\]/g, "\\$&")}$`));
  }

  expect(runtimeErrors.filter((message) => !message.includes("favicon"))).toEqual([]);
});
