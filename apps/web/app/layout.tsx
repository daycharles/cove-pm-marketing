import "./styles.css";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Cove PM",
  description:
    "Cove PM by Averion Software for portfolios, residents, leasing, and operations.",
  icons: { icon: "/brand/cove-logo-light.png" },
};

export default function Layout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
