"use client";

import { useEffect, useRef } from "react";

type Theme = "day" | "night";
const storageKey = "cove-theme";

function applyTheme(theme: Theme) {
  document.documentElement.dataset.theme = theme;
  document.documentElement.style.colorScheme = theme === "night" ? "dark" : "light";
}

export function ThemeToggle({ onThemeChange }: { onThemeChange?: (theme: Theme) => void }) {
  const buttonRef = useRef<HTMLButtonElement>(null);
  const syncButton = (theme: Theme) => {
    const button = buttonRef.current;
    if (!button) return;
    const next = theme === "day" ? "night" : "day";
    button.setAttribute("aria-pressed", String(theme === "night"));
    button.setAttribute("aria-label", `Switch to ${next} mode`);
    button.lastElementChild!.textContent = theme === "day" ? "Night" : "Day";
    button.firstElementChild!.textContent = theme === "day" ? "☾" : "☀";
  };

  useEffect(() => {
    const saved = window.localStorage.getItem(storageKey);
    const preferred: Theme = window.matchMedia("(prefers-color-scheme: dark)").matches
      ? "night"
      : "day";
    const next = saved === "day" || saved === "night" ? saved : preferred;
    applyTheme(next);
    syncButton(next);
    onThemeChange?.(next);
  }, [onThemeChange]);

  const toggle = () => {
    const current = document.documentElement.dataset.theme === "night" ? "night" : "day";
    const next = current === "day" ? "night" : "day";
    window.localStorage.setItem(storageKey, next);
    applyTheme(next);
    syncButton(next);
    onThemeChange?.(next);
  };

  return (
    <button
      type="button"
      className="theme-toggle"
      onClick={toggle}
      ref={buttonRef}
      aria-pressed="false"
      aria-label="Switch to night mode"
    >
      <span aria-hidden="true">☾</span>
      <span>Night</span>
    </button>
  );
}
