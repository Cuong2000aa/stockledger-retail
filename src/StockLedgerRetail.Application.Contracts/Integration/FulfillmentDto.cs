using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Integration;

public class FulfillmentScopeDto
{
    /// <summary>Kho xuất cố định. Null = tự chọn trong phạm vi candidate / fulfillment types.</summary>
    public Guid? WarehouseId { get; set; }

    /// <summary>Danh sách kho ứng viên (ưu tiên theo SelectionMode).</summary>
    public List<Guid>? CandidateWarehouseIds { get; set; }

    public WarehouseSelectionMode SelectionMode { get; set; } = WarehouseSelectionMode.StoreFirst;

    /// <summary>Kho ưu tiên nếu đủ tồn.</summary>
    public Guid? PreferredWarehouseId { get; set; }
}

public class WarehouseAvailabilityLineDto
{
    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public Guid? ProductVariantId { get; set; }

    public decimal RequestedQuantity { get; set; }

    public decimal AvailableQuantity { get; set; }

    public bool IsAvailable { get; set; }
}

public class WarehouseFulfillmentSummaryDto
{
    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public string WarehouseName { get; set; } = string.Empty;

    public WarehouseType WarehouseType { get; set; }

    public bool CanFulfillAll { get; set; }

    public decimal BottleneckAvailableQuantity { get; set; }

    public List<WarehouseAvailabilityLineDto> Lines { get; set; } = new();
}

public class CheckMultiWarehouseAvailabilityRequestDto
{
    public List<SalesLineRequestDto> Lines { get; set; } = new();

    public Guid? WarehouseId { get; set; }

    public List<Guid>? CandidateWarehouseIds { get; set; }
}

public class CheckMultiWarehouseAvailabilityResponseDto
{
    public bool CanFulfillAll { get; set; }

    public List<Guid> FulfillableWarehouseIds { get; set; } = new();

    public List<WarehouseFulfillmentSummaryDto> Warehouses { get; set; } = new();
}

public class AllocateWarehouseRequestDto : FulfillmentScopeDto
{
    public List<SalesLineRequestDto> Lines { get; set; } = new();
}

public class AllocateWarehouseResponseDto
{
    public Guid SelectedWarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public string WarehouseName { get; set; } = string.Empty;

    public WarehouseSelectionMode SelectionMode { get; set; }

    public bool CanFulfillAll { get; set; }

    public List<SalesAvailabilityLineDto> Lines { get; set; } = new();
}
