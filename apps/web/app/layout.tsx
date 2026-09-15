import "./styles.css";
import type { Metadata } from "next";
import { AppFrame } from "./components/app-frame";

export const metadata: Metadata = {
  title: "Cove PM",
  description: "Cove PM by Averion Software for portfolios, residents, leasing, and operations.",
  icons: { icon: "/brand/cove-logo-light.png" },
};

export default function Layout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body><AppFrame>{children}</AppFrame></body>
    </html>
  );
}
