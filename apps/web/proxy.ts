import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

/**
 * Per-request CSP with a nonce, following Next's own documented pattern
 * (https://nextjs.org/docs/app/guides/content-security-policy).
 *
 * `next.config.ts` used to set `Content-Security-Policy: default-src 'self'` as a static
 * header (PR #253/#255, closing part of `docs/followups.md`'s "Web CSP" row). That is right for
 * an already-hydrated page but it also blocks the browser from running the small inline
 * `<script>` tags the App Router itself emits to stream RSC payloads into the client — every
 * page loaded and its JS bundles all returned 200, but the console showed
 * "Executing inline script violates ... default-src 'self'" and the app sat on its SSR
 * "Loading Cove PM…" fallback forever, hydration never ran. `next dev` (Turbopack) needs
 * `'unsafe-eval'` too, for its HMR runtime. The app also `@import`s Nunito from Google Fonts
 * directly (`app/styles.css:1`, never self-hosted), which the old policy blocked outright in
 * every environment, dev and prod alike.
 *
 * The fix keeps the strict default and nonces Next's own inline scripts instead of opening
 * `script-src` with `'unsafe-inline'`: proxy mints a nonce per request, forwards it to the app
 * via the `x-nonce` request header (Next reads that header and stamps the nonce onto the
 * scripts it renders), and sets the same nonce on the response's CSP header.
 *
 * File convention is `proxy.ts`, not `middleware.ts` — Next 16 renamed and deprecated the latter
 * (`node_modules/next/dist/docs/01-app/03-api-reference/03-file-conventions/proxy.md`).
 */
export function proxy(request: NextRequest) {
  const nonce = Buffer.from(crypto.randomUUID()).toString("base64");
  const isDev = process.env.NODE_ENV !== "production";

  const csp = [
    "default-src 'self'",
    `script-src 'self' 'nonce-${nonce}'${isDev ? " 'unsafe-eval'" : ""}`,
    "style-src 'self' https://fonts.googleapis.com",
    "font-src 'self' https://fonts.gstatic.com",
    "frame-ancestors 'none'",
    "base-uri 'self'",
    "form-action 'self'",
  ].join("; ");

  const requestHeaders = new Headers(request.headers);
  requestHeaders.set("x-nonce", nonce);

  const response = NextResponse.next({ request: { headers: requestHeaders } });
  response.headers.set("Content-Security-Policy", csp);
  return response;
}

export const config = {
  // Run on every navigable route, but skip static assets and Next's own internals — they are
  // fingerprinted and never need a per-request nonce.
  matcher: ["/((?!_next/static|_next/image|favicon.ico).*)"],
};
