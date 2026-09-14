"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { AppShell } from "../components/app-shell";
import { ProtectedPage } from "../components/protected-page";
import { api, type Inspection, type InspectionDetail, type InspectionTemplate, type PropertyReference, type Session, type UnitTurn, type UnitTurnDetail, type UnitTurnTask } from "../../lib/api";

export default function InspectionsPage() {
  return <ProtectedPage capability="Work.Read">{(session) => <Workspace session={session} />}</ProtectedPage>;
}

function Workspace({ session }: { session: Session }) {
  const [properties, setProperties] = useState<PropertyReference[]>([]);
  const [templates, setTemplates] = useState<InspectionTemplate[]>([]);
  const [inspections, setInspections] = useState<Inspection[]>([]);
  const [turns, setTurns] = useState<UnitTurn[]>([]);
  const [selected, setSelected] = useState<InspectionDetail | null>(null);
  const [turnDetail, setTurnDetail] = useState<UnitTurnDetail | null>(null);
  const [propertyId, setPropertyId] = useState("");
  const [spaceId, setSpaceId] = useState("");
  const [spaces, setSpaces] = useState<{ id: string; code: string }[]>([]);
  const [templateId, setTemplateId] = useState("");
  const [kind, setKind] = useState<Inspection["kind"]>("MoveOut");
  const [targetReadyOn, setTargetReadyOn] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);

  async function load() {
    setLoading(true); setError("");
    try {
      const [nextProperties, nextTemplates, nextInspections, nextTurns] = await Promise.all([
        api.properties.list(), api.inspections.templates(), api.inspections.list(propertyId ? `propertyId=${propertyId}` : ""), api.unitTurns.list(propertyId ? `propertyId=${propertyId}` : ""),
      ]);
      setProperties(nextProperties); setTemplates(nextTemplates); setInspections(nextInspections); setTurns(nextTurns);
      if (!templateId && nextTemplates[0]) setTemplateId(nextTemplates[0].id);
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to load inspections."); }
    finally { setLoading(false); }
  }
  useEffect(() => { setSpaceId(""); if (propertyId) void api.properties.get(propertyId).then((detail) => setSpaces(detail.spaces)).catch(() => setSpaces([])); else setSpaces([]); void load(); }, [propertyId]);
  const propertyName = (id: string) => properties.find((x) => x.id === id)?.name ?? "Property";
  const openCount = (turn: UnitTurn) => turn.status === "Ready" ? "Ready" : turn.status === "Planned" ? "Not started" : "Tasks pending";
  async function refreshInspection(id: string) { setSelected(await api.inspections.get(id)); }
  async function action(action: () => Promise<unknown>) { try { setError(""); await action(); await load(); } catch (cause) { setError(cause instanceof Error ? cause.message : "Action could not be completed."); } }

  return <AppShell session={session}><section className="detail-workspace">
    <div className="work-heading"><div><p className="eyebrow">Operations · quality & turnover</p><h1>Inspections & make-ready</h1><p>Turn inspection findings into accountable unit-turn work, with a clear path to ready.</p></div><button className="secondary" onClick={() => void load()} disabled={loading}>{loading ? "Updating…" : "Refresh"}</button></div>
    {error && <p className="message" role="alert">{error}</p>}
    <section className="panel"><div className="form-grid"><label>Property<select value={propertyId} onChange={(e) => setPropertyId(e.target.value)}><option value="">All properties</option>{properties.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}</select></label><label>Unit (optional)<select value={spaceId} onChange={(e) => setSpaceId(e.target.value)} disabled={!propertyId}><option value="">Property-wide</option>{spaces.map((x) => <option key={x.id} value={x.id}>{x.code}</option>)}</select></label><label>Inspection template<select value={templateId} onChange={(e) => setTemplateId(e.target.value)}>{templates.map((t) => <option key={t.id} value={t.id}>{t.name} · v{t.version}</option>)}</select></label><label>Inspection kind<select value={kind} onChange={(e) => setKind(e.target.value as Inspection["kind"])}><option value="MoveOut">Move-out</option><option value="MoveIn">Move-in</option><option value="Routine">Routine</option></select></label></div><button onClick={() => propertyId && templateId ? void action(() => api.inspections.create({ propertyId, spaceId: spaceId || null, templateId, kind })) : setError("Choose a property and inspection template first.")}>Start inspection</button></section>
    <div className="detail-grid"><section className="panel"><div className="work-heading"><div><h2>Inspection queue</h2><p className="muted">{inspections.length} inspections · findings and approval stay linked to the unit.</p></div></div>{loading ? <p>Loading…</p> : inspections.length === 0 ? <p>No inspections match this property.</p> : <div className="stack-list">{inspections.map((item) => <button className={`stack-card ${selected?.item.id === item.id ? "selected" : ""}`} key={item.id} onClick={() => void refreshInspection(item.id)}><span><strong>{item.kind} inspection</strong><small>{propertyName(item.propertyId)} · {item.spaceId ? `Unit ${item.spaceId.slice(0, 8)}` : "Property-wide"}</small></span><span className={`badge status-${item.status.toLowerCase()}`}>{item.status}</span></button>)}</div>}</section>
      <section className="panel"><div className="work-heading"><div><h2>Make-ready board</h2><p className="muted">Units show their next action and why they are not ready.</p></div></div>{turns.length === 0 ? <p>No unit turns have been created.</p> : <div className="stack-list">{turns.map((turn) => <button className={`stack-card ${turnDetail?.turn.id === turn.id ? "selected" : ""}`} key={turn.id} onClick={() => void api.unitTurns.get(turn.id).then(setTurnDetail).catch((e) => setError(e.message))}><span><strong>{propertyName(turn.propertyId)} · Unit {turn.spaceId.slice(0, 8)}</strong><small>Ready target {turn.targetReadyOn} · {openCount(turn)}</small></span><span className={`badge status-${turn.status.toLowerCase()}`}>{turn.status}</span></button>)}</div>}</section></div>
    {selected && <section className="panel"><div className="work-heading"><div><p className="eyebrow">Checklist execution</p><h2>{selected.item.kind} inspection</h2><p>{propertyName(selected.item.propertyId)} · status {selected.item.status}</p></div><div>{selected.item.status === "Draft" && <button onClick={() => void action(async () => { await api.inspections.start(selected.item.id); await refreshInspection(selected.item.id); })}>Begin checklist</button>}{selected.item.status === "InProgress" && <button onClick={() => void action(async () => { await api.inspections.complete(selected.item.id); await refreshInspection(selected.item.id); })}>Complete inspection</button>}{selected.item.status === "Completed" && <button onClick={() => void action(async () => { await api.inspections.approve(selected.item.id); await refreshInspection(selected.item.id); })}>Approve</button>}</div></div><div className="panel nested-panel"><h3>Template checklist</h3><ul>{JSON.parse(templates.find((x) => x.id === selected.item.templateId)?.checklistJson ?? "[]").map((item: string) => <li key={item}>{item}</li>)}</ul></div>{selected.findings.length === 0 ? <p>No findings recorded. The inspection is clear to approve.</p> : <div className="table-wrap"><table><thead><tr><th>Area / finding</th><th>Severity</th><th>Status</th><th>Next action</th></tr></thead><tbody>{selected.findings.map((finding) => <tr key={finding.id}><td><strong>{finding.area}</strong><br /><span className="muted">{finding.description}</span>{finding.photoAttachmentIdsJson !== "[]" && <small> · photo evidence attached</small>}</td><td>{finding.severity}</td><td>{finding.status}</td><td>{finding.status === "Open" ? <button className="secondary" onClick={() => void action(async () => { await api.inspections.resolveFinding(selected.item.id, finding.id); await refreshInspection(selected.item.id); })}>Resolve</button> : "Closed"}</td></tr>)}</tbody></table></div>}{selected.item.kind === "MoveOut" && ["Completed", "Approved"].includes(selected.item.status) && selected.item.spaceId && <div className="panel nested-panel"><h3>Create unit turn from this inspection</h3><div className="form-grid"><label>Target ready date<input type="date" value={targetReadyOn} onChange={(e) => setTargetReadyOn(e.target.value)} /></label></div><button onClick={() => targetReadyOn ? void action(() => api.unitTurns.create({ spaceId: selected.item.spaceId!, moveOutInspectionId: selected.item.id, targetReadyOn })) : setError("Choose a target ready date.")}>Create make-ready plan</button></div>}</section>}
    {turnDetail && <section className="panel"><div className="work-heading"><div><p className="eyebrow">Unit-turn detail</p><h2>Next actions</h2><p>{turnDetail.tasks.filter((x) => x.status !== "Completed").length} incomplete tasks block ready state.</p></div><div>{turnDetail.turn.status === "Planned" && <button onClick={() => void action(async () => { await api.unitTurns.start(turnDetail.turn.id); setTurnDetail(await api.unitTurns.get(turnDetail.turn.id)); })}>Start turn</button>}{turnDetail.turn.status === "InProgress" && <button onClick={() => void action(async () => { await api.unitTurns.ready(turnDetail.turn.id); setTurnDetail(await api.unitTurns.get(turnDetail.turn.id)); })}>Mark ready</button>}</div></div>{turnDetail.tasks.length === 0 ? <p>No blockers were generated by the inspection.</p> : <div className="table-wrap"><table><thead><tr><th>Task / blocker</th><th>Status</th><th>Owner / next action</th></tr></thead><tbody>{turnDetail.tasks.map((task) => <tr key={task.id}><td>{task.workId ? <Link href={`/work/${task.workId}`}>{task.title}</Link> : task.title}</td><td><span className={`badge status-${task.status.toLowerCase()}`}>{task.status}</span></td><td>{task.status === "Completed" ? "Done" : <select aria-label={`Status for ${task.title}`} value={task.status} onChange={(e) => void action(async () => { await api.unitTurns.taskStatus(turnDetail.turn.id, task.id, e.target.value as UnitTurnTask["status"]); setTurnDetail(await api.unitTurns.get(turnDetail.turn.id)); })}><option>Pending</option><option>InProgress</option><option>Blocked</option><option>Completed</option></select>}</td></tr>)}</tbody></table></div>}</section>}
  </section></AppShell>;
}
