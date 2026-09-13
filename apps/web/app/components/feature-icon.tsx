type FeatureIconName = "property" | "leasing" | "analytics" | "resident";

/** Small solid-fill feature marks derived from the Cove style-guide icon language. */
export function FeatureIcon({ name, label }: { name: FeatureIconName; label: string }) {
  return (
    <svg className="feature-icon" viewBox="0 0 48 48" role="img" aria-label={label}>
      <title>{label}</title>
      {name === "property" && (
        <>
          <path fill="currentColor" d="M5 23 24 7l19 16v19H5Z" />
          <path fill="#fff" d="m24 13 13 11v13H11V24Z" />
          <path fill="currentColor" d="M20 25h8v12h-8zM14 25h4v4h-4zm16 0h4v4h-4z" />
          <path
            fill="var(--cove-green)"
            d="M5 39c10 4 18 4 26 0 6-3 10-2 14 1-8 5-16 6-25 4-6-1-10-3-15-5Z"
          />{" "}
        </>
      )}
      {name === "leasing" && (
        <>
          <path fill="currentColor" d="M10 5h23l7 7v31H10Z" />
          <path fill="#fff" d="M15 10h16v28H15Z" />
          <path fill="var(--cove-green)" d="m27 34 4-11 5-4 2 3-4 5-4 10z" />
          <path fill="currentColor" d="M18 16h10v3H18zm0 7h8v3h-8zm0 7h6v3h-6z" />{" "}
        </>
      )}
      {name === "analytics" && (
        <>
          <path fill="currentColor" d="M6 40h36v3H6zM10 35V23h7v12zm11 0V15h7v20zm11 0V7h7v28z" />
          <path fill="var(--cove-green)" d="m8 19 9-8 8 4 12-10 2 3-14 12-8-4-7 7z" />
        </>
      )}
      {name === "resident" && (
        <>
          <circle fill="currentColor" cx="24" cy="15" r="8" />
          <circle fill="currentColor" cx="10" cy="21" r="5" />
          <circle fill="currentColor" cx="38" cy="21" r="5" />
          <path
            fill="currentColor"
            d="M10 43c0-10 5-16 14-16s14 6 14 16zM0 42c0-7 3-11 9-11 2 0 4 1 5 2-3 2-5 5-6 9zm48 0c0-7-3-11-9-11-2 0-4 1-5 2 3 2 5 5 6 9z"
          />
          <path fill="var(--cove-green)" d="M20 34h8v3h-8z" />
        </>
      )}
    </svg>
  );
}
