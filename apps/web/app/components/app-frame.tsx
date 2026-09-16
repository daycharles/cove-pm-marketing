"use client";

import { usePathname } from "next/navigation";
import { useEffect, useState, type ReactNode } from "react";
import { api, type Session } from "../../lib/api";
import { AppShell } from "./app-shell";

/** Keeps the application chrome mounted while route content changes. */
export function AppFrame({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const [session, setSession] = useState<Session | null | undefined>();

  useEffect(() => {
    let active = true;
    const loadSession = () => {
      void api.session().then((next) => {
        if (active) setSession(next);
      }).catch(() => {
        if (active) setSession(null);
      });
    };
    loadSession();
    window.addEventListener("propflow:session-changed", loadSession);
    return () => {
      active = false;
      window.removeEventListener("propflow:session-changed", loadSession);
    };
  }, []);

  if (pathname.startsWith("/accept-invite")) return <>{children}</>;
  if (session === undefined) {
    return pathname === "/" ? <>{children}</> : <main className="centered"><p>Loading…</p></main>;
  }
  if (!session) return <>{children}</>;
  return <AppShell session={session} chromeOnly onLogout={() => setSession(null)}>{children}</AppShell>;
}
