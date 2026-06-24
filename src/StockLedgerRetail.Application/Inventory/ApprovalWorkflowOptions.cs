namespace StockLedgerRetail.Application.Inventory;

/// <summary>Ngưỡng giá trị phiếu/đơn để kích hoạt duyệt 2 cấp (VND).</summary>
public class ApprovalWorkflowOptions
{
    public const string SectionName = "ApprovalWorkflow";

    public decimal DocumentValueThreshold { get; set; } = 10_000_000m;
}
