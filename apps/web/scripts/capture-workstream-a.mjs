import { chromium } from "@playwright/test";
import { mkdir } from "node:fs/promises";

const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:3100";
const output = "../../outputs/workstream-a";
const routes = [
  ["today", "/", "Today"],
  ["properties", "/properties", "Properties"],
  ["leases", "/leasing/leases", "Lease lifecycle"],
];

const property = {
  id: "p1",
  portfolioId: "portfolio-1",
  name: "Harbor View Apartments",
  timeZoneId: "America/New_York",
};
const leases = [
  {
    id: "l1",
    residentId: "r1",
    spaceId: "s1",
    startsOn: "2026-01-01",
    endsOn: "2026-10-01",
    monthlyRent: 1850,
    status: "Active",
  },
  {
    id: "l2",
    residentId: "r2",
    spaceId: "s2",
    startsOn: "2025-10-01",
    endsOn: "2026-09-30",
    monthlyRent: 1625,
    status: "NoticeGiven",
  },
  {
    id: "l3",
    residentId: "r3",
    spaceId: "s3",
    startsOn: "2026-10-01",
    endsOn: "2027-09-30",
    monthlyRent: 1775,
    status: "Draft",
  },
];
function response(url) {
  const path = new URL(url).pathname;
  if (path === "/api/session")
    return {
      userId: "u1",
      organizationId: "o1",
      role: "Property Manager",
      capabilities: ["Work.Read", "Properties.Manage", "Leasing.Manage", "Applications.Manage"],
    };
  if (path === "/api/attention")
    return {
      criticalCount: 1,
      warningCount: 2,
      informationalCount: 0,
      items: [
        {
          workId: "w1",
          title: "Water leak reported in 204",
          propertyId: "p1",
          propertyName: "Harbor View Apartments",
          status: "New",
          priority: "Critical",
          dueDate: "2026-09-13",
          severity: "Critical",
          findings: [],
        },
        {
          workId: "w2",
          title: "Turn readiness: Unit 302",
          propertyId: "p1",
          propertyName: "Harbor View Apartments",
          status: "Scheduled",
          priority: "High",
          dueDate: "2026-09-14",
          severity: "Warning",
          findings: [],
        },
      ],
    };
  if (path === "/api/properties/")
    return [
      property,
      { id: "p2", portfolioId: "portfolio-1", name: "Maple Court", timeZoneId: "America/New_York" },
    ];
  if (path === "/api/portfolios/") return [{ id: "portfolio-1", name: "Tidewater Portfolio" }];
  if (path === "/api/leasing/leases/") return leases;
  if (path === "/api/residents/")
    return [
      { id: "r1", fullName: "Jordan Lee" },
      { id: "r2", fullName: "Mina Patel" },
      { id: "r3", fullName: "Chris Morgan" },
    ];
  if (path === "/api/marketing/listings/")
    return [
      {
        id: "listing-1",
        propertyId: "p1",
        headline: "Sunny one-bedroom",
        availableOn: "2026-09-25",
        monthlyRent: 1850,
        status: "Published",
      },
    ];
  return [];
}
async function prepare(page) {
  console.log("preparing mocked API");
  await page.route("**/api/**", async (request) =>
    request.fulfill({
      contentType: "application/json",
      body: JSON.stringify(response(request.request().url())),
    }),
  );
  console.log("opening mocked app");
  await page.goto(baseURL, { waitUntil: "domcontentloaded" });
  console.log("waiting for app shell");
  await page.getByRole("link", { name: "Cove PM home" }).waitFor();
}

async function capture(page, name, route, heading, theme) {
  console.log(`capturing ${name} ${theme}`);
  await page.goto(new URL(route, baseURL).href, { waitUntil: "domcontentloaded" });
  await page.getByRole("heading", { name: heading }).waitFor();
  const toggle = page.getByRole("button", { name: /Switch to (night|day) mode/ });
  const night = await toggle.getAttribute("aria-pressed");
  if ((theme === "night") !== (night === "true")) await toggle.click();
  await page.waitForTimeout(120);
  await page.screenshot({ path: `${output}/${name}-${theme}.png`, fullPage: true });
}

async function verifyThemePreference(browser) {
  const context = await browser.newContext({
    colorScheme: "dark",
    viewport: { width: 1280, height: 800 },
  });
  const page = await context.newPage();
  const preferenceErrors = [];
  page.on("console", (message) => {
    if (message.type() === "error") preferenceErrors.push(message.text());
  });
  page.on("pageerror", (error) => preferenceErrors.push(error.message));
  try {
    await prepare(page);
    await page.waitForFunction(() => document.documentElement.dataset.theme === "night");
    if ((await page.getByRole("button", { name: "Switch to day mode" }).count()) !== 1) {
      throw new Error("Night preference did not produce the accessible day-mode switch label.");
    }
    if (
      (await page.getByRole("img", { name: "Cove Property Management Software" }).count()) !== 1
    ) {
      throw new Error("The isolated Cove wordmark is missing its accessible name.");
    }
    await page.getByRole("button", { name: "Switch to day mode" }).click();
    await page.reload({ waitUntil: "domcontentloaded" });
    await page.getByRole("link", { name: "Cove PM home" }).waitFor();
    await page.waitForFunction(() => document.documentElement.dataset.theme === "day");
    if (preferenceErrors.length) throw new Error(preferenceErrors.join("\n"));
  } finally {
    await context.close();
  }
}

await mkdir(output, { recursive: true });
const errors = [];
console.log("launching capture browser");
const browser = await chromium.launch({ headless: true });
console.log("capture browser launched");
try {
  await verifyThemePreference(browser);
  const desktop = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
  desktop.on("console", (message) => {
    if (message.type() === "error") errors.push(`console: ${message.text()}`);
  });
  desktop.on("pageerror", (error) => errors.push(`pageerror: ${error.message}`));
  await prepare(desktop);
  for (const [name, route, heading] of routes) {
    await capture(desktop, name, route, heading, "day");
    await capture(desktop, name, route, heading, "night");
  }
  const mobile = await browser.newPage({ viewport: { width: 390, height: 844 }, isMobile: true });
  mobile.on("console", (message) => {
    if (message.type() === "error") errors.push(`mobile console: ${message.text()}`);
  });
  mobile.on("pageerror", (error) => errors.push(`mobile pageerror: ${error.message}`));
  await prepare(mobile);
  await capture(mobile, "today-mobile", "/", "Today", "night");
} finally {
  await browser.close();
}
if (errors.length) throw new Error(`Browser errors:\n${errors.join("\n")}`);
