import { formatDateOnly } from "@/lib/format";
import { useTranslations } from "next-intl";

export type AuditTrailFields = {
  createdBy?: string;
  createdAt?: string;
  updatedBy?: string | null;
  updatedAt?: string | null;
  approvedBy?: string | null;
  approvedAt?: string | null;
  submittedBy?: string | null;
  submittedAt?: string | null;
};

type AuditTrailPanelProps = {
  fields: AuditTrailFields;
  locale: string;
  /** Gắn trong card phiếu — không tạo card riêng */
  embedded?: boolean;
};

export function AuditTrailPanel({ fields, locale, embedded = true }: AuditTrailPanelProps) {
  const t = useTranslations("audit");

  const items = [
    fields.createdBy
      ? { key: "create", action: t("actionCreate"), by: fields.createdBy, at: fields.createdAt }
      : null,
    fields.updatedBy && fields.updatedBy !== fields.createdBy
      ? { key: "update", action: t("actionUpdate"), by: fields.updatedBy, at: fields.updatedAt ?? undefined }
      : null,
    fields.submittedBy
      ? { key: "submit", action: t("actionSubmit"), by: fields.submittedBy, at: fields.submittedAt ?? undefined }
      : null,
    fields.approvedBy
      ? { key: "approve", action: t("actionApprove"), by: fields.approvedBy, at: fields.approvedAt ?? undefined }
      : null,
  ].filter(Boolean) as Array<{ key: string; action: string; by: string; at?: string }>;

  if (items.length === 0) {
    return null;
  }

  const content = (
    <p className="text-xs leading-relaxed text-slate-500">
      <span className="font-medium text-slate-600">{t("performedBy")}: </span>
      {items.map((item, index) => (
        <span key={item.key}>
          {index > 0 && <span className="mx-1.5 text-slate-300">·</span>}
          <span>{item.action} </span>
          <span className="font-medium text-slate-700">{item.by}</span>
          {item.at ? (
            <span className="text-slate-400"> ({formatDateOnly(item.at, locale)})</span>
          ) : null}
        </span>
      ))}
    </p>
  );

  if (embedded) {
    return (
      <div className="border-t border-slate-100 bg-slate-50/40 px-6 py-3">{content}</div>
    );
  }

  return <div className="card px-6 py-3">{content}</div>;
}
