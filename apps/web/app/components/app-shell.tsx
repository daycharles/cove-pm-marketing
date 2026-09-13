"use client";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import type { ReactNode } from "react";
import { useEffect, useState } from "react";
import { api, type Session } from "../../lib/api";
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
  const [mobileNavOpen, setMobileNavOpen] = useState(false);
  const [theme, setTheme] = useState<"light" | "dark">("light");
  const nav = visibleNav(session);
  // Search spans work, assets, people and places — all behind Work.Read.
  const canSearch = hasCapability(session, "Work.Read");
  useEffect(() => {
    const timer = window.setTimeout(() => {
      const stored = window.localStorage.getItem("cove-theme");
      const preferred = window.matchMedia("(prefers-color-scheme: dark)").matches
        ? "dark"
        : "light";
      setTheme(stored === "dark" || stored === "light" ? stored : preferred);
    }, 0);
    return () => window.clearTimeout(timer);
  }, []);
  function toggleTheme() {
    const next = theme === "dark" ? "light" : "dark";
    setTheme(next);
    window.localStorage.setItem("cove-theme", next);
  }
  async function logout() {
    try {
      await api.auth.logout();
    } finally {
      onLogout?.();
      router.push("/");
      router.refresh();
    }
  }
  return (
    <main data-theme={theme}>
      <header className={mobileNavOpen ? "nav-open" : ""}>
        <Link className="brand" href="/" aria-label="Cove PM by Averion Software home">
          <img
            className="brand-image brand-light"
            src="/brand/cove-logo-light.png"
            alt="Cove PM by Averion Software"
          />
          <img
            className="brand-image brand-dark"
            src="/brand/cove-logo-dark.png"
            alt="Cove PM by Averion Software"
          />
        </Link>
        <button
          className="nav-toggle secondary"
          type="button"
          aria-expanded={mobileNavOpen}
          aria-controls="primary-navigation"
          onClick={() => setMobileNavOpen((open) => !open)}
        >
          <span aria-hidden="true">☰</span> Menu
        </button>
        {nav.length > 0 && (
          <nav id="primary-navigation" aria-label="Primary navigation">
            <span className="nav-section-label">Workspace</span>
            {nav
              .filter((item) =>
                ["/", "/properties", "/marketing/listings", "/leasing/leases"].includes(item.href),
              )
              .map((item) => (
                <Link
                  key={item.href}
                  href={item.href}
                  aria-current={pathname === item.href ? "page" : undefined}
                  onClick={() => setMobileNavOpen(false)}
                >
                  {item.label}
                </Link>
              ))}
            <span className="nav-section-label">People & updates</span>
            {nav
              .filter((item) => ["/portal", "/announcements", "/attention"].includes(item.href))
              .map((item) => (
                <Link
                  key={item.href}
                  href={item.href}
                  aria-current={pathname === item.href ? "page" : undefined}
                  onClick={() => setMobileNavOpen(false)}
                >
                  {item.label}
                </Link>
              ))}
            {nav.some(
              (item) => item.href.startsWith("/settings") || item.href === "/integrations",
            ) && <span className="nav-section-label">Admin</span>}
            {nav
              .filter((item) => item.href.startsWith("/settings") || item.href === "/integrations")
              .map((item) => (
                <Link
                  key={item.href}
                  href={item.href}
                  aria-current={pathname === item.href ? "page" : undefined}
                  onClick={() => setMobileNavOpen(false)}
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
        <button
          className="secondary theme-toggle"
          type="button"
          aria-label={`Switch to ${theme === "dark" ? "day" : "night"} mode`}
          aria-pressed={theme === "dark"}
          onClick={toggleTheme}
        >
          <span aria-hidden="true">{theme === "dark" ? "☀" : "☾"}</span>
          {theme === "dark" ? "Day mode" : "Night mode"}
        </button>
        <span className="role">{session.role}</span>
        <button className="secondary sign-out" onClick={() => void logout()}>
          Sign out
        </button>
      </header>
      {children}
      {canSearch && <CommandSearch />}
    </main>
  );
}
