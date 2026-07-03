param(
    [string]$RedisConnection = "127.0.0.1:6379"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

if (Get-Command docker -ErrorAction SilentlyContinue) {
    & "$PSScriptRoot\dev-up.ps1"
}
else {
    Write-Host "Docker not found — assuming Redis is already running at $RedisConnection (e.g. Redis Insight / Windows service)."
    $tcp = Test-NetConnection -ComputerName ($RedisConnection.Split(':')[0]) -Port ($RedisConnection.Split(':')[1]) -WarningAction SilentlyContinue
    if (-not $tcp.TcpTestSucceeded) {
        Write-Warning "Redis port may not be open. Start Redis manually or install Docker and run .\scripts\dev-up.ps1"
    }
}

$env:STOCKLEDGER_REDIS_CONNECTION = $RedisConnection

Write-Host "Running Redis automation tests..."
dotnet test "tests\StockLedgerRetail.Caching.Tests\StockLedgerRetail.Caching.Tests.csproj" --verbosity normal
