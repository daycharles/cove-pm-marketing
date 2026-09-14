"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { AppShell } from "../components/app-shell";
import { ProtectedPage } from "../components/protected-page";
import { api, type CalendarEvent, type Portfolio, type PropertyReference, type Session } from "../../lib/api";

const views = ["day", "week", "2-week", "month"] as const;
type View = typeof views[number];
const pad = (n: number) => String(n).padStart(2, "0");
const iso = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
const startOfWeek = (d: Date) => { const copy = new Date(d); copy.setDate(copy.getDate() - copy.getDay()); return copy; };

export default function CalendarPage() {
  return <ProtectedPage capability="Work.Read">{(session) => <CalendarWorkspace session={session} />}</ProtectedPage>;
}

function CalendarWorkspace({ session }: { session: Session }) {
  const [anchor, setAnchor] = useState(() => iso(new Date()));
  const [view, setView] = useState<View>("month");
  const [propertyId, setPropertyId] = useState("");
  const [portfolioId, setPortfolioId] = useState("");
  const [portfolios, setPortfolios] = useState<Portfolio[]>([]);
  const [properties, setProperties] = useState<PropertyReference[]>([]);
  const [events, setEvents] = useState<CalendarEvent[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const range = useMemo(() => {
    const d = new Date(`${anchor}T12:00:00`); const from = view === "day" ? d : startOfWeek(d);
    const days = view === "day" ? 0 : view === "week" ? 6 : view === "2-week" ? 13 : 41;
    const to = new Date(from); to.setDate(to.getDate() + days); return { from: iso(from), to: iso(to) };
  }, [anchor, view]);
  useEffect(() => { void Promise.all([api.properties.list(), api.portfolios.list()]).then(([nextProperties, nextPortfolios]) => { setProperties(nextProperties); setPortfolios(nextPortfolios); }).catch(() => setError("Unable to load calendar filters.")); }, []);
  useEffect(() => { const query = new URLSearchParams({ from: range.from, to: range.to }); if (propertyId) query.set("propertyId", propertyId); if (portfolioId) query.set("portfolioId", portfolioId); api.calendar.list(query.toString()).then(setEvents).catch((e) => setError(e instanceof Error ? e.message : "Unable to load calendar.")).finally(() => setLoading(false)); }, [range, propertyId, portfolioId]);
  const byDate = new Map<string, CalendarEvent[]>(); events.forEach((event) => byDate.set(event.date, [...(byDate.get(event.date) ?? []), event]));
  const days = Array.from({ length: view === "month" ? 42 : (view === "day" ? 1 : view === "week" ? 7 : 14) }, (_, i) => { const d = new Date(`${range.from}T12:00:00`); d.setDate(d.getDate() + i); return iso(d); });
  const move = (delta: number) => { const d = new Date(`${anchor}T12:00:00`); d.setDate(d.getDate() + delta); setAnchor(iso(d)); };
  return <AppShell session={session}><section className="detail-workspace"><div className="work-heading"><div><p className="eyebrow">Leasing · operations</p><h1>Lifecycle calendar</h1><p>Keep move-ins, notices, inspections, turns, and work deadlines in context.</p></div><button className="secondary" onClick={() => setAnchor(iso(new Date()))}>Today</button></div>
    <section className="panel"><div className="form-grid"><label>Portfolio<select value={portfolioId} onChange={(e) => { setPortfolioId(e.target.value); setPropertyId(""); }}><option value="">All portfolios</option>{portfolios.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}</select></label><label>Property<select value={propertyId} onChange={(e) => setPropertyId(e.target.value)}><option value="">All properties</option>{properties.filter((p) => !portfolioId || p.portfolioId === portfolioId).map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}</select></label><label>View<select value={view} onChange={(e) => setView(e.target.value as View)}>{views.map((x) => <option key={x} value={x}>{x}</option>)}</select></label></div><div className="calendar-toolbar"><button className="secondary" onClick={() => move(view === "day" ? -1 : view === "week" ? -7 : view === "2-week" ? -14 : -28)}>Previous</button><strong>{range.from} — {range.to}</strong><button className="secondary" onClick={() => move(view === "day" ? 1 : view === "week" ? 7 : view === "2-week" ? 14 : 28)}>Next</button></div></section>
    {error && <p className="message" role="alert">{error}</p>}{loading ? <section className="panel"><p>Loading lifecycle events…</p></section> : <section className={`panel calendar-grid calendar-${view}`}><div className="calendar-weekdays">{["Sun","Mon","Tue","Wed","Thu","Fri","Sat"].map((x) => <span key={x}>{x}</span>)}</div><div className="calendar-days">{days.map((day) => <div className="calendar-day" key={day}><h3>{day}</h3>{(byDate.get(day) ?? []).map((event) => <Link className={`calendar-event calendar-event-${event.type.toLowerCase()}`} href={event.href} key={event.id}><strong>{event.title}</strong><span>{event.propertyName}{event.residentName ? ` · ${event.residentName}` : ""}</span>{event.startsAt && <small>{new Date(event.startsAt).toLocaleTimeString([], { hour: "numeric", minute: "2-digit" })} · {event.timeZoneId}</small>}</Link>)}</div>)}</div></section>}
  </section></AppShell>;
}
