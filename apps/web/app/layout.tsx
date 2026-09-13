import "./styles.css";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Cove Property Management Software",
  description:
    "Cove property management software for portfolios, residents, leasing, and operations.",
  icons: { icon: "/brand/cove-logo-light.png" },
};

export default function Layout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
