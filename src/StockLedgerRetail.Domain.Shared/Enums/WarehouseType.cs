namespace StockLedgerRetail.Enums;

public enum WarehouseType
{
    Dc = 1,
    Store = 2,
    SubWarehouse = 3,
    Defect = 4,
    Return = 5,

    /// <summary>Kho ảo giữ tồn đang luân chuyển (in-transit) theo brand.</summary>
    InTransit = 6
}
