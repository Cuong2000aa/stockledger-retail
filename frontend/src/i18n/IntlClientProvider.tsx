"use client";

import { NextIntlClientProvider } from "next-intl";
import type { AbstractIntlMessages } from "next-intl";
import { getIntlMessageFallback } from "./message-fallback";

export function IntlClientProvider({
  locale,
  messages,
  children,
}: {
  locale: string;
  messages: AbstractIntlMessages;
  children: React.ReactNode;
}) {
  return (
    <NextIntlClientProvider
      locale={locale}
      messages={messages}
      getMessageFallback={(info) => getIntlMessageFallback({ ...info, locale })}
    >
      {children}
    </NextIntlClientProvider>
  );
}
