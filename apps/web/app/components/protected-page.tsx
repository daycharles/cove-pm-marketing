"use client";
import { useEffect, useState, type ReactNode } from "react";
import Link from "next/link";
import { api, type Session } from "../../lib/api";
import { hasCapability } from "../../lib/capabilities";
export function ProtectedPage({
  capability,
  children,
}: {
  capability: string;
  children: (session: Session) => ReactNode;
}) {
  const [session, setSession] = useState<Session | null | undefined>();
  const [theme, setTheme] = useState<"light" | "dark">("light");
  useEffect(() => {
    const timer = window.setTimeout(() => {
      const stored = window.localStorage.getItem("cove-theme");
      const preferred = window.matchMedia("(prefers-color-scheme: dark)").matches
        ? "dark"
        : "light";
      setTheme(stored === "dark" || stored === "light" ? stored : preferred);
    }, 0);
    const sessionRequest = api
      .session()
      .then(setSession)
      .catch(() => setSession(null));
    return () => {
      window.clearTimeout(timer);
      void sessionRequest;
    };
  }, []);
  if (session === undefined)
    return (
      <main className="centered">
        <p>Loading…</p>
      </main>
    );
  if (!session)
    return (
      <main className="centered">
        <h1>Sign in required</h1>
        <Link href="/">Return to sign in</Link>
      </main>
    );
  if (!hasCapability(session, capability))
    return (
      <main className="centered access-denied" data-theme={theme}>
        <button
          className="secondary theme-toggle access-denied-theme-toggle"
          type="button"
          aria-label={`Switch to ${theme === "dark" ? "day" : "night"} mode`}
          aria-pressed={theme === "dark"}
          onClick={() => {
            const next = theme === "dark" ? "light" : "dark";
            setTheme(next);
            window.localStorage.setItem("cove-theme", next);
          }}
        >
          <span aria-hidden="true">{theme === "dark" ? "☀" : "☾"}</span>
          {theme === "dark" ? "Day mode" : "Night mode"}
        </button>
        <div className="access-denied-card" role="alert" aria-labelledby="access-denied-title">
          <span className="access-denied-icon" aria-hidden="true">
            !
          </span>
          <p className="eyebrow">Cove PM workspace</p>
          <h1 id="access-denied-title">You don’t have access to this page</h1>
          <p>
            You’re signed in, but your account doesn’t include the permission needed for this
            workspace. No private information from the page was displayed.
          </p>
          <p className="access-denied-next-step">
            Return to Work to continue with the areas available to you, or contact your organization
            administrator if you need access.
          </p>
          <Link className="access-denied-action" href="/">
            Return to Work
          </Link>
        </div>
      </main>
    );
  return <>{children(session)}</>;
}
