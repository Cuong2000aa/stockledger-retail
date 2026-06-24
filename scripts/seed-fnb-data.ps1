# Seed Domino's & Popeyes F&B master data (idempotent — skips if DOMINOS exists).
# Usage:
#   .\scripts\seed-fnb-data.ps1

param(
    [string]$ConnectionString = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$sqlFile = Join-Path $PSScriptRoot "seed-fnb-data.sql"

if (-not (Test-Path $sqlFile)) {
    throw "SQL file not found: $sqlFile"
}

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    $appSettingsPath = Join-Path $repoRoot "host\StockLedgerRetail.HttpApi.Host\appsettings.json"
    if (-not (Test-Path $appSettingsPath)) {
        throw "appsettings.json not found. Pass -ConnectionString explicitly."
    }
    $json = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
    $ConnectionString = $json.ConnectionStrings.Default
}

function Parse-NpgsqlConnectionString([string]$cs) {
    $map = @{}
    foreach ($part in $cs.Split(';')) {
        if ([string]::IsNullOrWhiteSpace($part)) { continue }
        $kv = $part.Split('=', 2)
        if ($kv.Length -eq 2) {
            $map[$kv[0].Trim()] = $kv[1].Trim()
        }
    }
    return @{
        Host     = $map['Host']
        Port     = if ($map['Port']) { $map['Port'] } else { '5432' }
        Database = $map['Database']
        Username = $map['Username']
        Password = $map['Password']
    }
}

$db = Parse-NpgsqlConnectionString $ConnectionString
$psql = Get-Command psql -ErrorAction SilentlyContinue
if (-not $psql) {
    $candidates = @(
        "C:\Program Files\PostgreSQL\18\bin\psql.exe",
        "C:\Program Files\PostgreSQL\17\bin\psql.exe",
        "C:\Program Files\PostgreSQL\16\bin\psql.exe"
    )
    foreach ($path in $candidates) {
        if (Test-Path $path) { $psql = @{ Source = $path }; break }
    }
}
if (-not $psql) { throw "psql not found in PATH." }

$env:PGPASSWORD = $db.Password
Write-Host "Seeding F&B data (Domino's, Popeyes) into $($db.Database)..."

& $psql.Source -h $db.Host -p $db.Port -U $db.Username -d $db.Database -v ON_ERROR_STOP=1 -f $sqlFile

if ($LASTEXITCODE -ne 0) { throw "seed-fnb-data.sql failed." }
Write-Host "Done. Brands: DOMINOS, POPEYES. Filter SKU prefix DOM- / POP-."
