namespace StockLedgerRetail.Integration;

public class StockDeltaQueryDto
{
    /// <summary>Lấy các biến động tồn phát sinh từ mốc thời gian này (UTC).</summary>
    public DateTime SinceUtc { get; set; }

    /// <summary>Lọc theo kho cụ thể (tùy chọn).</summary>
    public Guid? WarehouseId { get; set; }

    /// <summary>Lọc theo thương hiệu (tùy chọn).</summary>
    public Guid? BrandId { get; set; }

    /// <summary>Số lượng tối đa mỗi trang (mặc định 500, tối đa 2000).</summary>
    public int Limit { get; set; } = 500;
}

public class StockDeltaItemDto
{
    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public string WarehouseName { get; set; } = string.Empty;

    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string? Size { get; set; }

    public string? Color { get; set; }

    public decimal OnHandQuantity { get; set; }

    public decimal ReservedQuantity { get; set; }

    public decimal AvailableQuantity { get; set; }

    public DateTime LastMovementAtUtc { get; set; }
}

public class StockDeltaResponseDto
{
    public DateTime SinceUtc { get; set; }

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

    public int TotalChanges { get; set; }

    public bool HasMore { get; set; }

    public List<StockDeltaItemDto> Items { get; set; } = new();
}
