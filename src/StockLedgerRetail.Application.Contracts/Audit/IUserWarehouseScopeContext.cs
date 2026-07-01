namespace StockLedgerRetail.Audit;

/// <summary>
/// Phạm vi kho của user đăng nhập. AllowedWarehouseIds = null nghĩa là không giới hạn.
/// </summary>
public interface IUserWarehouseScopeContext
{
    IReadOnlyCollection<Guid>? AllowedWarehouseIds { get; }

    Guid? PrimaryWarehouseId { get; }
}
