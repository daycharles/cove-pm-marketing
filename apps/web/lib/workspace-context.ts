export function returnToHref(pathname: string, search: string, target: string) {
  const returnTo = `${pathname}${search}`;
  const context = new URLSearchParams(search).get("propertyId");
  const separator = target.includes("?") ? "&" : "?";
  const contextQuery = context && !target.includes("propertyId=") ? `&propertyId=${encodeURIComponent(context)}` : "";
  return `${target}${separator}returnTo=${encodeURIComponent(returnTo)}${contextQuery}`;
}

export function safeReturnTo(value: string | null, fallback = "/work") {
  if (!value || !value.startsWith("/") || value.startsWith("//")) return fallback;
  return value;
}
