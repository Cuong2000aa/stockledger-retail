import clsx from "clsx";

type FormFieldProps = {
  label: string;
  required?: boolean;
  hint?: string;
  className?: string;
  children: React.ReactNode;
};

export function FormField({
  label,
  required,
  hint,
  className,
  children,
}: FormFieldProps) {
  return (
    <div className={clsx("min-w-0", className)}>
      <label className="mb-1.5 block text-sm font-medium text-slate-700">
        {label}
        {required && <span className="ml-0.5 text-red-500">*</span>}
      </label>
      {children}
      {hint && <p className="mt-1 text-xs text-slate-500">{hint}</p>}
    </div>
  );
}
