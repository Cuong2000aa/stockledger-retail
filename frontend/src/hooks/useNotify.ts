"use client";

import { useAppDialog } from "@/components/AppDialog";
import { formatApiErrorMessage } from "@/lib/api-errors";
import { getApiErrorMessage } from "@/lib/api";
import type { ValidationIssue } from "@/lib/validation";
import { useLocale, useTranslations } from "next-intl";
import { useCallback } from "react";

export function useNotify() {
  const dialog = useAppDialog();
  const locale = useLocale();
  const tValidation = useTranslations("validation");
  const tApiErrors = useTranslations("apiErrors");
  const tDialog = useTranslations("dialog");

  const notifyValidation = useCallback(
    (issues: ValidationIssue[]) => {
      if (issues.length === 0) {
        return false;
      }

      const message = issues
        .map((issue) => tValidation(issue.key as never, issue.values))
        .join("\n");

      dialog.alert({
        variant: "error",
        title: tDialog("validationTitle"),
        message,
      });
      return true;
    },
    [dialog, tDialog, tValidation]
  );

  const notifyError = useCallback(
    (error: unknown) => {
      const raw = getApiErrorMessage(error);
      dialog.alert({
        variant: "error",
        title: tDialog("errorTitle"),
        message: formatApiErrorMessage(
          raw,
          (key, values) => tApiErrors(key as never, values),
          locale
        ),
      });
    },
    [dialog, locale, tApiErrors, tDialog]
  );

  const notifySuccess = useCallback(
    (message: string) => {
      dialog.alert({
        variant: "success",
        title: tDialog("successTitle"),
        message,
      });
    },
    [dialog, tDialog]
  );

  const confirm = useCallback(
    (message: string, title?: string) =>
      dialog.confirm(title ?? tDialog("confirmTitle"), message),
    [dialog, tDialog]
  );

  return {
    notifyValidation,
    notifyError,
    notifySuccess,
    confirm,
  };
}
