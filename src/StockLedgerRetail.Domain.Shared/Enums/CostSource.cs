namespace StockLedgerRetail.Enums;

/// <summary>
/// Nguồn giá vốn — Inventory nhận giá từ hệ thống bên ngoài hoặc nhập thủ công.
/// </summary>
public enum CostSource
{
    Manual = 1,
    Erp = 2,
    Pos = 3,
    PurchaseSystem = 4
}
