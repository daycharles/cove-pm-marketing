import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

/**
 * Per-request CSP with a nonce, following Next's own documented pattern
 * (https://nextjs.org/docs/app/guides/content-security-policy).
 *
 * `next.config.ts` set `script-src 'self' 'unsafe-inline' ...` as a static header (`40b7bdd`,
 * "fix: restore app runtime under CSP") to get past the App Router blocking its own inline
 * hydration `<script>` tags under the original `default-src 'self'` policy from PR #253/#255.
 * It works, but `'unsafe-inline'` on `script-src` is unconditional — it holds in production too,
 * and it is close to giving up on script-src as an XSS mitigation at all: an attacker who gets
 * markup onto the page (a stored XSS) can now run arbitrary inline script, which is exactly what
 * this header exists to stop.
 *
 * This keeps the same problem fixed — Next's inline hydration scripts still run — without the
 * blanket allowance: proxy mints a nonce per request, forwards it to the app via the `x-nonce`
 * request header (Next reads that header and stamps the nonce onto the scripts it renders), and
 * sets the same nonce on the response's CSP header. `'unsafe-eval'` stays, but only outside
 * production, for Turbopack's dev HMR runtime — production `script-src` is `'self' 'nonce-…'`
 * and nothing else.
 *
 * `style-src` keeps `'unsafe-inline'` — tried dropping it first, and `sidebar-navigation.spec.ts`
 * (walking every `PRIMARY_NAV` route) caught 530 real "Applying inline style violates ...
 * style-src" console errors, 19 distinct content hashes, most of them the well-known SHA-256
 * hash of an empty string (`style=""`). `next/image` — used throughout for logos, wordmarks and
 * feature icons — sets an inline `style` attribute on its rendered `<img>` at runtime for
 * responsive sizing, whether or not there's anything in it. That's Next's own runtime behavior,
 * not app code, it's dynamic per element (so it can't be hash-pinned), and a style-attribute
 * injection is a much smaller attack surface than an inline *script* injection (CSS-based
 * exfiltration/UI redress, not arbitrary code execution) — so the trade a lot of real-world CSP
 * deployments make: nonce `script-src` strictly, leave `style-src` open. `font-src`/the
 * `https://fonts.googleapis.com` allowance on `style-src` stay, for the Nunito `@import` in
 * `app/styles.css:1`.
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
    "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com",
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
