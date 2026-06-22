using System.ComponentModel.DataAnnotations;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Warehouses;

public class CreateWarehouseDto
{
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public WarehouseType Type { get; set; }

    public Guid? ParentWarehouseId { get; set; }

    public WarehouseStatus Status { get; set; } = WarehouseStatus.Active;

    [MaxLength(300)]
    public string? AddressLine { get; set; }

    [MaxLength(100)]
    public string? Ward { get; set; }

    [MaxLength(100)]
    public string? District { get; set; }

    [MaxLength(100)]
    public string? Province { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? ContactName { get; set; }
}
