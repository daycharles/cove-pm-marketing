"use client";
import Link from "next/link";
import Image from "next/image";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useLayoutEffect, useRef, useState, type ReactNode } from "react";
import { api, type PropertyReference, type Session } from "../../lib/api";
import { hasCapability } from "../../lib/capabilities";
import { visibleNav } from "../../lib/navigation";
import { CommandSearch } from "./command-search";
import { ThemeToggle } from "./theme-toggle";

const navSections = [
  {
    label: "Workspace",
    hrefs: ["/", "/work", "/properties", "/marketing/listings", "/leasing/leases"],
  },
  { label: "People & updates", hrefs: ["/residents", "/announcements", "/portal", "/attention"] },
  {
    label: "Operations",
    hrefs: ["/calendar", "/inspections", "/procurement", "/reports", "/billing"],
  },
  {
    label: "Admin",
    hrefs: [
      "/autopilot",
      "/settings/categories",
      "/settings/automation",
      "/settings/configuration",
      "/integrations",
      "/settings/members",
    ],
  },
];

export function AppShell({
  session,
  children,
  onLogout,
  chromeOnly = false,
}: {
  session: Session;
  children: ReactNode;
  onLogout?: () => void;
  chromeOnly?: boolean;
}) {
  const router = useRouter();
  const pathname = usePathname();
  const search = typeof window === "undefined" ? "" : window.location.search;
  const nav = visibleNav(session);
  const [properties, setProperties] = useState<PropertyReference[]>([]);
  const [contextPropertyId, setContextPropertyId] = useState(() =>
    typeof window === "undefined"
      ? ""
      : (new URLSearchParams(window.location.search).get("propertyId") ?? ""),
  );
  const headerRef = useRef<HTMLElement | null>(null);
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
    if (!chromeOnly) return;
    if (!hasCapability(session, "Work.Read")) return;
    void api.properties
      .list()
      .then(setProperties)
      // Context should never prevent navigation if the reference list is temporarily unavailable.
      .catch(() => setProperties([]));
  }, [chromeOnly, session]);
  useLayoutEffect(() => {
    const savedScrollTop = window.sessionStorage.getItem("cove-sidebar-scroll-top");
    if (savedScrollTop == null) return;
    const scrollTop = Number(savedScrollTop);
    if (!Number.isFinite(scrollTop)) return;
    if (headerRef.current) headerRef.current.scrollTop = scrollTop;
  }, [pathname]);
  useEffect(() => {
    const header = headerRef.current;
    if (!header) return;
    const saveScrollPosition = () => {
      window.sessionStorage.setItem("cove-sidebar-scroll-top", String(header.scrollTop));
    };
    header.addEventListener("scroll", saveScrollPosition, { passive: true });
    return () => header.removeEventListener("scroll", saveScrollPosition);
  }, []);
  if (!chromeOnly) return <>{children}</>;
  return (
    <main>
      <header
        ref={headerRef}
        onClickCapture={() => {
          window.sessionStorage.setItem(
            "cove-sidebar-scroll-top",
            String(headerRef.current?.scrollTop ?? 0),
          );
        }}
      >
        <Link className="brand" href="/" aria-label="Cove PM home">
          <span className="brand-logo-frame">
            <Image
              className="brand-logo brand-logo-light"
              src="/brand/cove-logo-dark.png"
              alt="Cove Property Management Software"
              width={1256}
              height={590}
              priority
            />
          </span>
        </Link>
        {properties.length > 0 && (
          <label className="context-switcher">
            <span>Portfolio context</span>
            <select
              aria-label="Portfolio context"
              value={contextPropertyId}
              onChange={(event) => {
                const next = event.target.value;
                setContextPropertyId(next);
                const params = new URLSearchParams(search);
                if (next) params.set("propertyId", next);
                else params.delete("propertyId");
                router.push(`${pathname}${params.toString() ? `?${params}` : ""}`);
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
        <div className="workspace-context" aria-label="Workspace context">
          <Link href="/">Portfolio</Link>
          <span aria-hidden="true">›</span>
          <Link
            href={contextPropertyId ? `/properties?propertyId=${contextPropertyId}` : "/properties"}
          >
            {properties.find((property) => property.id === contextPropertyId)?.name ??
              "All properties"}
          </Link>
          {pathname !== "/" && (
            <>
              <span aria-hidden="true">›</span>
              <span>{nav.find((item) => pathname === item.href)?.label ?? "Workspace"}</span>
            </>
          )}
        </div>
        {nav.length > 0 && (
          <nav aria-label="Primary navigation">
            {navSections.map((section) => {
              const sectionItems = nav.filter((item) => section.hrefs.includes(item.href));
              if (!sectionItems.length) return null;
              return (
                <div key={section.label} className="nav-section">
                  <span className="nav-section-label">{section.label}</span>
                  {sectionItems.map((item) => (
                    <Link
                      key={item.href}
                      href={item.href}
                      aria-current={pathname === item.href ? "page" : undefined}
                    >
                      {item.label}
                    </Link>
                  ))}
                </div>
              );
            })}
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
        <ThemeToggle />
        <button className="secondary" onClick={() => void logout()}>
          Sign out
        </button>
      </header>
      {children}
      {canSearch && <CommandSearch />}
    </main>
  );
}
