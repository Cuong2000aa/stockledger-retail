using StockLedgerRetail.Domain.Entities;
using Microsoft.Extensions.Options;

namespace StockLedgerRetail.Application.Inventory;

public class ApprovalWorkflowHelper
{
    private readonly ApprovalWorkflowOptions _options;

    public ApprovalWorkflowHelper(IOptions<ApprovalWorkflowOptions> options)
    {
        _options = options.Value;
    }

    public int GetRequiredApprovalSteps(decimal documentValue) =>
        documentValue >= _options.DocumentValueThreshold ? 2 : 1;

    public static decimal CalculateInventoryDocumentValue(InventoryDocument document)
    {
        decimal total = 0;
        foreach (var line in document.Lines)
        {
            var unitCost = line.UnitCost ?? 0;
            total += line.Quantity * unitCost;
        }

        return total;
    }

    public static decimal CalculatePurchaseOrderValue(PurchaseOrder po)
    {
        decimal total = 0;
        foreach (var line in po.Lines)
        {
            total += line.OrderedQuantity * (line.UnitCost ?? 0);
        }

        return total;
    }
}
