namespace StockLedgerRetail.Domain.Entities;

/// <summary>Tồn theo lô tại từng kho — chi tiết bổ sung cho CurrentStock.</summary>
public class LotStock
{
    public Guid Id { get; set; }

    public Guid StockLotId { get; set; }

    public Guid WarehouseId { get; set; }

    public decimal QuantityOnHand { get; set; }

    public DateTime LastUpdatedAt { get; set; }

    public StockLot? StockLot { get; set; }

    public Warehouse? Warehouse { get; set; }
}
