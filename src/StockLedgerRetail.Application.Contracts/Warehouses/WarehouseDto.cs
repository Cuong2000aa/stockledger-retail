using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Warehouses;

public class WarehouseDto
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public WarehouseType Type { get; set; }

    public Guid? ParentWarehouseId { get; set; }

    public WarehouseStatus Status { get; set; }

    public Guid? BrandId { get; set; }

    public string? RegionCode { get; set; }

    public int FulfillmentPriority { get; set; }

    public string? AddressLine { get; set; }

    public string? Ward { get; set; }

    public string? District { get; set; }

    public string? Province { get; set; }

    public string? PostalCode { get; set; }

    public string? Phone { get; set; }

    public string? ContactName { get; set; }

    public string? FullAddress { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
