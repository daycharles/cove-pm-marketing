"use client";

import { FormEvent, useEffect, useState } from "react";
import Link from "next/link";
import Image from "next/image";
import {
  api,
  ApiError,
  type Session,
  type AttentionQueue,
  type Lease,
  type Listing,
  type PropertyReference,
} from "../lib/api";
import { AppShell } from "./components/app-shell";

export default function Home() {
  const [session, setSession] = useState<Session | null>(null);
  const [ready, setReady] = useState(false);
  const [message, setMessage] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [recovery, setRecovery] = useState(false);
  async function bootstrap() {
    try {
      setSession(await api.session());
    } catch (error) {
      if (!(error instanceof ApiError && error.status === 401))
        setMessage("We could not load your session. Please try again.");
    } finally {
      setReady(true);
    }
  }
  useEffect(() => {
    const timer = window.setTimeout(() => void bootstrap(), 0);
    return () => window.clearTimeout(timer);
  }, []);
  async function login(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const values = new FormData(event.currentTarget);
    setSubmitting(true);
    setMessage("");
    try {
      await api.auth.login({
        organizationSlug: String(values.get("organizationSlug") ?? ""),
        email: String(values.get("email") ?? ""),
        password: String(values.get("password") ?? ""),
      });
      await bootstrap();
    } catch (error) {
      setMessage(
        error instanceof ApiError && error.status === 429
          ? "Too many attempts. Please wait and try again."
          : "Unable to sign in with those credentials and organization.",
      );
    } finally {
      setSubmitting(false);
    }
  }
  async function requestRecovery(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const values = new FormData(event.currentTarget);
    setSubmitting(true);
    setMessage("");
    try {
      await api.auth.requestPasswordRecovery({
        organizationSlug: String(values.get("organizationSlug") ?? ""),
        email: String(values.get("email") ?? ""),
      });
      setMessage("If the account exists, reset instructions have been sent.");
    } catch {
      setMessage("If the account exists, reset instructions have been sent.");
    } finally {
      setSubmitting(false);
    }
  }
  async function resetPassword(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const values = new FormData(event.currentTarget);
    setSubmitting(true);
    setMessage("");
    try {
      await api.auth.resetPassword({
        organizationSlug: String(values.get("organizationSlug") ?? ""),
        email: String(values.get("email") ?? ""),
        token: String(values.get("token") ?? ""),
        newPassword: String(values.get("newPassword") ?? ""),
      });
      setRecovery(false);
      setMessage("Password reset. You can sign in now.");
    } catch (error) {
      setMessage(error instanceof ApiError ? error.message : "Unable to reset the password.");
    } finally {
      setSubmitting(false);
    }
  }
  if (!ready)
    return (
      <main className="centered">
        <p>Loading Cove PM…</p>
      </main>
    );
  if (!session)
    return (
      <main className="login">
        <Image
          className="login-logo"
          src="/brand/cove-logo-light.png"
          alt="Cove Property Management Software"
          width={1256}
          height={590}
          priority
        />
        <h1 className="sr-only">Cove PM sign in</h1>
        <p>Sign in to manage your portfolio.</p>
        {!recovery ? (
          <form onSubmit={login}>
            <label>
              Organization slug
              <input name="organizationSlug" autoComplete="organization" required />
            </label>
            <label>
              Email
              <input name="email" type="email" autoComplete="email" required />
            </label>
            <label>
              Password
              <input name="password" type="password" autoComplete="current-password" required />
            </label>
            <button disabled={submitting}>{submitting ? "Signing in…" : "Sign in"}</button>
          </form>
        ) : (
          <>
            <form onSubmit={requestRecovery}>
              <label>
                Organization slug
                <input name="organizationSlug" autoComplete="organization" required />
              </label>
              <label>
                Email
                <input name="email" type="email" autoComplete="email" required />
              </label>
              <button disabled={submitting}>
                {submitting ? "Sending…" : "Send reset instructions"}
              </button>
            </form>
            <form onSubmit={resetPassword}>
              <label>
                Organization slug
                <input name="organizationSlug" autoComplete="organization" required />
              </label>
              <label>
                Email
                <input name="email" type="email" autoComplete="email" required />
              </label>
              <label>
                Reset token
                <input name="token" required />
              </label>
              <label>
                New password
                <input
                  name="newPassword"
                  type="password"
                  autoComplete="new-password"
                  minLength={12}
                  required
                />
              </label>
              <button disabled={submitting}>Reset password</button>
            </form>
          </>
        )}
        <button
          type="button"
          className="link-button"
          onClick={() => {
            setRecovery((value) => !value);
            setMessage("");
          }}
        >
          {recovery ? "Back to sign in" : "Forgot password?"}
        </button>
        {message && (
          <p className="message" role="alert">
            {message}
          </p>
        )}
      </main>
    );
  return (
    <AppShell session={session} onLogout={() => setSession(null)}>
      <TodayDashboard session={session} />
    </AppShell>
  );
}

function TodayDashboard({ session }: { session: Session }) {
  const [attention, setAttention] = useState<AttentionQueue | null>(null);
  const [properties, setProperties] = useState<PropertyReference[]>([]);
  const [leases, setLeases] = useState<Lease[]>([]);
  const [listings, setListings] = useState<Listing[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  async function load() {
    setLoading(true);
    setError("");
    try {
      const [queue, propertyList, leaseList, listingList] = await Promise.all([
        api.attention.get(),
        api.properties.list(),
        api.leasing.leases.list(),
        api.marketing.listings.list(),
      ]);
      setAttention(queue);
      setProperties(propertyList);
      setLeases(leaseList);
      setListings(listingList);
    } catch (cause) {
      setError(cause instanceof ApiError ? cause.message : "Today could not be loaded. Try again.");
    } finally {
      setLoading(false);
    }
  }
  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, []);
  const activeLeases = leases.filter((lease) => ["Active", "Renewed"].includes(lease.status));
  const noticeLeases = leases.filter((lease) => lease.status === "NoticeGiven");
  const publishedListings = listings.filter((listing) => listing.status === "Published");
  const critical = attention?.criticalCount ?? 0;
  const warning = attention?.warningCount ?? 0;
  return (
    <section className="today-workspace">
      <div className="workspace-heading">
        <div>
          <p className="eyebrow">{session.role} workspace</p>
          <h1>Today</h1>
          <p>Resolve exceptions first, then move leasing forward.</p>
        </div>
        <button className="secondary" type="button" onClick={() => void load()} disabled={loading}>
          {loading ? "Updating…" : "Refresh"}
        </button>
      </div>
      {error && (
        <p className="message" role="alert">
          {error}
        </p>
      )}
      <section className="priority-hero" aria-labelledby="attention-heading">
        <div>
          <p className="eyebrow">Your next actions</p>
          <h2 id="attention-heading">Needs attention</h2>
          <p>Items are ordered by urgency and keep their property context visible.</p>
        </div>
        <div className="priority-counts" aria-label="Attention counts">
          <span className="count-critical">{critical} critical</span>
          <span className="count-warning">{warning} at risk</span>
        </div>
        <Link className="button-link" href="/attention">
          Open priority queue
        </Link>
      </section>
      {loading ? (
        <section className="dashboard-grid" aria-label="Loading today dashboard">
          <div className="panel skeleton-panel">Loading prioritized work…</div>
          <div className="panel skeleton-panel">Loading portfolio pulse…</div>
        </section>
      ) : (
        <div className="dashboard-grid">
          <section className="panel action-queue">
            <div className="section-heading">
              <div>
                <p className="eyebrow">Triage</p>
                <h2>Act next</h2>
              </div>
              <Link href="/attention">View all</Link>
            </div>
            {attention?.items.length ? (
              <ul className="action-list">
                {attention.items.slice(0, 5).map((item) => (
                  <li
                    key={item.workId}
                    className={`action-row severity-${item.severity.toLowerCase()}`}
                  >
                    <div>
                      <span className="badge">{item.priority}</span>
                      <strong>{item.title}</strong>
                      <small>
                        {item.propertyName ?? "Property context unavailable"} · {item.status}
                      </small>
                    </div>
                    <div className="action-row-end">
                      <small>{item.dueDate ? `Due ${item.dueDate}` : "Review now"}</small>
                      <Link href={`/work/${item.workId}`}>Open</Link>
                    </div>
                  </li>
                ))}
              </ul>
            ) : (
              <div className="empty-state">
                <strong>Nothing urgent is waiting.</strong>
                <p>
                  Keep the portfolio moving by reviewing availability and upcoming lease changes.
                </p>
                <Link href="/marketing/listings">Review availability</Link>
              </div>
            )}
          </section>
          <section className="panel portfolio-pulse">
            <div className="section-heading">
              <div>
                <p className="eyebrow">Portfolio pulse</p>
                <h2>What changed</h2>
              </div>
              <Link href="/properties">Portfolio</Link>
            </div>
            <div className="metric-grid">
              <Link href="/properties">
                <Image
                  src="/brand/icon-property.png"
                  alt=""
                  aria-hidden="true"
                  width={203}
                  height={110}
                />
                <strong>{properties.length}</strong>
                <span>properties</span>
              </Link>
              <Link href="/leasing/leases">
                <Image
                  src="/brand/icon-leasing.png"
                  alt=""
                  aria-hidden="true"
                  width={145}
                  height={110}
                />
                <strong>{activeLeases.length}</strong>
                <span>active leases</span>
              </Link>
              <Link href="/leasing/leases">
                <Image
                  src="/brand/icon-leasing.png"
                  alt=""
                  aria-hidden="true"
                  width={145}
                  height={110}
                />
                <strong>{noticeLeases.length}</strong>
                <span>move-outs to plan</span>
              </Link>
              <Link href="/marketing/listings">
                <Image
                  src="/brand/icon-reporting.png"
                  alt=""
                  aria-hidden="true"
                  width={150}
                  height={110}
                />
                <strong>{publishedListings.length}</strong>
                <span>homes marketed</span>
              </Link>
            </div>
          </section>
        </div>
      )}
      <section className="dashboard-grid completion-grid">
        <section className="panel">
          <p className="eyebrow">Fill homes</p>
          <h2>Leasing flow</h2>
          <p>
            {noticeLeases.length
              ? `${noticeLeases.length} move-out${noticeLeases.length === 1 ? "" : "s"} need a next leasing step.`
              : "No recorded move-outs are waiting for a leasing decision."}
          </p>
          <Link className="button-link secondary-link" href="/leasing/leases">
            Review lease lifecycle
          </Link>
        </section>
        <section className="panel">
          <p className="eyebrow">Keep context</p>
          <h2>Portfolio readiness</h2>
          <p>
            Open a property to see occupied and vacant spaces, contacts, documents, and operational
            context together.
          </p>
          <Link className="button-link secondary-link" href="/properties">
            Open properties
          </Link>
        </section>
      </section>
    </section>
  );
}
