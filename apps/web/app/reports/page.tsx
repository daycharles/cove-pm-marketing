"use client";

import { useEffect, useMemo, useState, type FormEvent } from "react";
import { AppShell } from "../components/app-shell";
import { ProtectedPage } from "../components/protected-page";
import { api, type ReportKind, type ReportResult, type ReportSchedule, type ReportDelivery, type Session } from "../../lib/api";

type CatalogGroup = { label: string; description: string; reports: { kind: ReportKind; label: string }[] };
const catalog: CatalogGroup[] = [
  { label: "Operations", description: "Work health and open operational demand.", reports: [{ kind: "operational", label: "Operational overview" }, { kind: "maintenance", label: "Maintenance activity" }] },
  { label: "Leasing & occupancy", description: "Lease activity and the occupied portfolio.", reports: [{ kind: "leasing", label: "Lease activity" }, { kind: "occupancy", label: "Occupancy snapshot" }] },
  { label: "Vendors & finance", description: "Vendor performance and money movement.", reports: [{ kind: "vendor", label: "Vendor performance" }, { kind: "financial", label: "Financial activity" }] },
  { label: "Portfolio", description: "A concise view across properties.", reports: [{ kind: "portfolio", label: "Portfolio summary" }] },
];
const today = new Date().toISOString().slice(0, 10);
const monthAgo = new Date(Date.now() - 30 * 86400000).toISOString().slice(0, 10);
const titleFor = (kind: ReportKind) => catalog.flatMap((group) => group.reports).find((report) => report.kind === kind)?.label ?? kind;
const display = (key: string, value: number) => key.toLowerCase().includes("cost") || key.toLowerCase().includes("rent") || key.toLowerCase().includes("charge") || key.toLowerCase().includes("payment") ? new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(value) : value.toLocaleString();

export default function ReportsPage() {
  return <ProtectedPage capability="Reports.Read">{(session) => <ReportsWorkspace session={session} />}</ProtectedPage>;
}

function ReportsWorkspace({ session }: { session: Session }) {
  const canSchedule = session.capabilities.includes("Settings.ManageConfiguration");
  const [kind, setKind] = useState<ReportKind>("operational");
  const [filters, setFilters] = useState({ from: monthAgo, to: today, propertyId: "" });
  const [result, setResult] = useState<ReportResult | null>(null);
  const [properties, setProperties] = useState<{ id: string; name: string }[]>([]);
  const [schedules, setSchedules] = useState<ReportSchedule[]>([]);
  const [deliveries, setDeliveries] = useState<Record<string, ReportDelivery[]>>({});
  const [selectedSchedule, setSelectedSchedule] = useState("");
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [form, setForm] = useState({ name: "", frequency: "Weekly" as ReportSchedule["frequency"], recipient: "", nextRunAt: "" });

  async function loadSchedules() { if (!canSchedule) return; setSchedules(await api.reports.schedules.list()); }
  async function runReport(event?: FormEvent) {
    event?.preventDefault(); setLoading(true); setError(""); setMessage("");
    try { setResult(await api.reports.get(kind, { ...filters, take: 1000 })); setMessage(`${titleFor(kind)} is ready.`); }
    catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to run report."); }
    finally { setLoading(false); }
  }
  useEffect(() => { const timer = window.setTimeout(() => { void api.properties.list().then((items) => setProperties(items)).catch(() => setProperties([])); void loadSchedules().catch(() => setError("Unable to load saved schedules.")); }, 0); return () => window.clearTimeout(timer); }, []);
  useEffect(() => { const timer = window.setTimeout(() => void runReport(), 0); return () => window.clearTimeout(timer); }, [kind]);
  async function saveSchedule(event: FormEvent) {
    event.preventDefault(); setError("");
    try { await api.reports.schedules.create({ name: form.name, kind, frequency: form.frequency, recipient: form.recipient, format: "Csv", nextRunAt: form.nextRunAt ? new Date(form.nextRunAt).toISOString() : new Date().toISOString(), filters: { from: filters.from, to: filters.to, ...(filters.propertyId ? { propertyId: filters.propertyId } : {}) } }); setForm({ ...form, name: "", recipient: "" }); setMessage("Schedule saved."); await loadSchedules(); }
    catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to save schedule."); }
  }
  async function scheduleAction(action: () => Promise<unknown>, success: string) { setError(""); try { await action(); setMessage(success); await loadSchedules(); } catch (cause) { setError(cause instanceof Error ? cause.message : "Schedule action failed."); } }
  async function showDeliveries(id: string) { setSelectedSchedule(selectedSchedule === id ? "" : id); if (!deliveries[id]) setDeliveries({ ...deliveries, [id]: await api.reports.schedules.deliveries(id) }); }
  const totalEntries = result?.totals && !Array.isArray(result.totals) ? Object.entries(result.totals) : [];
  const rowKeys = useMemo(() => result?.rows?.length ? Object.keys(result.rows[0]) : [], [result]);

  return <AppShell session={session}>
    <section className="panel"><div className="work-heading"><div><p className="eyebrow">Insights / Reporting</p><h1>Reports hub</h1><p>Find a trusted report, apply shared filters, and keep recurring delivery visible to the team.</p></div><span className="status status-success">Tenant scoped</span></div>{message && <p className="message">{message}</p>}{error && <p className="message" role="alert">{error}</p>}</section>
    <section className="panel"><div className="section-heading"><div><p className="eyebrow">Report library</p><h2>Choose a report</h2></div><span className="muted">7 report views</span></div><div className="report-catalog">{catalog.map((group) => <div className="report-group" key={group.label}><p className="eyebrow">{group.label}</p><p>{group.description}</p>{group.reports.map((report) => <button key={report.kind} className={kind === report.kind ? "report-choice active" : "report-choice"} type="button" onClick={() => setKind(report.kind)}><strong>{report.label}</strong><span>{report.kind === "financial" ? "Charges and settled payments" : "Open this projection"}</span></button>)}</div>)}</div></section>
    <section className="panel"><div className="section-heading"><div><p className="eyebrow">Shared filters</p><h2>{titleFor(kind)}</h2></div><div><button className="secondary" type="button" onClick={() => window.location.assign(api.reports.exportUrl(kind, filters))}>Export CSV</button> <button type="button" onClick={() => void runReport()} disabled={loading}>{loading ? "Running…" : "Run report"}</button></div></div><form className="form-grid" onSubmit={(event) => void runReport(event)}><label>From<input type="date" value={filters.from} onChange={(e) => setFilters({ ...filters, from: e.target.value })} /></label><label>To<input type="date" value={filters.to} onChange={(e) => setFilters({ ...filters, to: e.target.value })} /></label><label>Property<select value={filters.propertyId} onChange={(e) => setFilters({ ...filters, propertyId: e.target.value })}><option value="">All properties</option>{properties.map((property) => <option key={property.id} value={property.id}>{property.name}</option>)}</select></label></form>{result && <><div className="report-totals">{totalEntries.map(([key, value]) => <div className="metric" key={key}><span>{key.replace(/[A-Z]/g, (letter) => ` ${letter}`).replace(/^./, (letter) => letter.toUpperCase())}</span><strong>{display(key, value)}</strong></div>)}{Array.isArray(result.totals) && result.totals.map((row, index) => <div className="metric" key={index}><span>Vendor {String(row.vendorId ?? "") .slice(0, 8)}</span><strong>{String(row.workItems ?? 0)} work orders · {display("cost", Number(row.cost ?? 0))}</strong></div>)}</div><div className="table-wrap"><table><thead><tr>{rowKeys.map((key) => <th key={key}>{key}</th>)}</tr></thead><tbody>{result.rows.map((row, index) => <tr key={String(row.id ?? index)}>{rowKeys.map((key) => <td key={key}>{row[key] == null ? "—" : String(row[key])}</td>)}</tr>)}{!result.rows.length && <tr><td colSpan={Math.max(rowKeys.length, 1)}>This report is summarized above; there are no detail rows for this view.</td></tr>}</tbody></table></div></>}</section>
    {canSchedule && <section className="panel"><div className="section-heading"><div><p className="eyebrow">Saved delivery</p><h2>Schedules</h2></div><span className="muted">Configuration managers only</span></div><form className="form-grid" onSubmit={(event) => void saveSchedule(event)}><label>Name<input required value={form.name} placeholder="Weekly operations" onChange={(e) => setForm({ ...form, name: e.target.value })} /></label><label>Frequency<select value={form.frequency} onChange={(e) => setForm({ ...form, frequency: e.target.value as ReportSchedule["frequency"] })}><option>Daily</option><option>Weekly</option><option>Monthly</option></select></label><label>Recipient email<input required type="email" value={form.recipient} placeholder="manager@example.com" onChange={(e) => setForm({ ...form, recipient: e.target.value })} /></label><label>First run<input type="datetime-local" value={form.nextRunAt} onChange={(e) => setForm({ ...form, nextRunAt: e.target.value })} /></label><div><button type="submit">Save schedule</button></div></form><div className="table-wrap"><table><thead><tr><th>Name</th><th>Report</th><th>Cadence</th><th>Next run</th><th>Status</th><th>History</th><th>Actions</th></tr></thead><tbody>{schedules.map((schedule) => <tr key={schedule.id}><td><strong>{schedule.name}</strong><br /><small>{schedule.recipient}</small></td><td>{titleFor(schedule.kind)}</td><td>{schedule.frequency}</td><td>{new Date(schedule.nextRunAt).toLocaleString()}</td><td>{schedule.isActive ? "Active" : "Paused"}</td><td><button className="secondary" type="button" onClick={() => void showDeliveries(schedule.id)}>{schedule.deliveryCount} deliveries</button></td><td>{schedule.isActive ? <button className="secondary" type="button" onClick={() => void scheduleAction(() => api.reports.schedules.pause(schedule.id), "Schedule paused.")}>Pause</button> : "—"} <button className="secondary" type="button" onClick={() => void scheduleAction(() => api.reports.schedules.run(schedule.id), "Delivery recorded.")}>Run now</button></td></tr>)}{!schedules.length && <tr><td colSpan={7}>No saved schedules yet. Save the current report above to start one.</td></tr>}</tbody></table></div>{selectedSchedule && <div className="detail-card"><h3>Delivery history</h3>{(deliveries[selectedSchedule] ?? []).map((delivery) => <p key={delivery.id}><strong>{new Date(delivery.deliveredAt).toLocaleString()}</strong> · {delivery.rowCount} rows · {delivery.format} · {delivery.recipient} · <code>{delivery.payloadHash.slice(0, 12)}</code></p>)}{!deliveries[selectedSchedule]?.length && <p>No deliveries recorded.</p>}</div>}</section>}
  </AppShell>;
}
