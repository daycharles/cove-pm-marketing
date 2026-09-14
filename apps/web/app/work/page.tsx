"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { useSearchParams } from "next/navigation";
import Link from "next/link";
import Image from "next/image";
import {
  api,
  ApiError,
  type Category,
  type Employee,
  type MessageTemplate,
  type Session,
  type Vendor,
  type SavedView,
  type WorkItem,
  type WorkListQuery,
} from "../../lib/api";
import { hasCapability } from "../../lib/capabilities";
import { AppShell } from "../components/app-shell";
import { AssignNotifyFlow } from "../components/assign-notify";
import { BulkEditFlow } from "../components/bulk-edit";
import { ProtectedPage } from "../components/protected-page";
import { returnToHref } from "../../lib/workspace-context";

const statuses = [
  "Draft",
  "New",
  "Assigned",
  "Scheduled",
  "InProgress",
  "OnHold",
  "Completed",
  "Cancelled",
];
const priorities = ["Low", "Normal", "High", "Critical"];
type Sort = "title" | "status" | "priority" | "dueDate";

// PF-S03.09 audit finding: the Cove UI rebrand replaced Home()'s default view with
// TodayDashboard and dropped this page's route/nav entry, leaving the whole bulk-work-order
// workflow (filters, bulk selection, bulk vendor assignment, saved views — docs/demo-script.md's
// own headline feature) unreachable from the UI even though every line of it still worked.
// This file restores it at its own route, the same shape every other feature area already uses
// (/properties, /leasing/leases, /settings/categories, ...) rather than reinstating it as the
// default landing view, which the rebrand's Today dashboard is a genuine improvement over.
export default function WorkPage() {
  return (
    <ProtectedPage capability="Work.Read">
      {(session) => (
        <AppShell session={session}>
          <WorkList session={session} />
        </AppShell>
      )}
    </ProtectedPage>
  );
}

const defaultQuery: WorkListQuery = { sort: "title", page: 1, pageSize: 100 };

// The global search palette deep-links here with a filter in the URL (e.g. ?propertyId=…).
const urlFilterKeys = [
  "search",
  "status",
  "priority",
  "categoryId",
  "propertyId",
  "spaceId",
  "employeeId",
  "vendorId",
  "age",
] as const;
function queryFromUrl(): Partial<WorkListQuery> {
  if (typeof window === "undefined") return {};
  const params = new URLSearchParams(window.location.search);
  const picked: Partial<WorkListQuery> = {};
  for (const key of urlFilterKeys) {
    const value = params.get(key);
    if (value) picked[key] = value;
  }
  return picked;
}

function WorkList({ session }: { session: Session }) {
  const searchParams = useSearchParams();
  const currentLocation = `/work${searchParams.toString() ? `?${searchParams}` : ""}`;
  const [work, setWork] = useState<WorkItem[]>([]);
  const [vendors, setVendors] = useState<Vendor[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [templates, setTemplates] = useState<MessageTemplate[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  // A filter carried in the URL (a global-search click-through) seeds the query and, below,
  // stops a saved default view from overriding it. WorkList only ever renders on the client
  // (Home shows the login form until the session loads), so reading the URL here is safe.
  const urlSeed = queryFromUrl();
  const [query, setQuery] = useState<WorkListQuery>(() => ({ ...defaultQuery, ...urlSeed }));
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [vendorId, setVendorId] = useState("");
  const [assignNotify, setAssignNotify] = useState(false);
  const [bulkEdit, setBulkEdit] = useState(false);
  const [employeeId, setEmployeeId] = useState("");
  const [templateId, setTemplateId] = useState("");
  const [bulkAction, setBulkAction] = useState<"vendor" | "employee" | "message">("vendor");
  const [flow, setFlow] = useState<"choose" | "confirm" | "success" | null>(null);
  const [result, setResult] = useState<{
    changed: number;
    unchanged: number;
    total: number;
  } | null>(null);
  const [savedViews, setSavedViews] = useState<SavedView[]>([]);
  const [viewName, setViewName] = useState("");
  const [viewIsDefault, setViewIsDefault] = useState(false);
  const [queueClock, setQueueClock] = useState<number | null>(null);
  // The default view is applied once, when reference data first arrives. Any user-driven query
  // change also sets this, so a late default view cannot clobber filters already on screen.
  const defaultViewApplied = useRef(Object.keys(urlSeed).length > 0);
  async function saveView() {
    const name = viewName.trim();
    if (!name) return;
    try {
      const created = await api.savedViews.create({
        name,
        filters: query,
        isDefault: viewIsDefault,
      });
      // Saving a new default clears the previous one server-side, so re-read the list — through
      // loadReference so the read carries the same sequence guard as every other reference load.
      if (viewIsDefault) await loadReference();
      else setSavedViews((current) => [...current, created]);
      setViewName("");
      setViewIsDefault(false);
    } catch (cause) {
      setError(cause instanceof ApiError ? cause.message : "Unable to save this view.");
    }
  }
  function applyView(view: SavedView) {
    defaultViewApplied.current = true;
    try {
      setQuery(JSON.parse(view.filters) as WorkListQuery);
    } catch {
      setError("This saved view has invalid filters.");
    }
  }
  async function deleteView(id: string) {
    try {
      await api.savedViews.delete(id);
      setSavedViews((current) => current.filter((view) => view.id !== id));
    } catch (cause) {
      setError(cause instanceof ApiError ? cause.message : "Unable to delete this view.");
    }
  }
  const visibleSelected = useMemo(
    () => work.filter((item) => selected.has(item.id)),
    [work, selected],
  );
  // Every work request takes the next ticket; only the newest ticket may touch state. Without
  // this an earlier, unfiltered response can resolve last and overwrite a filtered one, leaving
  // rows on screen that contradict the filters (and never correcting themselves).
  const workRequest = useRef(0);
  async function loadWork(next = query) {
    const ticket = ++workRequest.current;
    setLoading(true);
    setError("");
    try {
      const listed = await api.work.list(next);
      if (ticket !== workRequest.current) return;
      setWork(listed.items);
      setQueueClock(Date.now());
      setSelected(
        (current) =>
          new Set([...current].filter((id) => listed.items.some((item) => item.id === id))),
      );
    } catch (cause) {
      if (ticket !== workRequest.current) return;
      setError(
        cause instanceof ApiError
          ? cause.message
          : "Unable to load work. Please refresh and try again.",
      );
    } finally {
      // A superseded request must not clear the spinner a newer one is still showing.
      if (ticket === workRequest.current) setLoading(false);
    }
  }
  // Vendors, saved views and categories do not depend on `query`, so they load once per mount
  // (and on an explicit Refresh) rather than on every keystroke.
  const referenceRequest = useRef(0);
  async function loadReference() {
    const ticket = ++referenceRequest.current;
    try {
      const [vendorList, viewList, categoryList, employeeList, templateList] = await Promise.all([
        api.vendors.list(),
        api.savedViews.list(),
        api.categories.list(),
        hasCapability(session, "Work.AssignEmployee") ? api.employees.list() : Promise.resolve([]),
        hasCapability(session, "Communications.SendMessage")
          ? api.messageTemplates.available()
          : Promise.resolve([]),
      ]);
      if (ticket !== referenceRequest.current) return;
      setVendors(vendorList.filter((vendor) => vendor.isActive));
      setSavedViews(viewList);
      setCategories(categoryList.filter((category) => !category.isArchived));
      setEmployees(employeeList.filter((employee) => employee.isActive));
      setTemplates(templateList);
      if (!defaultViewApplied.current) {
        defaultViewApplied.current = true;
        const preferred = viewList.find((view) => view.isDefault);
        // Applying the default view changes `query`, which re-runs the work effect below.
        if (preferred) applyView(preferred);
      }
    } catch (cause) {
      if (ticket !== referenceRequest.current) return;
      setError(
        cause instanceof ApiError ? cause.message : "Unable to load vendors, views and categories.",
      );
    }
  }
  useEffect(() => {
    const timer = window.setTimeout(() => void loadReference(), 0);
    return () => window.clearTimeout(timer);
    // Reference data is query-independent; it loads once for this mount.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);
  useEffect(() => {
    const timer = window.setTimeout(() => void loadWork(), 0);
    return () => window.clearTimeout(timer);
    // loadWork reads the query from this render; its identity is not part of the request trigger.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [query]);
  function updateQuery(changes: Partial<WorkListQuery>) {
    // The user has taken control of the filters; a default view arriving late must not win.
    defaultViewApplied.current = true;
    setQuery((current) => ({ ...current, ...changes, page: 1 }));
  }
  function clearFilters() {
    defaultViewApplied.current = true;
    setQuery({ ...defaultQuery });
  }
  function toggle(id: string) {
    setSelected((current) => {
      const next = new Set(current);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }
  function sortBy(sort: Sort) {
    defaultViewApplied.current = true;
    setQuery((current) => ({
      ...current,
      sort,
      descending: current.sort === sort ? !current.descending : false,
    }));
  }
  async function runBulkAction() {
    if (
      (bulkAction === "vendor" && !vendorId) ||
      (bulkAction === "employee" && !employeeId) ||
      (bulkAction === "message" && !templateId)
    )
      return;
    setError("");
    try {
      const concurrencyTokens = Object.fromEntries(
        visibleSelected
          .filter((item) => item.rowVersion)
          .map((item) => [item.id, item.rowVersion!]),
      );
      const input = { workIds: visibleSelected.map((item) => item.id), concurrencyTokens };
      const response =
        bulkAction === "vendor"
          ? await api.work.bulkAssignVendor({ ...input, vendorId })
          : bulkAction === "employee"
            ? await api.work.bulkAssignEmployee({ ...input, employeeId })
            : await api.work.bulkSendResidentMessage({ workIds: input.workIds, templateId });
      setResult(response);
      setFlow("success");
      await loadWork();
    } catch (cause) {
      setFlow(null);
      // Refresh first: loadWork clears the error banner on entry, so setting the message before
      // it would wipe the message the user needs to read.
      await loadWork();
      setError(
        bulkAction !== "message" && cause instanceof ApiError && cause.status === 409
          ? "Some selected work changed. The list was refreshed; review it and try again."
          : cause instanceof ApiError
            ? cause.message
            : `Bulk ${bulkAction === "message" ? "message" : "assignment"} could not be completed.`,
      );
    }
  }
  const allSelected = work.length > 0 && work.every((item) => selected.has(item.id));
  const chosenVendor = vendors.find((vendor) => vendor.id === vendorId);
  const chosenEmployee = employees.find((employee) => employee.id === employeeId);
  const chosenTemplate = templates.find((template) => template.id === templateId);
  const actionLabel =
    bulkAction === "vendor"
      ? "Assign vendor"
      : bulkAction === "employee"
        ? "Assign employee"
        : "Send resident message";
  const defaultView = savedViews.find((view) => view.isDefault);
  const assignedTotal = result?.total ?? visibleSelected.length;
  const openWork = work.filter((item) => !["Completed", "Cancelled"].includes(item.status));
  const urgentWork = openWork.filter((item) => ["High", "Critical"].includes(item.priority));
  const unownedWork = openWork.filter((item) => !item.vendorName && !item.employeeId);
  const dueSoon = openWork.filter((item) => {
    if (!item.dueDate) return false;
    const due = new Date(item.dueDate).getTime();
    return queueClock !== null && due <= queueClock + 48 * 60 * 60 * 1000;
  });
  const quickViews = [
    { label: "Urgent", count: urgentWork.length, changes: { priority: "High" } },
    { label: "Unassigned", count: unownedWork.length, changes: { status: "New" } },
    {
      label: "Scheduled",
      count: work.filter((item) => item.status === "Scheduled").length,
      changes: { status: "Scheduled" },
    },
    {
      label: "On hold",
      count: work.filter((item) => item.status === "OnHold").length,
      changes: { status: "OnHold" },
    },
  ] as const;
  if (!hasCapability(session, "Work.Read"))
    return (
      <section className="panel">
        <h1>Work</h1>
        <p>You do not have access to work items.</p>
      </section>
    );
  return (
    <section className="work-queue workflow-branded">
      <div className="work-heading">
        <div>
          <p className="eyebrow">Operations · maintenance & inspections</p>
          <div className="workflow-title-lockup">
            <Image
              className="workflow-feature-icon"
              src="/brand/maintenance-feature-icon.png"
              alt=""
              aria-hidden="true"
              width={220}
              height={140}
            />
            <h1>Work queue</h1>
          </div>
          <p>Resolve the work that is blocked, urgent, or due next.</p>
        </div>
        <div className="workflow-heading-actions">
          <Image
            className="workflow-wordmark"
            src="/brand/cove-pm-wordmark.png"
            alt="Cove Property Management Software"
            width={1256}
            height={590}
          />
          <button
            className="secondary"
            onClick={() => {
              void loadWork();
              void loadReference();
            }}
            disabled={loading}
          >
            Refresh
          </button>
        </div>
      </div>
      {error && (
        <p className="message" role="alert">
          {error}
        </p>
      )}
      <section className="queue-pulse" aria-label="Queue summary">
        <button
          type="button"
          className="queue-metric is-urgent"
          onClick={() => updateQuery({ priority: "High" })}
        >
          <strong>{urgentWork.length}</strong>
          <span>urgent or critical</span>
        </button>
        <button
          type="button"
          className="queue-metric"
          onClick={() => updateQuery({ status: "New" })}
        >
          <strong>{unownedWork.length}</strong>
          <span>need an owner</span>
        </button>
        <button type="button" className="queue-metric" onClick={() => sortBy("dueDate")}>
          <strong>{dueSoon.length}</strong>
          <span>due within 48 hours</span>
        </button>
        <div className="queue-metric queue-progress">
          <strong>{openWork.length}</strong>
          <span>active of {work.length} visible</span>
        </div>
      </section>
      <div className="filter-chips" aria-label="Quick queue filters">
        <span>Show:</span>
        {quickViews.map((view) => (
          <button
            key={view.label}
            type="button"
            className="filter-chip"
            onClick={() => updateQuery(view.changes)}
          >
            {view.label} <b>{view.count}</b>
          </button>
        ))}
        <button type="button" className="link-button" onClick={clearFilters}>
          Reset queue
        </button>
      </div>
      <section className="panel work-panel">
        <div className="filters" aria-label="Work filters">
          <label>
            Search
            <input
              value={query.search ?? ""}
              onChange={(event) => updateQuery({ search: event.target.value })}
              placeholder="Title, resident, or unit"
            />
          </label>
          <label>
            Status
            <select
              value={query.status ?? ""}
              onChange={(event) => updateQuery({ status: event.target.value || undefined })}
            >
              <option value="">All statuses</option>
              {statuses.map((value) => (
                <option key={value}>{value}</option>
              ))}
            </select>
          </label>
          <label>
            Priority
            <select
              value={query.priority ?? ""}
              onChange={(event) => updateQuery({ priority: event.target.value || undefined })}
            >
              <option value="">All priorities</option>
              {priorities.map((value) => (
                <option key={value}>{value}</option>
              ))}
            </select>
          </label>
          <label>
            Category
            <select
              value={query.categoryId ?? ""}
              onChange={(event) => updateQuery({ categoryId: event.target.value || undefined })}
            >
              <option value="">All categories</option>
              {categories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </select>
          </label>
          <label className="mobile-sort">
            Sort by
            <select
              aria-label="Sort by"
              value={`${query.sort ?? "title"}:${query.descending ? "desc" : "asc"}`}
              onChange={(event) => {
                const [sort, direction] = event.target.value.split(":");
                defaultViewApplied.current = true;
                setQuery((current) => ({ ...current, sort, descending: direction === "desc" }));
              }}
            >
              <option value="title:asc">Title (A–Z)</option>
              <option value="title:desc">Title (Z–A)</option>
              <option value="status:asc">Status</option>
              <option value="priority:desc">Priority (high first)</option>
              <option value="priority:asc">Priority (low first)</option>
              <option value="dueDate:asc">Due date (soonest)</option>
              <option value="dueDate:desc">Due date (latest)</option>
            </select>
          </label>
          <button className="secondary filter-clear" onClick={clearFilters}>
            Clear filters
          </button>
        </div>
        <div className="saved-views" aria-label="Saved views">
          <strong>Saved views</strong>
          <select
            aria-label="Apply saved view"
            defaultValue=""
            onChange={(event) => {
              const view = savedViews.find((item) => item.id === event.target.value);
              if (view) applyView(view);
              event.currentTarget.value = "";
            }}
          >
            <option value="">Apply a view…</option>
            {savedViews.map((view) => (
              <option key={view.id} value={view.id}>
                {viewLabel(view)}
              </option>
            ))}
          </select>
          <input
            aria-label="Saved view name"
            value={viewName}
            onChange={(event) => setViewName(event.target.value)}
            placeholder="View name"
          />
          <label className="inline-check">
            <input
              type="checkbox"
              checked={viewIsDefault}
              onChange={(event) => setViewIsDefault(event.target.checked)}
            />
            Make this my default view
          </label>
          <button className="secondary" onClick={() => void saveView()} disabled={!viewName.trim()}>
            Save current view
          </button>
          {savedViews.length > 0 && (
            <select
              aria-label="Delete saved view"
              defaultValue=""
              onChange={(event) => {
                if (event.target.value) void deleteView(event.target.value);
                event.currentTarget.value = "";
              }}
            >
              <option value="">Delete a view…</option>
              {savedViews.map((view) => (
                <option key={view.id} value={view.id}>
                  {viewLabel(view)}
                </option>
              ))}
            </select>
          )}
          <small className="saved-views-note">
            {defaultView
              ? `Default view: ${defaultView.name} (applied when the list opens)`
              : "No default view yet."}
          </small>
        </div>
        {selected.size > 0 && (
          <div className="bulk-toolbar" role="status">
            <strong>{selected.size} selected</strong>
            <button
              onClick={() => setAssignNotify(true)}
              disabled={!hasCapability(session, "Work.AssignVendor")}
            >
              Assign &amp; notify
            </button>
            <button
              className="secondary"
              onClick={() => {
                setBulkAction("vendor");
                setVendorId("");
                setFlow("choose");
              }}
              disabled={!hasCapability(session, "Work.AssignVendor")}
            >
              Assign vendor only
            </button>
            <button
              onClick={() => {
                setBulkAction("employee");
                setEmployeeId("");
                setFlow("choose");
              }}
              disabled={!hasCapability(session, "Work.AssignEmployee")}
            >
              Assign employee
            </button>
            <button
              onClick={() => {
                setBulkAction("message");
                setTemplateId("");
                setFlow("choose");
              }}
              disabled={!hasCapability(session, "Communications.SendMessage")}
            >
              Send resident message
            </button>
            <button
              className="secondary"
              onClick={() => setBulkEdit(true)}
              disabled={!hasCapability(session, "Work.Update")}
            >
              Bulk edit…
            </button>
            <button className="secondary" onClick={() => setSelected(new Set())}>
              Clear selection
            </button>
          </div>
        )}
        <div className="queue-list-heading">
          <div>
            <h2>Active work</h2>
            <p>{loading ? "Updating queue…" : `${work.length} items in this view`}</p>
          </div>
          <span className="queue-legend">
            <i className="legend-dot urgent" /> urgent <i className="legend-dot" /> routine
          </span>
        </div>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>
                  <input
                    aria-label="Select all visible work"
                    type="checkbox"
                    checked={allSelected}
                    onChange={() =>
                      setSelected(allSelected ? new Set() : new Set(work.map((item) => item.id)))
                    }
                  />
                </th>
                <SortHeader
                  label="Title"
                  sort="title"
                  active={query.sort}
                  descending={query.descending}
                  onSort={sortBy}
                />
                <SortHeader
                  label="Status"
                  sort="status"
                  active={query.sort}
                  descending={query.descending}
                  onSort={sortBy}
                />
                <SortHeader
                  label="Priority"
                  sort="priority"
                  active={query.sort}
                  descending={query.descending}
                  onSort={sortBy}
                />
                <th>Vendor</th>
                <th>Next action</th>
                <SortHeader
                  label="Due"
                  sort="dueDate"
                  active={query.sort}
                  descending={query.descending}
                  onSort={sortBy}
                />
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={8}>Loading work queue…</td>
                </tr>
              ) : work.length === 0 ? (
                <tr>
                  <td colSpan={8}>
                    <div className="queue-empty">
                      <strong>No work matches this queue.</strong>
                      <span>
                        Try another saved view or reset the filters to see active operations work.
                      </span>
                    </div>
                  </td>
                </tr>
              ) : (
                work.map((item) => (
                  <tr key={item.id}>
                    <td data-label="Select">
                      <input
                        aria-label={`Select ${item.title}`}
                        type="checkbox"
                        checked={selected.has(item.id)}
                        onChange={() => toggle(item.id)}
                      />
                    </td>
                    <td data-label="Work">
                      <Link href={returnToHref("/work", currentLocation.replace(/^\/work/, ""), `/work/${item.id}`)}>
                        <strong>{item.title}</strong>
                      </Link>
                      {item.propertyName && <small>{item.propertyName}</small>}
                    </td>
                    <td data-label="Status">
                      <span className={`badge ${statusClass(item.status)}`}>{item.status}</span>
                    </td>
                    <td data-label="Priority">
                      <span className={`priority ${priorityClass(item.priority, item.status)} `}>
                        {item.priority}
                      </span>
                    </td>
                    <td data-label="Vendor">{item.vendorName ?? "Unassigned"}</td>
                    <td data-label="Due">{formatDate(item.dueDate)}</td>
                    <td data-label="Next action">
                      <Link className="row-action" href={returnToHref("/work", currentLocation.replace(/^\/work/, ""), `/work/${item.id}`)}>
                        {nextAction(item)} →
                      </Link>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </section>
      {assignNotify && (
        <AssignNotifyFlow
          session={session}
          items={visibleSelected}
          vendors={vendors}
          onClose={(reload) => {
            setAssignNotify(false);
            if (reload) {
              setSelected(new Set());
              void loadWork();
            }
          }}
        />
      )}
      {bulkEdit && (
        <BulkEditFlow
          session={session}
          items={visibleSelected}
          onClose={(reload) => {
            setBulkEdit(false);
            if (reload) {
              setSelected(new Set());
              void loadWork();
            }
          }}
        />
      )}
      {flow && (
        <div className="modal-backdrop" role="presentation">
          <section
            className="modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="assignment-title"
          >
            {flow === "choose" && (
              <>
                <h2 id="assignment-title">{actionLabel}</h2>
                <p>
                  {bulkAction === "message"
                    ? "Send a resident message for"
                    : "Apply this assignment to"}{" "}
                  {visibleSelected.length} selected work{" "}
                  {visibleSelected.length === 1 ? "item" : "items"}.
                </p>
                {bulkAction === "vendor" && (
                  <label>
                    Vendor
                    <select value={vendorId} onChange={(event) => setVendorId(event.target.value)}>
                      <option value="">Choose a vendor</option>
                      {vendors.map((vendor) => (
                        <option key={vendor.id} value={vendor.id}>
                          {vendor.name}
                        </option>
                      ))}
                    </select>
                  </label>
                )}
                {bulkAction === "employee" && (
                  <label>
                    Employee
                    <select
                      value={employeeId}
                      onChange={(event) => setEmployeeId(event.target.value)}
                    >
                      <option value="">Choose an employee</option>
                      {employees.map((employee) => (
                        <option key={employee.id} value={employee.id}>
                          {employee.displayName}
                        </option>
                      ))}
                    </select>
                  </label>
                )}
                {bulkAction === "message" && (
                  <label>
                    Message template
                    <select
                      value={templateId}
                      onChange={(event) => setTemplateId(event.target.value)}
                    >
                      <option value="">Choose a template</option>
                      {templates.map((template) => (
                        <option key={template.id} value={template.id}>
                          {template.name} ({template.channel})
                        </option>
                      ))}
                    </select>
                  </label>
                )}
                <div className="modal-actions">
                  <button className="secondary" onClick={() => setFlow(null)}>
                    Cancel
                  </button>
                  <button
                    disabled={
                      bulkAction === "vendor"
                        ? !vendorId
                        : bulkAction === "employee"
                          ? !employeeId
                          : !templateId
                    }
                    onClick={() => setFlow("confirm")}
                  >
                    Continue
                  </button>
                </div>
              </>
            )}
            {flow === "confirm" && (
              <>
                <h2 id="assignment-title">Confirm {actionLabel.toLowerCase()}</h2>
                <p>
                  <strong>
                    {bulkAction === "vendor"
                      ? chosenVendor?.name
                      : bulkAction === "employee"
                        ? chosenEmployee?.displayName
                        : chosenTemplate?.name}
                  </strong>{" "}
                  {bulkAction === "message"
                    ? "will be rendered and queued for"
                    : "will be applied to"}{" "}
                  {visibleSelected.length} work {visibleSelected.length === 1 ? "item" : "items"}.
                  {bulkAction === "message"
                    ? " Every selected resident must be contactable through the template channel."
                    : " This updates assignment history for each item."}
                </p>
                <div className="modal-actions">
                  <button className="secondary" onClick={() => setFlow("choose")}>
                    Back
                  </button>
                  <button
                    aria-label={bulkAction === "message" ? "Confirm message" : "Confirm assignment"}
                    onClick={() => void runBulkAction()}
                  >
                    Confirm
                  </button>
                </div>
              </>
            )}
            {flow === "success" && (
              <>
                <h2 id="assignment-title">
                  {bulkAction === "message"
                    ? "Resident message queued"
                    : `${bulkAction === "vendor" ? "Vendor" : "Employee"} assigned`}
                </h2>
                <p>
                  {bulkAction === "message" ? "Queued" : "Assigned"}{" "}
                  {result?.changed ?? assignedTotal} of {assignedTotal}{" "}
                  {assignedTotal === 1 ? "work item" : "work items"}
                  {bulkAction === "vendor"
                    ? ` to ${chosenVendor?.name}`
                    : bulkAction === "employee"
                      ? ` to ${chosenEmployee?.displayName}`
                      : ""}
                  {result && result.unchanged > 0
                    ? ` (${result.unchanged} already had this ${bulkAction === "message" ? "message queued" : "assignment"})`
                    : ""}
                  . The list has been refreshed.
                </p>
                <div className="modal-actions">
                  <button
                    onClick={() => {
                      setSelected(new Set());
                      setFlow(null);
                    }}
                  >
                    Done
                  </button>
                </div>
              </>
            )}
          </section>
        </div>
      )}
    </section>
  );
}
function SortHeader({
  label,
  sort,
  active,
  descending,
  onSort,
}: {
  label: string;
  sort: Sort;
  active?: string;
  descending?: boolean;
  onSort: (sort: Sort) => void;
}) {
  return (
    <th>
      <button className="sort-button" onClick={() => onSort(sort)}>
        {label}
        {active === sort ? (descending ? " ↓" : " ↑") : ""}
      </button>
    </th>
  );
}
function viewLabel(view: SavedView) {
  return view.isDefault ? `${view.name} (default)` : view.name;
}
function formatDate(value?: string | null) {
  return value
    ? new Intl.DateTimeFormat(undefined, {
        month: "short",
        day: "numeric",
        year: "numeric",
      }).format(new Date(value))
    : "—";
}

function statusClass(status: string) {
  return status === "Completed" ? "badge-complete" : status === "New" ? "badge-urgent" : "";
}

function priorityClass(priority: string, status: string) {
  return status === "Completed" || status === "Cancelled"
    ? ""
    : priority === "High" || priority === "Critical"
      ? "priority-urgent"
      : "";
}

function nextAction(item: WorkItem) {
  if (["Completed", "Cancelled"].includes(item.status)) return "Review outcome";
  if (!item.vendorName && !item.employeeId) return "Assign owner";
  if (item.status === "New") return "Set schedule";
  if (item.status === "Scheduled") return "Confirm visit";
  if (item.status === "OnHold") return "Resolve blocker";
  if (item.status === "InProgress") return "Post update";
  return "Open work";
}
