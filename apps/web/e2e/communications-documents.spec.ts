import { expect, test } from "@playwright/test";

const password = process.env.PLAYWRIGHT_DEMO_PASSWORD ?? "DemoPassword!123";

test("authenticated admin can reach communications and document workflow APIs", async ({
  page,
}) => {
  await page.goto("/");
  await page.getByLabel("Organization slug").fill("averion-demo");
  await page.getByLabel("Email").fill("demo-admin@averion.example.test");
  await page.getByLabel("Password").fill(password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page.getByRole("heading", { name: "Today" })).toBeVisible();

  const statuses = await page.evaluate(async () => {
    const paths = ["/api/communication/campaigns", "/api/communication/document-templates"];
    return Promise.all(paths.map(async (path) => (await fetch(path)).status));
  });
  expect(statuses).toEqual([200, 200]);
});

test("workflow APIs reject unauthenticated browser requests", async ({ request }) => {
  expect((await request.get("/api/communication/conversations")).status()).toBe(401);
  expect((await request.get("/api/communication/document-packets")).status()).toBe(401);
});
