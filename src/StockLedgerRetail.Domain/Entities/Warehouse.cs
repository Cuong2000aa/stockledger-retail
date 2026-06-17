using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Entities;

public class Warehouse : AuditedEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public WarehouseType Type { get; set; }

    public Guid? ParentWarehouseId { get; set; }

    public WarehouseStatus Status { get; set; } = WarehouseStatus.Active;

    public Warehouse? ParentWarehouse { get; set; }

    public ICollection<Warehouse> ChildWarehouses { get; set; } = new List<Warehouse>();

    public ICollection<CurrentStock> CurrentStocks { get; set; } = new List<CurrentStock>();
}
