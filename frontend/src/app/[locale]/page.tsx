import { Link } from "@/i18n/routing";
import { PageHeader } from "@/components/PageHeader";
import { getTranslations } from "next-intl/server";
import {
  Boxes,
  FileText,
  Package,
  Tags,
  Warehouse,
} from "lucide-react";

export default async function DashboardPage() {
  const t = await getTranslations("dashboard");
  const tNav = await getTranslations("nav");

  const links = [
    { href: "/products", label: tNav("products"), icon: Package },
    { href: "/product-variants", label: tNav("productVariants"), icon: Tags },
    { href: "/warehouses", label: tNav("warehouses"), icon: Warehouse },
    {
      href: "/inventory-documents",
      label: tNav("inventoryDocuments"),
      icon: FileText,
    },
    { href: "/current-stocks", label: tNav("currentStocks"), icon: Boxes },
  ];

  return (
    <div>
      <PageHeader title={t("title")} subtitle={t("subtitle")} />
      <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-500">
        {t("quickLinks")}
      </h2>
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {links.map(({ href, label, icon: Icon }) => (
          <Link
            key={href}
            href={href}
            className="card flex items-center gap-4 p-5 transition-shadow hover:shadow-md"
          >
            <div className="rounded-lg bg-brand-50 p-3 text-brand-600">
              <Icon className="h-6 w-6" />
            </div>
            <span className="font-medium text-slate-800">{label}</span>
          </Link>
        ))}
      </div>
    </div>
  );
}
