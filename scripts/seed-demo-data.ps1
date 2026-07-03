# Seed N demo rows per table into PostgreSQL.
# Usage (from repo root):
#   .\scripts\seed-demo-data.ps1
#   .\scripts\seed-demo-data.ps1 -RowCount 1000
#   .\scripts\seed-demo-data.ps1 -RowCount 10000 -BrandCount 200
#   .\scripts\seed-demo-data.ps1 -ConnectionString "Host=localhost;Port=5432;Database=stockledger_retail;Username=postgres;Password=YOUR_PASSWORD"

param(
    [string]$ConnectionString = "",
    [int]$RowCount = 100,
    [int]$BrandCount = 0
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$sqlFile = Join-Path $PSScriptRoot "seed-demo-data.sql"

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

if ($RowCount -lt 1 -or $RowCount -gt 200000) {
    throw "-RowCount must be between 1 and 200000."
}

if ($BrandCount -eq 0) {
    $BrandCount = $RowCount
}

if ($BrandCount -lt 1 -or $BrandCount -gt 200000) {
    throw "-BrandCount must be between 1 and 200000."
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
if (-not $db.Host -or -not $db.Database -or -not $db.Username) {
    throw "Invalid connection string. Need Host, Database, Username."
}

$psql = Get-Command psql -ErrorAction SilentlyContinue
if (-not $psql) {
    $candidates = @(
        "C:\Program Files\PostgreSQL\18\bin\psql.exe",
        "C:\Program Files\PostgreSQL\17\bin\psql.exe",
        "C:\Program Files\PostgreSQL\16\bin\psql.exe",
        "C:\Program Files\PostgreSQL\15\bin\psql.exe"
    )
    foreach ($path in $candidates) {
        if (Test-Path $path) {
            $psql = @{ Source = $path }
            break
        }
    }
}

if (-not $psql) {
    throw @"
psql not found in PATH.
Install PostgreSQL client tools, or run manually:
  psql -h $($db.Host) -p $($db.Port) -U $($db.Username) -d $($db.Database) -f `"$sqlFile`"
"@
}

$env:PGPASSWORD = $db.Password
Write-Host "Seeding demo data into $($db.Database) on $($db.Host):$($db.Port) (RowCount=$RowCount, BrandCount=$BrandCount) ..."

& $psql.Source `
    -h $db.Host `
    -p $db.Port `
    -U $db.Username `
    -d $db.Database `
    -v ON_ERROR_STOP=1 `
    -v seed_count=$RowCount `
    -v brand_count=$BrandCount `
    -f $sqlFile

if ($LASTEXITCODE -ne 0) {
    throw "seed-demo-data.sql failed with exit code $LASTEXITCODE"
}

Write-Host "Done. Filter UI/API with prefix SEED- to find demo records."
