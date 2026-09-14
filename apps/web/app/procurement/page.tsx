"use client";

import { useEffect, useMemo, useState, type FormEvent } from "react";
import { AppShell } from "../components/app-shell";
import { ProtectedPage } from "../components/protected-page";
import { api, type ProcurementPurchaseOrder, type PropertyReference, type Vendor, type VendorDocument, type WorkItem } from "../../lib/api";

const money = (value: number) => new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(value);
const today = () => new Date().toISOString().slice(0, 10);

export default function ProcurementPage() {
  return <ProtectedPage capability="Procurement.Read">{(session) => <ProcurementContent session={session} />}</ProtectedPage>;
}

function ProcurementContent({ session }: { session: import("../../lib/api").Session }) {
  const [vendors, setVendors] = useState<Vendor[]>([]);
  const [properties, setProperties] = useState<PropertyReference[]>([]);
  const [work, setWork] = useState<WorkItem[]>([]);
  const [orders, setOrders] = useState<ProcurementPurchaseOrder[]>([]);
  const [documents, setDocuments] = useState<VendorDocument[]>([]);
  const [vendorId, setVendorId] = useState("");
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [form, setForm] = useState({ number: "", amount: "", approvalThreshold: "500", propertyId: "", workItemId: "" });
  const selectedVendor = vendors.find((vendor) => vendor.id === vendorId);
  const selectedWork = work.find((item) => item.id === form.workItemId);
  const canManage = session.capabilities.includes("Procurement.Manage");
  const expiredDocument = useMemo(() => documents.some((doc) => doc.isActive && (doc.type === "Insurance" || doc.type === "License") && doc.expiresOn < today()), [documents]);

  async function refresh() {
    setError("");
    try {
      const [vendorList, propertyList, workPage, orderList] = await Promise.all([api.vendors.list(), api.properties.list(), api.work.list({ status: "New", pageSize: 100 }), api.procurement.purchaseOrders.list()]);
      setVendors(vendorList); setProperties(propertyList); setWork(workPage.items); setOrders(orderList);
      const nextVendor = vendorId || vendorList.find((vendor) => vendor.isActive)?.id || "";
      setVendorId(nextVendor);
      setDocuments(nextVendor ? await api.procurement.vendorDocuments(nextVendor) : []);
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to load procurement workspace."); }
  }
  useEffect(() => { const timer = window.setTimeout(() => void refresh(), 0); return () => window.clearTimeout(timer); }, []);
  useEffect(() => { if (vendorId) void api.procurement.vendorDocuments(vendorId).then(setDocuments).catch(() => setDocuments([])); }, [vendorId]);

  async function run(action: () => Promise<unknown>, success: string) {
    setError("");
    try { await action(); setMessage(success); await refresh(); } catch (cause) { setError(cause instanceof Error ? cause.message : "Procurement action failed."); }
  }
  async function createOrder(event: FormEvent) {
    event.preventDefault();
    if (!vendorId) return;
    await run(() => api.procurement.purchaseOrders.create({ vendorId, propertyId: form.propertyId || null, workItemId: form.workItemId || null, number: form.number, amount: Number(form.amount), approvalThreshold: Number(form.approvalThreshold) }), "Purchase order drafted.");
    setForm({ number: "", amount: "", approvalThreshold: "500", propertyId: "", workItemId: "" });
  }
  const statusAction = (order: ProcurementPurchaseOrder) => order.status === "Draft" ? ["Submit", () => api.procurement.purchaseOrders.submit(order.id)] as const : order.status === "PendingApproval" ? ["Approve", () => api.procurement.purchaseOrders.approve(order.id)] as const : order.status === "Approved" ? ["Issue", () => api.procurement.purchaseOrders.issue(order.id)] as const : null;

  return <AppShell session={session}>
    <section className="panel">
      <p className="eyebrow">Operations / Procurement</p><h1>Procurement workspace</h1>
      <p>Manage vendor compliance, compare work-order purchasing, and move purchase orders through explicit authorization.</p>
      {message && <p className="message">{message}</p>}{error && <p className="message" role="alert">{error}</p>}
      <div className="form-grid"><label>Vendor<select value={vendorId} onChange={(event) => setVendorId(event.target.value)}><option value="">Select a vendor</option>{vendors.filter((v) => v.isActive).map((vendor) => <option key={vendor.id} value={vendor.id}>{vendor.name}</option>)}</select></label><div><span className="eyebrow">Selected vendor</span><strong>{selectedVendor?.name ?? "—"}</strong></div></div>
    </section>
    <section className="panel"><div className="section-heading"><div><p className="eyebrow">Vendor controls</p><h2>Compliance documents</h2></div><span className={expiredDocument ? "status status-danger" : "status status-success"}>{expiredDocument ? "Issuance blocked" : "Ready for issuance"}</span></div>
      {expiredDocument && <p className="message" role="alert">An active insurance or license document is expired. Renew it before submitting a purchase order.</p>}
      <div className="table-wrap"><table><thead><tr><th>Type</th><th>Document</th><th>Expires</th><th>Status</th></tr></thead><tbody>{documents.map((doc) => <tr key={doc.id}><td>{doc.type}</td><td>{doc.documentNumber}</td><td>{doc.expiresOn}</td><td>{doc.isActive ? (doc.expiresOn < today() ? "Expired" : "Active") : "Inactive"}</td></tr>)}{!documents.length && <tr><td colSpan={4}>No compliance documents recorded.</td></tr>}</tbody></table></div>
    </section>
    {canManage && <section className="panel"><p className="eyebrow">Authorization</p><h2>Draft a purchase order</h2><form className="form-grid" onSubmit={(event) => void createOrder(event)}><label>PO number<input required value={form.number} onChange={(e) => setForm({ ...form, number: e.target.value })} placeholder="PO-2026-001" /></label><label>Amount<input required min="0.01" step="0.01" type="number" value={form.amount} onChange={(e) => setForm({ ...form, amount: e.target.value })} /></label><label>Approval threshold<input required min="0.01" step="0.01" type="number" value={form.approvalThreshold} onChange={(e) => setForm({ ...form, approvalThreshold: e.target.value })} /></label><label>Property<select value={form.propertyId} onChange={(e) => setForm({ ...form, propertyId: e.target.value })}><option value="">Any property</option>{properties.map((property) => <option key={property.id} value={property.id}>{property.name}</option>)}</select></label><label>Work order<select value={form.workItemId} onChange={(e) => setForm({ ...form, workItemId: e.target.value })}><option value="">Link a work order</option>{work.map((item) => <option key={item.id} value={item.id}>{item.title} · {item.propertyName ?? "Unassigned"}</option>)}</select></label><div>{selectedWork && <small>Linked: {selectedWork.title}</small>}<button type="submit" disabled={!vendorId}>Create draft</button></div></form></section>}
    <section className="panel"><div className="section-heading"><div><p className="eyebrow">Audit trail</p><h2>Purchase orders</h2></div><button className="secondary" type="button" onClick={() => void refresh()}>Refresh</button></div><div className="table-wrap"><table><thead><tr><th>Number</th><th>Vendor</th><th>Work order</th><th>Amount</th><th>Status</th><th>Action</th></tr></thead><tbody>{orders.map((order) => { const action = statusAction(order); return <tr key={order.id}><td>{order.number}</td><td>{vendors.find((v) => v.id === order.vendorId)?.name ?? order.vendorId.slice(0, 8)}</td><td>{work.find((item) => item.id === order.workItemId)?.title ?? (order.workItemId ? order.workItemId.slice(0, 8) : "—")}</td><td>{money(order.amount)}</td><td>{order.status}</td><td>{action && canManage ? <button className="secondary" type="button" disabled={expiredDocument && order.status === "Approved"} onClick={() => void run(action[1], `${action[0]}d purchase order.`)}>{action[0]}</button> : "—"}</td></tr>})}{!orders.length && <tr><td colSpan={6}>No purchase orders yet.</td></tr>}</tbody></table></div></section>
  </AppShell>;
}
