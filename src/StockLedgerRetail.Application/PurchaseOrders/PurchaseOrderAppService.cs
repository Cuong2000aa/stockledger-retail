using StockLedgerRetail.Audit;
using StockLedgerRetail.Application.Inventory;
using StockLedgerRetail.Common;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.PurchaseOrders;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.PurchaseOrders;

/// <summary>Dịch vụ quản lý đơn mua hàng (Purchase Order).</summary>
public class PurchaseOrderAppService : IPurchaseOrderAppService
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IProductVariantRepository _productVariantRepository;
    private readonly ITransactionAuditService _transactionAuditService;
    private readonly IAuditContext _auditContext;
    private readonly ApprovalWorkflowHelper _approvalWorkflowHelper;
    private readonly IPermissionAuthorizationService _permissionAuthorizationService;

    public PurchaseOrderAppService(
        IPurchaseOrderRepository purchaseOrderRepository,
        ISupplierRepository supplierRepository,
        IWarehouseRepository warehouseRepository,
        IProductVariantRepository productVariantRepository,
        ITransactionAuditService transactionAuditService,
        IAuditContext auditContext,
        ApprovalWorkflowHelper approvalWorkflowHelper,
        IPermissionAuthorizationService permissionAuthorizationService)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _supplierRepository = supplierRepository;
        _warehouseRepository = warehouseRepository;
        _productVariantRepository = productVariantRepository;
        _transactionAuditService = transactionAuditService;
        _auditContext = auditContext;
        _approvalWorkflowHelper = approvalWorkflowHelper;
        _permissionAuthorizationService = permissionAuthorizationService;
    }

    public async Task<PagedResultDto<PurchaseOrderDto>> GetListAsync(
        PurchaseOrderStatus? status = null,
        Guid? supplierId = null,
        int? page = null,
        int? pageSize = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var (skip, take, normalizedPage, normalizedPageSize) = PagingNormalizer.Normalize(page, pageSize);
        var (items, totalCount) = await _purchaseOrderRepository.GetPagedListAsync(
            status, supplierId, skip, take, search, cancellationToken);

        return PagingNormalizer.Create(
            items.Select(MapToDtoWithoutLines).ToList(),
            totalCount,
            normalizedPage,
            normalizedPageSize);
    }

    public async Task<PurchaseOrderDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var po = await _purchaseOrderRepository.GetByIdWithLinesAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Purchase order '{id}' was not found.");
        return MapToDto(po);
    }

    public async Task<PurchaseOrderDto> CreateAsync(
        CreatePurchaseOrderDto input,
        CancellationToken cancellationToken = default)
    {
        await EnsureSupplierExistsAsync(input.SupplierId, cancellationToken);
        await EnsureWarehouseExistsAsync(input.WarehouseId, cancellationToken);
        ValidateLines(input.Lines);
        await EnsureVariantsExistAsync(input.Lines, cancellationToken);
        await ValidateLineBarcodesAsync(input.Lines, cancellationToken);

        var now = DateTime.UtcNow;
        var poId = Guid.NewGuid();
        var poNo = await GeneratePoNoAsync(now, cancellationToken);

        var po = new PurchaseOrder
        {
            Id = poId,
            PoNo = poNo,
            SupplierId = input.SupplierId,
            WarehouseId = input.WarehouseId,
            Status = PurchaseOrderStatus.Draft,
            OrderDate = input.OrderDate ?? now,
            ExpectedDate = input.ExpectedDate,
            ReferenceNo = input.ReferenceNo?.Trim(),
            Note = input.Note?.Trim(),
            CreatedBy = _auditContext.UserName,
            CreatedAt = now,
            Lines = await BuildPurchaseOrderLinesAsync(poId, input.Lines, cancellationToken)
        };

        await _purchaseOrderRepository.InsertAsync(po, cancellationToken);
        await _purchaseOrderRepository.SaveChangesAsync(cancellationToken);

        var dto = await GetAsync(po.Id, cancellationToken);
        await _transactionAuditService.LogAsync(nameof(PurchaseOrder), po.Id, AuditActionType.Create, null, dto, cancellationToken);
        return dto;
    }

    public async Task<PurchaseOrderDto> SubmitAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var po = await _purchaseOrderRepository.GetByIdWithLinesAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Purchase order '{id}' was not found.");

        if (po.Status is not PurchaseOrderStatus.Draft)
        {
            throw new InvalidOperationException("Only draft purchase orders can be submitted.");
        }

        if (po.Lines.Count == 0)
        {
            throw new InvalidOperationException("Purchase order must contain at least one line.");
        }

        var oldDto = MapToDto(po);
        var value = ApprovalWorkflowHelper.CalculatePurchaseOrderValue(po);
        var requiredSteps = _approvalWorkflowHelper.GetRequiredApprovalSteps(value);
        var now = DateTime.UtcNow;

        po.RequiredApprovalSteps = requiredSteps;
        po.CompletedApprovalSteps = 0;
        po.SubmittedAt = now;
        po.Status = requiredSteps > 1
            ? PurchaseOrderStatus.PendingApproval
            : PurchaseOrderStatus.Submitted;

        await _purchaseOrderRepository.UpdateAsync(po, cancellationToken);
        await _purchaseOrderRepository.SaveChangesAsync(cancellationToken);

        var newDto = await GetAsync(po.Id, cancellationToken);
        await _transactionAuditService.LogAsync(nameof(PurchaseOrder), po.Id, AuditActionType.Approve, oldDto, newDto, cancellationToken);
        return newDto;
    }

    public async Task<PurchaseOrderDto> ApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var po = await _purchaseOrderRepository.GetByIdWithLinesAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Purchase order '{id}' was not found.");

        if (po.Status is not PurchaseOrderStatus.PendingApproval)
        {
            throw new InvalidOperationException("Only purchase orders pending approval can be approved.");
        }

        await _permissionAuthorizationService.EnsureCanApproveInventoryDocumentAsync(
            po.CreatedBy,
            cancellationToken);

        var oldDto = MapToDto(po);
        var now = DateTime.UtcNow;

        if (po.CompletedApprovalSteps + 1 < po.RequiredApprovalSteps)
        {
            po.CompletedApprovalSteps++;
            po.FirstApprovedBy = _auditContext.UserName;
            po.FirstApprovedAt = now;
        }
        else
        {
            po.Status = PurchaseOrderStatus.Submitted;
            po.ApprovedBy = _auditContext.UserName;
            po.ApprovedAt = now;
        }

        await _purchaseOrderRepository.UpdateAsync(po, cancellationToken);
        await _purchaseOrderRepository.SaveChangesAsync(cancellationToken);

        var newDto = await GetAsync(po.Id, cancellationToken);
        await _transactionAuditService.LogAsync(nameof(PurchaseOrder), po.Id, AuditActionType.Approve, oldDto, newDto, cancellationToken);
        return newDto;
    }

    public async Task<PurchaseOrderDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var po = await _purchaseOrderRepository.GetByIdWithLinesAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Purchase order '{id}' was not found.");

        if (po.Status is PurchaseOrderStatus.Received or PurchaseOrderStatus.Cancelled)
        {
            throw new InvalidOperationException("This purchase order cannot be cancelled.");
        }

        if (po.Lines.Any(l => l.ReceivedQuantity > 0))
        {
            throw new InvalidOperationException("Cannot cancel a purchase order that has received goods.");
        }

        var oldDto = MapToDto(po);
        po.Status = PurchaseOrderStatus.Cancelled;
        po.CancelledAt = DateTime.UtcNow;

        await _purchaseOrderRepository.UpdateAsync(po, cancellationToken);
        await _purchaseOrderRepository.SaveChangesAsync(cancellationToken);

        var newDto = await GetAsync(po.Id, cancellationToken);
        await _transactionAuditService.LogAsync(nameof(PurchaseOrder), po.Id, AuditActionType.Cancel, oldDto, newDto, cancellationToken);
        return newDto;
    }

    private async Task<string> GeneratePoNoAsync(DateTime now, CancellationToken cancellationToken)
    {
        var prefix = $"PO-{now:yyyyMMdd}-";
        var count = await _purchaseOrderRepository.CountByDatePrefixAsync(prefix, cancellationToken);
        return $"{prefix}{(count + 1).ToString().PadLeft(4, '0')}";
    }

    private static void ValidateLines(List<CreatePurchaseOrderLineDto> lines)
    {
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Purchase order must contain at least one line.");
        }

        foreach (var line in lines)
        {
            if (line.OrderedQuantity <= 0)
            {
                throw new InvalidOperationException("Ordered quantity must be greater than zero.");
            }
        }
    }

    private async Task EnsureVariantsExistAsync(
        List<CreatePurchaseOrderLineDto> lines,
        CancellationToken cancellationToken)
    {
        foreach (var line in lines)
        {
            var variant = await _productVariantRepository.GetByIdAsync(line.ProductVariantId, cancellationToken);
            if (variant is null)
            {
                throw new InvalidOperationException($"Product variant '{line.ProductVariantId}' was not found.");
            }
        }
    }

    private async Task<List<PurchaseOrderLine>> BuildPurchaseOrderLinesAsync(
        Guid purchaseOrderId,
        List<CreatePurchaseOrderLineDto> lines,
        CancellationToken cancellationToken)
    {
        var result = new List<PurchaseOrderLine>();

        foreach (var line in lines)
        {
            var variant = await _productVariantRepository.GetByIdAsync(line.ProductVariantId, cancellationToken)
                ?? throw new InvalidOperationException($"Product variant '{line.ProductVariantId}' was not found.");

            var barcodes = BarcodeLineValidator.RequireNormalizedBarcodes(
                variant,
                line.OrderedQuantity,
                line.Barcodes);

            var lineId = Guid.NewGuid();
            result.Add(new PurchaseOrderLine
            {
                Id = lineId,
                PurchaseOrderId = purchaseOrderId,
                ProductVariantId = line.ProductVariantId,
                OrderedQuantity = line.OrderedQuantity,
                ReceivedQuantity = 0,
                UnitCost = line.UnitCost,
                Note = line.Note?.Trim(),
                UnitBarcodes = DocumentLineBarcodeFactory.CreateForPurchaseOrderLine(lineId, barcodes)
            });
        }

        return result;
    }

    private async Task ValidateLineBarcodesAsync(
        List<CreatePurchaseOrderLineDto> lines,
        CancellationToken cancellationToken)
    {
        foreach (var line in lines)
        {
            var variant = await _productVariantRepository.GetByIdAsync(line.ProductVariantId, cancellationToken)
                ?? throw new InvalidOperationException($"Product variant '{line.ProductVariantId}' was not found.");

            BarcodeLineValidator.RequireNormalizedBarcodes(
                variant,
                line.OrderedQuantity,
                line.Barcodes);
        }
    }

    private async Task EnsureSupplierExistsAsync(Guid supplierId, CancellationToken cancellationToken)
    {
        var supplier = await _supplierRepository.GetByIdAsync(supplierId, cancellationToken);
        if (supplier is null)
        {
            throw new InvalidOperationException($"Supplier '{supplierId}' was not found.");
        }
    }

    private async Task EnsureWarehouseExistsAsync(Guid warehouseId, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId, cancellationToken);
        if (warehouse is null)
        {
            throw new InvalidOperationException($"Warehouse '{warehouseId}' was not found.");
        }
    }

    private static PurchaseOrderDto MapToDto(PurchaseOrder po) => new()
    {
        Id = po.Id,
        PoNo = po.PoNo,
        SupplierId = po.SupplierId,
        SupplierCode = po.Supplier?.Code ?? string.Empty,
        SupplierName = po.Supplier?.Name ?? string.Empty,
        WarehouseId = po.WarehouseId,
        WarehouseCode = po.Warehouse?.Code ?? string.Empty,
        WarehouseName = po.Warehouse?.Name ?? string.Empty,
        Status = po.Status,
        OrderDate = po.OrderDate,
        ExpectedDate = po.ExpectedDate,
        ReferenceNo = po.ReferenceNo,
        Note = po.Note,
        CreatedBy = po.CreatedBy,
        CreatedAt = po.CreatedAt,
        SubmittedAt = po.SubmittedAt,
        RequiredApprovalSteps = po.RequiredApprovalSteps,
        CompletedApprovalSteps = po.CompletedApprovalSteps,
        FirstApprovedBy = po.FirstApprovedBy,
        FirstApprovedAt = po.FirstApprovedAt,
        ApprovedBy = po.ApprovedBy,
        ApprovedAt = po.ApprovedAt,
        Lines = po.Lines.Select(l => new PurchaseOrderLineDto
        {
            Id = l.Id,
            ProductVariantId = l.ProductVariantId,
            Sku = l.ProductVariant?.Sku ?? string.Empty,
            OrderedQuantity = l.OrderedQuantity,
            ReceivedQuantity = l.ReceivedQuantity,
            UnitCost = l.UnitCost,
            Barcodes = BarcodeNormalization.FromLine(l),
            Note = l.Note
        }).ToList()
    };

    private static PurchaseOrderDto MapToDtoWithoutLines(PurchaseOrder po) => new()
    {
        Id = po.Id,
        PoNo = po.PoNo,
        SupplierId = po.SupplierId,
        SupplierCode = po.Supplier?.Code ?? string.Empty,
        SupplierName = po.Supplier?.Name ?? string.Empty,
        WarehouseId = po.WarehouseId,
        WarehouseCode = po.Warehouse?.Code ?? string.Empty,
        WarehouseName = po.Warehouse?.Name ?? string.Empty,
        Status = po.Status,
        OrderDate = po.OrderDate,
        ExpectedDate = po.ExpectedDate,
        ReferenceNo = po.ReferenceNo,
        Note = po.Note,
        CreatedBy = po.CreatedBy,
        CreatedAt = po.CreatedAt,
        SubmittedAt = po.SubmittedAt,
        RequiredApprovalSteps = po.RequiredApprovalSteps,
        CompletedApprovalSteps = po.CompletedApprovalSteps,
        FirstApprovedBy = po.FirstApprovedBy,
        FirstApprovedAt = po.FirstApprovedAt,
        ApprovedBy = po.ApprovedBy,
        ApprovedAt = po.ApprovedAt
    };
}
