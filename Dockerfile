# -----------------------------------------------------------------------------
# StockLedger Retail - Backend API (.NET 10) Dockerfile
# Multi-stage build for minimal container size & security
# -----------------------------------------------------------------------------

# Stage 1: Build & Restore
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS builder
WORKDIR /src

# Copy solution and project files for cached dependency restore
COPY StockLedgerRetail.slnx ./
COPY src/StockLedgerRetail.Domain.Shared/*.csproj src/StockLedgerRetail.Domain.Shared/
COPY src/StockLedgerRetail.Domain/*.csproj src/StockLedgerRetail.Domain/
COPY src/StockLedgerRetail.Application.Contracts/*.csproj src/StockLedgerRetail.Application.Contracts/
COPY src/StockLedgerRetail.Application/*.csproj src/StockLedgerRetail.Application/
COPY src/StockLedgerRetail.EntityFrameworkCore/*.csproj src/StockLedgerRetail.EntityFrameworkCore/
COPY src/StockLedgerRetail.HttpApi/*.csproj src/StockLedgerRetail.HttpApi/
COPY host/StockLedgerRetail.HttpApi.Host/*.csproj host/StockLedgerRetail.HttpApi.Host/

# Restore dependencies
RUN dotnet restore host/StockLedgerRetail.HttpApi.Host/StockLedgerRetail.HttpApi.Host.csproj

# Copy source code and build/publish
COPY src/ src/
COPY host/ host/

WORKDIR /src/host/StockLedgerRetail.HttpApi.Host
RUN dotnet publish StockLedgerRetail.HttpApi.Host.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Runner
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runner
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5270
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Create non-root app user
RUN addgroup --system --gid 1001 appgroup && \
    adduser --system --uid 1001 --ingroup appgroup appuser && \
    mkdir -p /app/logs && chown -R appuser:appgroup /app

COPY --from=builder --chown=appuser:appgroup /app/publish ./

USER appuser

EXPOSE 5270

HEALTHCHECK --interval=15s --timeout=3s --start-period=10s --retries=3 \
  CMD curl --fail http://localhost:5270/health || exit 1

ENTRYPOINT ["dotnet", "StockLedgerRetail.HttpApi.Host.dll"]
