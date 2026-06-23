namespace StockLedgerRetail.Enums;

/// <summary>Cách chọn kho xuất khi client không chỉ định warehouseId.</summary>
public enum WarehouseSelectionMode
{
    /// <summary>Ưu tiên cửa hàng trước DC, rồi tồn khả dụng cao nhất (bottleneck).</summary>
    StoreFirst = 1,

    /// <summary>Chọn kho có tồn khả dụng tối thiểu cao nhất trên toàn bộ dòng.</summary>
    HighestAvailableStock = 2,

    /// <summary>Theo thứ tự CandidateWarehouseIds.</summary>
    CandidateOrder = 3
}
