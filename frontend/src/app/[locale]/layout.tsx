import { NextIntlClientProvider } from "next-intl";
import { getMessages, getTranslations } from "next-intl/server";
import { notFound } from "next/navigation";
import { routing } from "@/i18n/routing";
import { AppDialogProvider } from "@/components/AppDialog";
import { AppLayout } from "@/components/AppLayout";
import { QueryProvider } from "@/components/QueryProvider";
import "../globals.css";

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  const t = await getTranslations({ locale, namespace: "common" });

  return {
    title: `${t("appBrand")} | ${t("appTagline")}`,
    description: t("appName"),
  };
}

export default async function LocaleLayout({
  children,
  params,
}: {
  children: React.ReactNode;
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;

  if (!routing.locales.includes(locale as "vi" | "en")) {
    notFound();
  }

  const messages = await getMessages();

  return (
    <html lang={locale}>
      <body>
        <NextIntlClientProvider messages={messages}>
          <QueryProvider>
            <AppDialogProvider>
              <AppLayout>{children}</AppLayout>
            </AppDialogProvider>
          </QueryProvider>
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
