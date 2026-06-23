import clsx from "clsx";

const styles = {
  critical: "bg-red-50 text-red-700 ring-red-200",
  warning: "bg-amber-50 text-amber-800 ring-amber-200",
  info: "bg-sky-50 text-sky-700 ring-sky-200",
} as const;

export function SeverityBadge({
  severity,
  label,
}: {
  severity: string;
  label: string;
}) {
  const tone =
    severity === "critical" || severity === "warning" || severity === "info"
      ? severity
      : "info";

  return (
    <span className={clsx("badge", styles[tone])}>{label}</span>
  );
}
