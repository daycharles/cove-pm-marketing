"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { AppShell } from "../components/app-shell";
import { ProtectedPage } from "../components/protected-page";
import { api, type ResidentDirectoryRow, type ResidentProfile, type Session } from "../../lib/api";

export default function ResidentsPage() {
  return <ProtectedPage capability="Work.Read">{(session) => <ResidentsWorkspace session={session} />}</ProtectedPage>;
}

function ResidentsWorkspace({ session }: { session: Session }) {
  const [rows, setRows] = useState<ResidentDirectoryRow[]>([]);
  const [profiles, setProfiles] = useState<ResidentProfile[]>([]);
  const [activeProfileId, setActiveProfileId] = useState<string | null>(null);
  const [profilesMinimized, setProfilesMinimized] = useState(false);
  const [q, setQ] = useState("");
  const [status, setStatus] = useState("");
  const [moveInFrom, setMoveInFrom] = useState("");
  const [moveInTo, setMoveInTo] = useState("");
  const [floor, setFloor] = useState("");
  const [room, setRoom] = useState("");
  const [noticeFrom, setNoticeFrom] = useState("");
  const [noticeTo, setNoticeTo] = useState("");
  const [leaseExpiresFrom, setLeaseExpiresFrom] = useState("");
  const [leaseExpiresTo, setLeaseExpiresTo] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);

  async function load() {
    setLoading(true); setError("");
    try {
      const params = new URLSearchParams();
      if (q.trim()) params.set("q", q.trim());
      if (status) params.set("status", status);
      if (moveInFrom) params.set("moveInFrom", moveInFrom);
      if (moveInTo) params.set("moveInTo", moveInTo);
      if (floor.trim()) params.set("floor", floor.trim());
      if (room.trim()) params.set("room", room.trim());
      if (noticeFrom) params.set("noticeFrom", noticeFrom);
      if (noticeTo) params.set("noticeTo", noticeTo);
      if (leaseExpiresFrom) params.set("leaseExpiresFrom", leaseExpiresFrom);
      if (leaseExpiresTo) params.set("leaseExpiresTo", leaseExpiresTo);
      setRows(await api.residents.directory(`?${params.toString()}`));
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to load residents."); }
    finally { setLoading(false); }
  }

  useEffect(() => { const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, [status, moveInFrom, moveInTo, floor, room, noticeFrom, noticeTo, leaseExpiresFrom, leaseExpiresTo]);

  async function openProfile(id: string) {
    const existing = profiles.find((item) => item.resident.id === id);
    if (existing) {
      setActiveProfileId(id);
      setProfilesMinimized(false);
      return;
    }
    try {
      const nextProfile = await api.residents.profile(id);
      setProfiles((current) => [...current, nextProfile]);
      setActiveProfileId(id);
      setProfilesMinimized(false);
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to load resident profile."); }
  }

  function closeProfile(id: string) {
    setProfiles((current) => {
      const next = current.filter((item) => item.resident.id !== id);
      if (activeProfileId === id) setActiveProfileId(next.at(-1)?.resident.id ?? null);
      if (next.length === 0) setProfilesMinimized(false);
      return next;
    });
  }

  const activeProfile = profiles.find((item) => item.resident.id === activeProfileId) ?? profiles[0] ?? null;

  return <AppShell session={session}><section className="detail-workspace">
    <div className="work-heading"><div><p className="eyebrow">People and occupancy</p><h1>Residents</h1><p>Find residents by name, contact, lease status, move-in dates, and unit context.</p></div><button className="secondary" onClick={() => void load()} disabled={loading}>{loading ? "Updating…" : "Refresh"}</button></div>
    <section className="panel"><div className="form-grid">
      <label>Search<input value={q} onChange={(e) => setQ(e.target.value)} onKeyDown={(e) => { if (e.key === "Enter") void load(); }} placeholder="Name, email, or phone" /></label>
      <label>Lease status<select value={status} onChange={(e) => setStatus(e.target.value)}><option value="">Any status</option><option>Active</option><option>Renewed</option><option>NoticeGiven</option><option>Ended</option><option>Draft</option></select></label>
      <label>Moved in from<input type="date" value={moveInFrom} onChange={(e) => setMoveInFrom(e.target.value)} /></label>
      <label>Moved in to<input type="date" value={moveInTo} onChange={(e) => setMoveInTo(e.target.value)} /></label>
      <label>Floor<input value={floor} onChange={(e) => setFloor(e.target.value)} placeholder="e.g. 3" /></label>
      <label>Room<input value={room} onChange={(e) => setRoom(e.target.value)} placeholder="e.g. 06" /></label>
      <label>Notice from<input type="date" value={noticeFrom} onChange={(e) => setNoticeFrom(e.target.value)} /></label>
      <label>Notice to<input type="date" value={noticeTo} onChange={(e) => setNoticeTo(e.target.value)} /></label>
      <label>Lease expires from<input type="date" value={leaseExpiresFrom} onChange={(e) => setLeaseExpiresFrom(e.target.value)} /></label>
      <label>Lease expires to<input type="date" value={leaseExpiresTo} onChange={(e) => setLeaseExpiresTo(e.target.value)} /></label>
    </div><button onClick={() => void load()}>Search residents</button></section>
    {error && <p className="message" role="alert">{error}</p>}
    <section className="panel table-wrap"><h2>Directory <span className="muted">{rows.length} results</span></h2>{loading ? <p>Loading residents…</p> : rows.length === 0 ? <p>No residents match these filters.</p> : <table><thead><tr><th>Resident</th><th>Property / unit</th><th>Lease</th><th>Move-in</th><th>Contact</th></tr></thead><tbody>{rows.map((row) => <tr key={row.id}><td><button className="link-button" onClick={() => void openProfile(row.id)}>{row.fullName}</button></td><td>{row.propertyName ?? "—"}{row.spaceCode ? ` · ${row.spaceCode}` : ""}</td><td>{row.leaseStatus ?? "No lease"}</td><td>{row.moveInOn ?? "—"}</td><td>{row.email ?? row.phone ?? "—"}</td></tr>)}</tbody></table>}</section>
    {profiles.length > 0 && <aside className={`resident-dock${profilesMinimized ? " is-minimized" : ""}`} aria-label="Open resident profiles">
      {profilesMinimized ? <div className="resident-dock-rail">
        <button className="resident-dock-expand" onClick={() => setProfilesMinimized(false)} aria-label="Restore resident profiles" title="Restore resident profiles">↗</button>
      </div> : <>
        <div className="resident-dock-header"><div><p className="eyebrow">Open residents</p><strong>{profiles.length} profile{profiles.length === 1 ? "" : "s"}</strong></div><button className="secondary" onClick={() => setProfilesMinimized(true)}>Minimize</button></div>
        <div className="resident-dock-tabs" role="tablist" aria-label="Open resident profiles">{profiles.map((item) => <div className="resident-dock-tab" key={item.resident.id}><button role="tab" aria-selected={item.resident.id === activeProfile?.resident.id} onClick={() => setActiveProfileId(item.resident.id)}>{item.resident.fullName}</button><button className="resident-dock-tab-close" onClick={() => closeProfile(item.resident.id)} aria-label={`Close ${item.resident.fullName}`}>×</button></div>)}</div>
        {activeProfile && <div className="resident-dock-body"><div className="resident-dock-title"><div><p className="eyebrow">Resident profile</p><h2>{activeProfile.resident.fullName}</h2><p>{activeProfile.resident.email ?? "No email"} · {activeProfile.resident.phone ?? "No phone"}</p></div><button className="secondary" onClick={() => closeProfile(activeProfile.resident.id)}>Close</button></div><div className="resident-profile-grid"><div><h3>Occupancy</h3>{activeProfile.occupancies.length ? <ul>{activeProfile.occupancies.map((x) => <li key={x.id}>{x.propertyName ?? "Property"} · {x.spaceCode ?? x.spaceId}<br /><span className="muted">{x.movedInOn} — {x.movedOutOn ?? "Current"}</span></li>)}</ul> : <p>No occupancy history.</p>}</div><div><h3>Leases</h3>{activeProfile.leases.length ? <ul>{activeProfile.leases.map((x) => <li key={x.id}>{x.status} · {x.startsOn} — {x.endsOn}<br /><span className="muted">${x.monthlyRent.toLocaleString()} monthly{x.noticeDate ? ` · notice ${x.noticeDate}` : ""}</span></li>)}</ul> : <p>No leases.</p>}</div><div><h3>Household</h3>{activeProfile.household.length ? <ul>{activeProfile.household.map((x) => <li key={x.id}>{x.fullName} · {x.relationship}</li>)}</ul> : <p>No household members.</p>}</div><div><h3>Linked work</h3>{activeProfile.work.length ? <ul>{activeProfile.work.map((x) => <li key={x.id}><Link href={`/work/${x.id}`}>{x.title}</Link><br /><span className="muted">{x.status} · {x.priority}</span></li>)}</ul> : <p>No linked work.</p>}</div></div></div>}
      </>}
    </aside>}
  </section></AppShell>;
}
