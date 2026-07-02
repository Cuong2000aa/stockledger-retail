# AGENTS.md

## Cursor Cloud specific instructions

### Services

| Service | Stack | Port | How to run |
|---------|-------|------|------------|
| Backend API (`StockLedgerRetail.HttpApi.Host`) | .NET 10, ASP.NET Core, EF Core | `5270` (HTTP) | `dotnet run --project host/StockLedgerRetail.HttpApi.Host --launch-profile http` |
| Frontend | Next.js 15, React 19, TypeScript | `3000` | `cd frontend && npm run dev` |
| PostgreSQL 16 | database `stockledger_retail` | `5432` | see startup note below |

Standard build/test/run commands live in `README.md` and `frontend/package.json`; only the non-obvious caveats are captured here.

### Startup caveats (dependencies are already installed by the update script)

- **PostgreSQL is not started automatically on VM boot.** Start it before running the API or DB-backed tests: `sudo pg_ctlcluster 16 main start`. The `postgres` role password is `201020` to match the committed `ConnectionStrings:Default`; the `stockledger_retail` database already exists in the snapshot.
- **Apply EF migrations after pulling new backend code** (creates/updates tables), from the repo root:
  `dotnet ef database update --project src/StockLedgerRetail.EntityFrameworkCore/StockLedgerRetail.EntityFrameworkCore.csproj --startup-project host/StockLedgerRetail.HttpApi.Host/StockLedgerRetail.HttpApi.Host.csproj`
  The `dotnet-ef` tool is installed at `~/.dotnet/tools` (ensure that is on `PATH`).
- **Login uses the email, not `admin`.** The README mentions `admin`/`1234`, but the stub login (and `X-User-Email` header) expects `admin@stockledger.local` / `1234`. Use the email in both the API (`POST /api/auth/login` with `username`=email) and the frontend `/login` form.
- **Demo data seeds on first API start** (`Seed:Fb:Enabled: true`) — brands (Domino's, Popeyes), warehouses, SKUs, stock. No manual seeding needed for a fresh DB.
- **Frontend API URL** defaults to `http://localhost:5270` (`frontend/src/lib/api.ts`), so `frontend/.env.local` is optional; create it from `.env.local.example` only to override.
- **`npm run dev`** runs `frontend/scripts/dev.mjs`, which clears the `.next` cache and frees port 3000 on each start. Use `npm run dev:quick` to skip that cleanup.
- Health probes (no auth): `GET /health` (liveness), `GET /health/ready` (DB connectivity).

### Tests

- .NET unit tests (no DB): `dotnet test tests/StockLedgerRetail.Domain.Shared.Tests` and `dotnet test tests/StockLedgerRetail.Application.Tests`.
- .NET integration tests (**require running PostgreSQL**): `dotnet test tests/StockLedgerRetail.Integration.Tests` (uses `appsettings.Testing.json`, same DB).
- Frontend: `cd frontend && npm run lint` and `npm run test:pricing`.
