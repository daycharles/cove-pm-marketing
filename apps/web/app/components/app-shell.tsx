"use client";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState, type ReactNode } from "react";
import { api, type PropertyReference, type Session } from "../../lib/api";
import { hasCapability } from "../../lib/capabilities";
import { visibleNav } from "../../lib/navigation";
import { CommandSearch } from "./command-search";
export function AppShell({
  session,
  children,
  onLogout,
}: {
  session: Session;
  children: ReactNode;
  onLogout?: () => void;
}) {
  const router = useRouter();
  const pathname = usePathname();
  const nav = visibleNav(session);
  const [properties, setProperties] = useState<PropertyReference[]>([]);
  const [contextPropertyId, setContextPropertyId] = useState("");
  // Search spans work, assets, people and places — all behind Work.Read.
  const canSearch = hasCapability(session, "Work.Read");
  async function logout() {
    try {
      await api.auth.logout();
    } finally {
      onLogout?.();
      router.push("/");
      router.refresh();
    }
  }
  useEffect(() => {
    if (!hasCapability(session, "Work.Read")) return;
    void api.properties
      .list()
      .then(setProperties)
      // Context should never prevent navigation if the reference list is temporarily unavailable.
      .catch(() => setProperties([]));
  }, [session]);
  return (
    <main>
      <header>
        <Link className="brand" href="/" aria-label="Today">
          PropFlow
        </Link>
        <p className="shell-kicker">Property operations</p>
        {properties.length > 0 && (
          <label className="context-switcher">
            <span>Portfolio context</span>
            <select
              aria-label="Portfolio context"
              value={contextPropertyId}
              onChange={(event) => {
                const next = event.target.value;
                setContextPropertyId(next);
                if (next) router.push(`/properties?propertyId=${encodeURIComponent(next)}`);
              }}
            >
              <option value="">All properties</option>
              {properties.map((property) => (
                <option key={property.id} value={property.id}>
                  {property.name}
                </option>
              ))}
            </select>
          </label>
        )}
        {nav.length > 0 && (
          <nav aria-label="Primary navigation">
            {nav.map((item) => (
              <Link
                key={item.href}
                href={item.href}
                aria-current={pathname === item.href ? "page" : undefined}
              >
                {item.label}
              </Link>
            ))}
          </nav>
        )}
        {canSearch && (
          <button
            className="secondary search-trigger"
            onClick={() => window.dispatchEvent(new Event("propflow:open-search"))}
            aria-keyshortcuts="Meta+K Control+K"
          >
            Search <kbd>⌘K</kbd>
          </button>
        )}
        <span className="role">{session.role}</span>
        <button className="secondary" onClick={() => void logout()}>
          Sign out
        </button>
      </header>
      {children}
      {canSearch && <CommandSearch />}
    </main>
  );
}
