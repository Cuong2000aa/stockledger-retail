using StockLedgerRetail.Audit;
using StockLedgerRetail.Application.Inventory;
using StockLedgerRetail.Common;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.GoodsReceipts;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.GoodsReceipts;

/// <summary>
/// Dịch vụ nhận hàng (Goods Receipt) — khi duyệt tạo phiếu nhập kho và cập nhật PO.
/// </summary>
public class GoodsReceiptAppService : IGoodsReceiptAppService
{
    private const string ProcurementSourceSystem = "PROCUREMENT";

    private readonly IGoodsReceiptRepository _goodsReceiptRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IInventoryDocumentRepository _inventoryDocumentRepository;
    private readonly IInventoryDocumentAppService _inventoryDocumentAppService;
    private readonly IProductVariantRepository _productVariantRepository;
    private readonly ITransactionAuditService _transactionAuditService;
    private readonly IAuditContext _auditContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUnitBarcodeStockService _unitBarcodeStockService;
    private readonly IPermissionAuthorizationService _permissionAuthorizationService;

    public GoodsReceiptAppService(
        IGoodsReceiptRepository goodsReceiptRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        IInventoryDocumentRepository inventoryDocumentRepository,
        IInventoryDocumentAppService inventoryDocumentAppService,
        IProductVariantRepository productVariantRepository,
        ITransactionAuditService transactionAuditService,
        IAuditContext auditContext,
        IUnitOfWork unitOfWork,
        IUnitBarcodeStockService unitBarcodeStockService,
        IPermissionAuthorizationService permissionAuthorizationService)
    {
        _goodsReceiptRepository = goodsReceiptRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _inventoryDocumentRepository = inventoryDocumentRepository;
        _inventoryDocumentAppService = inventoryDocumentAppService;
        _productVariantRepository = productVariantRepository;
        _transactionAuditService = transactionAuditService;
        _auditContext = auditContext;
        _unitOfWork = unitOfWork;
        _unitBarcodeStockService = unitBarcodeStockService;
        _permissionAuthorizationService = permissionAuthorizationService;
    }

    public async Task<PagedResultDto<GoodsReceiptDto>> GetListAsync(
        Guid? purchaseOrderId = null,
        GoodsReceiptStatus? status = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var (skip, take, normalizedPage, normalizedPageSize) = PagingNormalizer.Normalize(page, pageSize);
        var (items, totalCount) = await _goodsReceiptRepository.GetPagedListAsync(
            purchaseOrderId, status, skip, take, cancellationToken);

        return PagingNormalizer.Create(
            items.Select(MapToDtoWithoutLines).ToList(),
            totalCount,
            normalizedPage,
            normalizedPageSize);
    }

    public async Task<GoodsReceiptDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var gr = await _goodsReceiptRepository.GetByIdWithLinesAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Goods receipt '{id}' was not found.");

        var dto = MapToDto(gr);
        if (gr.InventoryDocumentId.HasValue)
        {
            var doc = await _inventoryDocumentRepository.GetByIdAsync(gr.InventoryDocumentId.Value, cancellationToken);
            dto.InventoryDocumentNo = doc?.DocumentNo;
        }

        return dto;
    }

    public async Task<GoodsReceiptDto> CreateAsync(
        CreateGoodsReceiptDto input,
        CancellationToken cancellationToken = default)
    {
        var po = await _purchaseOrderRepository.GetByIdWithLinesAsync(input.PurchaseOrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Purchase order '{input.PurchaseOrderId}' was not found.");

        if (po.Status is not PurchaseOrderStatus.Submitted and not PurchaseOrderStatus.PartiallyReceived)
        {
            throw new InvalidOperationException("Goods receipt can only be created for submitted purchase orders.");
        }

        ValidateReceiptLines(input.Lines, po);
        await ValidateReceiptLineBarcodesAsync(input.Lines, po, cancellationToken);

        var now = DateTime.UtcNow;
        var grId = Guid.NewGuid();
        var grNo = await GenerateGrNoAsync(now, cancellationToken);

        var gr = new GoodsReceipt
        {
            Id = grId,
            GrNo = grNo,
            PurchaseOrderId = po.Id,
            WarehouseId = po.WarehouseId,
            Status = GoodsReceiptStatus.Draft,
            ReceiptDate = input.ReceiptDate ?? now,
            ReferenceNo = input.ReferenceNo?.Trim(),
            Note = input.Note?.Trim(),
            CreatedBy = _auditContext.UserName,
            CreatedAt = now,
            Lines = await BuildGoodsReceiptLinesAsync(grId, input.Lines, po, cancellationToken)
        };

        await _goodsReceiptRepository.InsertAsync(gr, cancellationToken);
        await _goodsReceiptRepository.SaveChangesAsync(cancellationToken);

        var dto = await GetAsync(gr.Id, cancellationToken);
        await _transactionAuditService.LogAsync(nameof(GoodsReceipt), gr.Id, AuditActionType.Create, null, dto, cancellationToken);
        return dto;
    }

    public async Task<GoodsReceiptDto> ApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var gr = await _goodsReceiptRepository.GetByIdWithLinesAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Goods receipt '{id}' was not found.");

        if (gr.Status is not GoodsReceiptStatus.Draft)
        {
            throw new InvalidOperationException("Only draft goods receipts can be approved.");
        }

        await _permissionAuthorizationService.EnsureCanApproveInventoryDocumentAsync(
            gr.CreatedBy,
            cancellationToken);

        var po = await _purchaseOrderRepository.GetByIdWithLinesAsync(gr.PurchaseOrderId, cancellationToken)
            ?? throw new InvalidOperationException("Linked purchase order was not found.");

        ValidateReceiptAgainstPo(gr, po);

        var oldDto = MapToDto(gr);
        var referenceNo = gr.GrNo;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var existingDoc = await _inventoryDocumentRepository.GetBySourceReferenceAsync(
                ProcurementSourceSystem,
                referenceNo,
                InventoryDocumentType.StockIn,
                ct);

            InventoryDocumentDto stockInDoc;
            var applyReceiptToPo = false;

            if (existingDoc is not null)
            {
                switch (existingDoc.Status)
                {
                    case InventoryDocumentStatus.Draft:
                        if (string.IsNullOrWhiteSpace(existingDoc.SourceSystem))
                        {
                            existingDoc.SourceSystem = ProcurementSourceSystem;
                            await _inventoryDocumentRepository.UpdateAsync(existingDoc, ct);
                            await _inventoryDocumentRepository.SaveChangesAsync(ct);
                        }

                        stockInDoc = await _inventoryDocumentAppService.ApproveAsync(existingDoc.Id, ct);
                        applyReceiptToPo = true;
                        break;
                    case InventoryDocumentStatus.Approved:
                    case InventoryDocumentStatus.Completed:
                        stockInDoc = await _inventoryDocumentAppService.GetAsync(existingDoc.Id, ct);
                        applyReceiptToPo = !gr.InventoryDocumentId.HasValue;
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Cannot approve goods receipt because linked stock-in is in status '{existingDoc.Status}'.");
                }
            }
            else
            {
                var created = await _inventoryDocumentAppService.CreateStockInAsync(new CreateStockInDto
                {
                    DestinationWarehouseId = gr.WarehouseId,
                    DocumentDate = gr.ReceiptDate,
                    ReferenceNo = referenceNo,
                    SourceSystem = ProcurementSourceSystem,
                    Note = BuildStockInNote(gr),
                    Lines = gr.Lines.Select(l => new CreateInventoryDocumentLineDto
                    {
                        ProductVariantId = l.ProductVariantId,
                        Quantity = l.ReceivedQuantity,
                        UnitCost = l.UnitCost,
                        LotCode = l.LotCode,
                        Barcodes = BarcodeNormalization.FromLine(l),
                        ExpiryDate = l.ExpiryDate,
                        Note = l.Note
                    }).ToList()
                }, ct);

                stockInDoc = await _inventoryDocumentAppService.ApproveAsync(created.Id, ct);
                applyReceiptToPo = true;
            }

            if (applyReceiptToPo)
            {
                foreach (var grLine in gr.Lines)
                {
                    var poLine = po.Lines.First(l => l.Id == grLine.PurchaseOrderLineId);
                    poLine.ReceivedQuantity += grLine.ReceivedQuantity;
                }

                po.Status = po.Lines.All(l => l.ReceivedQuantity >= l.OrderedQuantity)
                    ? PurchaseOrderStatus.Received
                    : PurchaseOrderStatus.PartiallyReceived;

                await _purchaseOrderRepository.UpdateAsync(po, ct);
            }
            var now = DateTime.UtcNow;
            gr.Status = GoodsReceiptStatus.Approved;
            gr.InventoryDocumentId = stockInDoc.Id;
            gr.ApprovedBy = _auditContext.UserName;
            gr.ApprovedAt = now;

            await _goodsReceiptRepository.UpdateAsync(gr, ct);
        }, cancellationToken);

        var newDto = await GetAsync(gr.Id, cancellationToken);
        await _transactionAuditService.LogAsync(nameof(GoodsReceipt), gr.Id, AuditActionType.Approve, oldDto, newDto, cancellationToken);
        return newDto;
    }

    public async Task<GoodsReceiptDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var gr = await _goodsReceiptRepository.GetByIdWithLinesAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Goods receipt '{id}' was not found.");

        if (gr.Status is not GoodsReceiptStatus.Draft)
        {
            throw new InvalidOperationException("Only draft goods receipts can be cancelled.");
        }

        var oldDto = MapToDto(gr);
        gr.Status = GoodsReceiptStatus.Cancelled;

        await _goodsReceiptRepository.UpdateAsync(gr, cancellationToken);
        await _goodsReceiptRepository.SaveChangesAsync(cancellationToken);

        var newDto = await GetAsync(gr.Id, cancellationToken);
        await _transactionAuditService.LogAsync(nameof(GoodsReceipt), gr.Id, AuditActionType.Cancel, oldDto, newDto, cancellationToken);
        return newDto;
    }

    private async Task<string> GenerateGrNoAsync(DateTime now, CancellationToken cancellationToken)
    {
        var prefix = $"GR-{now:yyyyMMdd}-";
        var count = await _goodsReceiptRepository.CountByDatePrefixAsync(prefix, cancellationToken);
        return $"{prefix}{(count + 1).ToString().PadLeft(4, '0')}";
    }

    private static void ValidateReceiptLines(List<CreateGoodsReceiptLineDto> lines, PurchaseOrder po)
    {
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Goods receipt must contain at least one line.");
        }

        foreach (var line in lines)
        {
            if (line.ReceivedQuantity <= 0)
            {
                throw new InvalidOperationException("Received quantity must be greater than zero.");
            }

            var poLine = po.Lines.FirstOrDefault(l => l.Id == line.PurchaseOrderLineId);
            if (poLine is null)
            {
                throw new InvalidOperationException($"Purchase order line '{line.PurchaseOrderLineId}' was not found.");
            }

            var remaining = poLine.OrderedQuantity - poLine.ReceivedQuantity;
            if (line.ReceivedQuantity > remaining)
            {
                throw new InvalidOperationException(
                    $"Received quantity exceeds remaining for SKU line. Remaining: {remaining}, requested: {line.ReceivedQuantity}.");
            }
        }
    }

    private async Task<List<GoodsReceiptLine>> BuildGoodsReceiptLinesAsync(
        Guid goodsReceiptId,
        List<CreateGoodsReceiptLineDto> lines,
        PurchaseOrder po,
        CancellationToken cancellationToken)
    {
        var result = new List<GoodsReceiptLine>();

        foreach (var line in lines)
        {
            if (line.ReceivedQuantity <= 0)
            {
                continue;
            }

            var poLine = po.Lines.First(l => l.Id == line.PurchaseOrderLineId);
            var variant = await _productVariantRepository.GetByIdAsync(poLine.ProductVariantId, cancellationToken)
                ?? throw new InvalidOperationException($"Product variant '{poLine.ProductVariantId}' was not found.");

            var barcodes = BarcodeLineValidator.RequireNormalizedBarcodes(
                variant,
                line.ReceivedQuantity,
                line.Barcodes);

            var lineId = Guid.NewGuid();
            result.Add(new GoodsReceiptLine
            {
                Id = lineId,
                GoodsReceiptId = goodsReceiptId,
                PurchaseOrderLineId = line.PurchaseOrderLineId,
                ProductVariantId = poLine.ProductVariantId,
                ReceivedQuantity = line.ReceivedQuantity,
                UnitCost = poLine.UnitCost,
                LotCode = line.LotCode?.Trim(),
                ExpiryDate = line.ExpiryDate,
                Note = line.Note?.Trim(),
                UnitBarcodes = DocumentLineBarcodeFactory.CreateForGoodsReceiptLine(lineId, barcodes)
            });
        }

        return result;
    }

    private async Task ValidateReceiptLineBarcodesAsync(
        List<CreateGoodsReceiptLineDto> lines,
        PurchaseOrder po,
        CancellationToken cancellationToken)
    {
        foreach (var line in lines)
        {
            if (line.ReceivedQuantity <= 0)
            {
                continue;
            }

            var poLine = po.Lines.First(l => l.Id == line.PurchaseOrderLineId);
            var variant = await _productVariantRepository.GetByIdAsync(poLine.ProductVariantId, cancellationToken)
                ?? throw new InvalidOperationException($"Product variant '{poLine.ProductVariantId}' was not found.");

            var barcodes = BarcodeLineValidator.RequireNormalizedBarcodes(
                variant,
                line.ReceivedQuantity,
                line.Barcodes);

            if (variant.IsBarcode)
            {
                await _unitBarcodeStockService.ValidateInboundAsync(
                    variant.Id,
                    barcodes,
                    cancellationToken);
            }
        }
    }

    private static void ValidateReceiptAgainstPo(GoodsReceipt gr, PurchaseOrder po)
    {
        foreach (var grLine in gr.Lines)
        {
            var poLine = po.Lines.FirstOrDefault(l => l.Id == grLine.PurchaseOrderLineId);
            if (poLine is null)
            {
                throw new InvalidOperationException($"Purchase order line '{grLine.PurchaseOrderLineId}' was not found.");
            }

            var remaining = poLine.OrderedQuantity - poLine.ReceivedQuantity;
            if (grLine.ReceivedQuantity > remaining)
            {
                throw new InvalidOperationException(
                    $"Received quantity exceeds remaining. Remaining: {remaining}, requested: {grLine.ReceivedQuantity}.");
            }
        }
    }

    private static string BuildStockInNote(GoodsReceipt gr) =>
        $"[GR] {gr.GrNo}" + (string.IsNullOrWhiteSpace(gr.Note) ? string.Empty : $" | {gr.Note.Trim()}");

    private static GoodsReceiptDto MapToDto(GoodsReceipt gr) => new()
    {
        Id = gr.Id,
        GrNo = gr.GrNo,
        PurchaseOrderId = gr.PurchaseOrderId,
        PoNo = gr.PurchaseOrder?.PoNo ?? string.Empty,
        WarehouseId = gr.WarehouseId,
        WarehouseCode = gr.Warehouse?.Code ?? string.Empty,
        Status = gr.Status,
        ReceiptDate = gr.ReceiptDate,
        ReferenceNo = gr.ReferenceNo,
        Note = gr.Note,
        InventoryDocumentId = gr.InventoryDocumentId,
        CreatedBy = gr.CreatedBy,
        CreatedAt = gr.CreatedAt,
        ApprovedBy = gr.ApprovedBy,
        ApprovedAt = gr.ApprovedAt,
        Lines = gr.Lines.Select(l => new GoodsReceiptLineDto
        {
            Id = l.Id,
            PurchaseOrderLineId = l.PurchaseOrderLineId,
            ProductVariantId = l.ProductVariantId,
            Sku = l.ProductVariant?.Sku ?? string.Empty,
            ReceivedQuantity = l.ReceivedQuantity,
            UnitCost = l.UnitCost,
            LotCode = l.LotCode,
            Barcodes = BarcodeNormalization.FromLine(l),
            ExpiryDate = l.ExpiryDate,
            Note = l.Note
        }).ToList()
    };

    private static GoodsReceiptDto MapToDtoWithoutLines(GoodsReceipt gr) => new()
    {
        Id = gr.Id,
        GrNo = gr.GrNo,
        PurchaseOrderId = gr.PurchaseOrderId,
        PoNo = gr.PurchaseOrder?.PoNo ?? string.Empty,
        WarehouseId = gr.WarehouseId,
        WarehouseCode = gr.Warehouse?.Code ?? string.Empty,
        Status = gr.Status,
        ReceiptDate = gr.ReceiptDate,
        ReferenceNo = gr.ReferenceNo,
        Note = gr.Note,
        InventoryDocumentId = gr.InventoryDocumentId,
        CreatedBy = gr.CreatedBy,
        CreatedAt = gr.CreatedAt,
        ApprovedBy = gr.ApprovedBy,
        ApprovedAt = gr.ApprovedAt
    };
}
