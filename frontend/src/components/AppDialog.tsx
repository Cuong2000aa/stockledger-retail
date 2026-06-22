"use client";

import clsx from "clsx";
import { AlertCircle, CheckCircle2, HelpCircle, X } from "lucide-react";
import { useTranslations } from "next-intl";
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";

export type DialogVariant = "error" | "success" | "info" | "confirm";

type AlertPayload = {
  variant: Exclude<DialogVariant, "confirm">;
  title: string;
  message: string;
};

type ConfirmPayload = {
  title: string;
  message: string;
  resolve: (value: boolean) => void;
};

type AppDialogContextValue = {
  alert: (payload: AlertPayload) => void;
  confirm: (title: string, message: string) => Promise<boolean>;
};

const AppDialogContext = createContext<AppDialogContextValue | null>(null);

export function AppDialogProvider({ children }: { children: React.ReactNode }) {
  const tDialog = useTranslations("dialog");
  const [alertState, setAlertState] = useState<AlertPayload | null>(null);
  const [confirmState, setConfirmState] = useState<ConfirmPayload | null>(null);

  const alert = useCallback((payload: AlertPayload) => {
    setAlertState(payload);
  }, []);

  const confirm = useCallback((title: string, message: string) => {
    return new Promise<boolean>((resolve) => {
      setConfirmState({ title, message, resolve });
    });
  }, []);

  const closeAlert = useCallback(() => setAlertState(null), []);

  const closeConfirm = useCallback(
    (result: boolean) => {
      if (confirmState) {
        confirmState.resolve(result);
      }
      setConfirmState(null);
    },
    [confirmState]
  );

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        if (confirmState) {
          closeConfirm(false);
        } else if (alertState) {
          closeAlert();
        }
      }
    };

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [alertState, closeAlert, closeConfirm, confirmState]);

  const value = useMemo(() => ({ alert, confirm }), [alert, confirm]);

  const open = alertState ?? confirmState;

  return (
    <AppDialogContext.Provider value={value}>
      {children}
      {open && (
        <div
          className="fixed inset-0 z-[100] flex items-center justify-center bg-slate-900/50 p-4"
          role="presentation"
          onClick={() => {
            if (confirmState) {
              closeConfirm(false);
            } else {
              closeAlert();
            }
          }}
        >
          <div
            role="dialog"
            aria-modal="true"
            aria-labelledby="app-dialog-title"
            aria-describedby="app-dialog-message"
            className="card w-full max-w-md p-6 shadow-xl"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="mb-4 flex items-start gap-3">
              <DialogIcon
                variant={alertState?.variant ?? "confirm"}
              />
              <div className="min-w-0 flex-1">
                <h2
                  id="app-dialog-title"
                  className="text-lg font-semibold text-slate-900"
                >
                  {alertState?.title ?? confirmState?.title}
                </h2>
                <p
                  id="app-dialog-message"
                  className="mt-2 whitespace-pre-line text-sm leading-relaxed text-slate-600"
                >
                  {alertState?.message ?? confirmState?.message}
                </p>
              </div>
              <button
                type="button"
                className="rounded-lg p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600"
                aria-label={tDialog("close")}
                onClick={() => {
                  if (confirmState) {
                    closeConfirm(false);
                  } else {
                    closeAlert();
                  }
                }}
              >
                <X className="h-5 w-5" />
              </button>
            </div>

            <div className="flex justify-end gap-2">
              {confirmState ? (
                <>
                  <button
                    type="button"
                    className="btn-secondary"
                    onClick={() => closeConfirm(false)}
                  >
                    {tDialog("cancel")}
                  </button>
                  <button
                    type="button"
                    className="btn-primary"
                    onClick={() => closeConfirm(true)}
                  >
                    {tDialog("confirm")}
                  </button>
                </>
              ) : (
                <button type="button" className="btn-primary" onClick={closeAlert}>
                  {tDialog("ok")}
                </button>
              )}
            </div>
          </div>
        </div>
      )}
    </AppDialogContext.Provider>
  );
}

function DialogIcon({ variant }: { variant: DialogVariant }) {
  const className = clsx("mt-0.5 h-6 w-6 shrink-0", {
    "text-red-600": variant === "error",
    "text-emerald-600": variant === "success",
    "text-brand-600": variant === "info",
    "text-amber-600": variant === "confirm",
  });

  if (variant === "success") {
    return <CheckCircle2 className={className} aria-hidden />;
  }

  if (variant === "confirm") {
    return <HelpCircle className={className} aria-hidden />;
  }

  if (variant === "info") {
    return <HelpCircle className={className} aria-hidden />;
  }

  return <AlertCircle className={className} aria-hidden />;
}

export function useAppDialog() {
  const context = useContext(AppDialogContext);
  if (!context) {
    throw new Error("useAppDialog must be used within AppDialogProvider");
  }
  return context;
}
