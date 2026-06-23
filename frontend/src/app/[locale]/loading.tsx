import { TableSkeleton } from "@/components/LoadingState";

export default function LocaleLoading() {
  return (
    <div className="animate-fade-in">
      <div className="mb-8">
        <div className="mb-2 h-1 w-10 rounded-full bg-gradient-to-r from-brand-500/40 to-brand-300/40" />
        <div className="skeleton h-8 w-52 max-w-full" />
        <div className="skeleton mt-2 h-4 w-80 max-w-full" />
      </div>
      <div className="card overflow-hidden">
        <div className="border-b border-slate-100 px-5 py-4">
          <div className="skeleton h-9 w-full max-w-md" />
        </div>
        <TableSkeleton rows={8} cols={5} />
      </div>
    </div>
  );
}
