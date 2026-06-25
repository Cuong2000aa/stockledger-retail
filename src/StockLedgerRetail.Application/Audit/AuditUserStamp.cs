using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Audit;

/// <summary>Gán CreatedBy/UpdatedBy cho entity và phiếu nghiệp vụ.</summary>
public static class AuditUserStamp
{
    public static void OnCreate(AuditedEntity entity, IAuditContext context, DateTime now)
    {
        var user = context.UserName;
        entity.CreatedBy = user;
        entity.CreatedAt = now;
        entity.UpdatedBy = user;
        entity.UpdatedAt = now;
    }

    public static void OnUpdate(AuditedEntity entity, IAuditContext context, DateTime now)
    {
        entity.UpdatedBy = context.UserName;
        entity.UpdatedAt = now;
    }

    public static void Touch(InventoryDocument document, IAuditContext context, DateTime now)
    {
        document.UpdatedBy = context.UserName;
        document.UpdatedAt = now;
    }

    public static void Touch(GoodsReceipt receipt, IAuditContext context, DateTime now)
    {
        receipt.UpdatedBy = context.UserName;
        receipt.UpdatedAt = now;
    }

    public static void Touch(PurchaseOrder order, IAuditContext context, DateTime now)
    {
        order.UpdatedBy = context.UserName;
        order.UpdatedAt = now;
    }
}
