# Expanded rollout smoke: health, scope on read APIs, clerk <= admin visibility.
# Usage: .\scripts\rollout-smoke.ps1 [-BaseUrl http://localhost:5270]

param(
    [string]$BaseUrl = "http://localhost:5270",
    [string]$AdminEmail = "admin@stockledger.local",
    [string]$ClerkEmail = "clerk@stockledger.local"
)

$ErrorActionPreference = "Stop"

function Invoke-Api {
    param(
        [string]$Email,
        [string]$Method = "GET",
        [string]$Path
    )

    $uri = "$BaseUrl$Path"
    return Invoke-RestMethod -Uri $uri -Method $Method -Headers @{ "X-User-Email" = $Email }
}

function Assert-ClerkNotBroader {
    param(
        [string]$Label,
        [int]$AdminTotal,
        [int]$ClerkTotal
    )

    if ($ClerkTotal -gt $AdminTotal) {
        throw "$Label: clerk total ($ClerkTotal) exceeds admin ($AdminTotal)."
    }

    Write-Host "  $Label admin=$AdminTotal clerk=$ClerkTotal OK"
}

Write-Host "Rollout smoke against $BaseUrl"

Write-Host "`n[1] Health probes"
$live = Invoke-RestMethod -Uri "$BaseUrl/health" -Method GET
if ($live.status -ne "healthy") { throw "Liveness check failed." }
Write-Host "  /health => $($live.status)"

$ready = Invoke-RestMethod -Uri "$BaseUrl/health/ready" -Method GET
if ($ready.status -ne "ready" -or -not $ready.database) { throw "Readiness check failed." }
Write-Host "  /health/ready => $($ready.status) database=$($ready.database)"

Write-Host "`n[2] Warehouse scope on list endpoints"

$checks = @(
    @{ Label = "current-stocks"; AdminPath = "/api/current-stocks?page=1&pageSize=5"; ClerkPath = "/api/current-stocks?page=1&pageSize=5"; AdminCount = { $args[0].totalCount }; ClerkCount = { $args[0].totalCount } },
    @{ Label = "inventory-value"; AdminPath = "/api/reports/inventory-value?page=1&pageSize=5"; ClerkPath = "/api/reports/inventory-value?page=1&pageSize=5"; AdminCount = { $args[0].totalLineCount }; ClerkCount = { $args[0].totalLineCount } },
    @{ Label = "purchase-orders"; AdminPath = "/api/purchase-orders?page=1&pageSize=5"; ClerkPath = "/api/purchase-orders?page=1&pageSize=5"; AdminCount = { $args[0].totalCount }; ClerkCount = { $args[0].totalCount } },
    @{ Label = "goods-receipts"; AdminPath = "/api/goods-receipts?page=1&pageSize=5"; ClerkPath = "/api/goods-receipts?page=1&pageSize=5"; AdminCount = { $args[0].totalCount }; ClerkCount = { $args[0].totalCount } },
    @{ Label = "inventory-documents"; AdminPath = "/api/inventory-documents?page=1&pageSize=5"; ClerkPath = "/api/inventory-documents?page=1&pageSize=5"; AdminCount = { $args[0].totalCount }; ClerkCount = { $args[0].totalCount } },
    @{ Label = "stock-reservations"; AdminPath = "/api/stock-reservations?page=1&pageSize=5"; ClerkPath = "/api/stock-reservations?page=1&pageSize=5"; AdminCount = { $args[0].totalCount }; ClerkCount = { $args[0].totalCount } }
)

foreach ($check in $checks) {
    $admin = Invoke-Api -Email $AdminEmail -Path $check.AdminPath
    $clerk = Invoke-Api -Email $ClerkEmail -Path $check.ClerkPath
    Assert-ClerkNotBroader -Label $check.Label -AdminTotal (& $check.AdminCount $admin) -ClerkTotal (& $check.ClerkCount $clerk)
}

Write-Host "`n[3] Analytics summary reachable"
$adminSummary = Invoke-Api -Email $AdminEmail -Path "/api/analytics/summary"
$clerkSummary = Invoke-Api -Email $ClerkEmail -Path "/api/analytics/summary"
Write-Host "  admin openPOs=$($adminSummary.openPurchaseOrders) pendingGRs=$($adminSummary.pendingGoodsReceipts)"
Write-Host "  clerk openPOs=$($clerkSummary.openPurchaseOrders) pendingGRs=$($clerkSummary.pendingGoodsReceipts)"

if ($clerkSummary.pendingGoodsReceipts -gt $adminSummary.pendingGoodsReceipts) {
    throw "Clerk pending GR count exceeds admin."
}

Write-Host "`nRollout smoke completed successfully."
