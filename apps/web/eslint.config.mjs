import { defineConfig, globalIgnores } from "eslint/config";
import nextVitals from "eslint-config-next/core-web-vitals";

export default defineConfig([
  ...nextVitals,
  globalIgnores([
    ".next/**",
    ".next-sandbox-1/**",
    ".next-sandbox-2/**",
    ".next-sandbox-3/**",
    "node_modules/**",
    "playwright-report/**",
    "test-results/**",
  ]),
]);
