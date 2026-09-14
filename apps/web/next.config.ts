import type { NextConfig } from "next";
const config: NextConfig = {
  // Concurrent local/sandbox sessions must not share Next's build output or dev lock.
  distDir: process.env.NEXT_DIST_DIR ?? ".next",
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: `${process.env.PROPFLOW_API_ORIGIN ?? "https://localhost:5001"}/api/:path*`,
      },
    ];
  },
  async headers() {
    return [
      {
        source: "/(.*)",
        headers: [
          // Content-Security-Policy is set per-request by proxy.ts instead of here — it needs a
          // fresh nonce on every response so Next's own inline hydration scripts can run
          // without a blanket 'unsafe-inline'. See proxy.ts for why.
          { key: "Referrer-Policy", value: "no-referrer" },
          { key: "X-Content-Type-Options", value: "nosniff" },
          { key: "X-Frame-Options", value: "DENY" },
          { key: "Permissions-Policy", value: "camera=(), microphone=(), geolocation=()" },
          { key: "Strict-Transport-Security", value: "max-age=31536000; includeSubDomains" },
        ],
      },
    ];
  },
};
export default config;
