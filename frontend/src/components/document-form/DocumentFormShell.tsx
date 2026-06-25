type DocumentFormShellProps = {
  children: React.ReactNode;
  footer: React.ReactNode;
};

export function DocumentFormShell({ children, footer }: DocumentFormShellProps) {
  return (
    <div className="card mx-auto max-w-5xl overflow-hidden">
      <div className="p-5 sm:p-6">{children}</div>
      <div className="flex flex-wrap items-center justify-end gap-3 border-t border-slate-100 bg-slate-50/60 px-5 py-4 sm:px-6">
        {footer}
      </div>
    </div>
  );
}
