import { expect, test } from "@playwright/test";

const session = {
  userId: "operator-1",
  organizationId: "org-1",
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

const work = [
  { id: "work-1", title: "No cooling in unit 4B", status: "New", priority: "Critical", propertyName: "Harbor View", dueDate: "2026-09-14T14:00:00Z", rowVersion: "2", version: 2 },
  { id: "work-2", title: "Move-out inspection: 12A", status: "Scheduled", priority: "High", propertyName: "Maple Court", vendorName: "Northstar Services", dueDate: "2026-09-15T15:00:00Z", rowVersion: "4", version: 4 },
  { id: "work-3", title: "Replace hallway light", status: "InProgress", priority: "Normal", propertyName: "Harbor View", vendorName: "Northstar Services", dueDate: "2026-09-18T15:00:00Z", rowVersion: "6", version: 6 },
];

async function mockApi(page: import("@playwright/test").Page) {
  await page.context().route("**/*", async (route) => {
    const url = new URL(route.request().url());
    if (!url.pathname.startsWith("/api/")) return route.continue();
    const json = (body: unknown) => route.fulfill({ contentType: "application/json", body: JSON.stringify(body) });
    if (url.pathname === "/api/session") return json(session);
    if (url.pathname === "/api/work/" || url.pathname === "/api/work") return json({ items: work, totalCount: work.length });
    if (url.pathname === "/api/vendors/") return json([{ id: "vendor-1", name: "Northstar Services", isActive: true }]);
    if (url.pathname === "/api/employees/") return json([{ id: "employee-1", displayName: "Jordan Lee", isActive: true }]);
    if (url.pathname === "/api/saved-views/") return json([]);
    if (url.pathname === "/api/categories/") return json([]);
    if (url.pathname === "/api/communication/templates/available") return json([]);
    if (url.pathname === "/api/work/work-1") return json({ item: { ...work[0], propertyId: "property-1", description: "Resident reports warm air from the bedroom vent.", vendorId: null, employeeId: null, scheduledStart: null, scheduledEnd: null, internalNotes: "Resident requested an afternoon visit.", residentVisibleNotes: "We received your request.", cost: null, assetId: null }, version: 2 });
    if (url.pathname === "/api/work/work-1/timeline") return json([{ id: "event-1", eventType: "WorkCreated", occurredAt: "2026-09-13T14:00:00Z", newValue: "New" }, { id: "event-2", eventType: "PriorityChanged", occurredAt: "2026-09-13T14:05:00Z", oldValue: "High", newValue: "Critical" }]);
    if (url.pathname === "/api/assets/") return json([]);
    if (url.pathname === "/api/work/work-1/attachments/") return json([]);
    if (url.pathname === "/api/announcements/") return json([{ id: "announce-1", title: "Lobby elevator maintenance", status: "Draft", expiresAt: null }, { id: "announce-2", title: "Autumn fire-safety inspection", status: "Published", expiresAt: "2026-10-01T16:00:00Z" }]);
    return json([]);
  });
}

test("captures operations workstream surfaces", async ({ page }) => {
  await mockApi(page);
  await page.setViewportSize({ width: 1440, height: 1050 });
  await page.goto("http://127.0.0.1:3200/");
  await expect(page.getByRole("heading", { name: "Work queue" })).toBeVisible();
  await expect(page.getByAltText("Cove Property Management Software")).toBeVisible();
  await page.screenshot({ path: "../../outputs/ui-workstream-b-work-queue.png", fullPage: true });

  await page.emulateMedia({ colorScheme: "dark" });
  await page.screenshot({ path: "../../outputs/ui-workstream-b-work-queue-night.png", fullPage: true });
  await page.emulateMedia({ colorScheme: "light" });

  await page.goto("http://127.0.0.1:3200/work/work-1");
  await expect(page.getByText("Assign a vendor or employee")).toBeVisible();
  await page.screenshot({ path: "../../outputs/ui-workstream-b-work-detail.png", fullPage: true });

  await page.goto("http://127.0.0.1:3200/announcements");
  await expect(page.getByRole("heading", { name: "Announcement queue" })).toBeVisible();
  await page.screenshot({ path: "../../outputs/ui-workstream-b-communications.png", fullPage: true });

  await page.emulateMedia({ colorScheme: "dark" });
  await page.screenshot({ path: "../../outputs/ui-workstream-b-communications-night.png", fullPage: true });
});
