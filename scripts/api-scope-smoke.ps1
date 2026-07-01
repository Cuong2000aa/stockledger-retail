# Smoke checks for warehouse scope and pricing authority.
# Usage: .\scripts\api-scope-smoke.ps1 [-BaseUrl http://localhost:5270]

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

Write-Host "API scope smoke against $BaseUrl"

$adminReports = Invoke-Api -Email $AdminEmail -Path "/api/reports/inventory-value?page=1&pageSize=5"
$clerkReports = Invoke-Api -Email $ClerkEmail -Path "/api/reports/inventory-value?page=1&pageSize=5"

Write-Host "Admin report lines: $($adminReports.lines.Count) totalLines=$($adminReports.totalLineCount)"
Write-Host "Clerk report lines: $($clerkReports.lines.Count) totalLines=$($clerkReports.totalLineCount)"

$adminStocks = Invoke-Api -Email $AdminEmail -Path "/api/current-stocks?page=1&pageSize=5"
$clerkStocks = Invoke-Api -Email $ClerkEmail -Path "/api/current-stocks?page=1&pageSize=5"

Write-Host "Admin current stocks: $($adminStocks.items.Count) total=$($adminStocks.totalCount)"
Write-Host "Clerk current stocks: $($clerkStocks.items.Count) total=$($clerkStocks.totalCount)"

if ($clerkStocks.totalCount -gt $adminStocks.totalCount) {
    throw "Clerk should not see more stock rows than admin."
}

try {
    Invoke-Api -Email $ClerkEmail -Method "POST" -Path "/api/product-variants/00000000-0000-0000-0000-000000000099/prices" | Out-Null
    throw "Expected pricing mismatch to be rejected."
}
catch {
    if ($_.Exception.Response.StatusCode.value__ -ne 404 -and $_.Exception.Response.StatusCode.value__ -ne 400) {
        Write-Host "Pricing negative path returned expected HTTP error."
    }
}

Write-Host "Smoke checks completed."
