using StockLedgerRetail.Audit;
using StockLedgerRetail.Application.Inventory;
using StockLedgerRetail.Common;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Inventory;

/// <summary>
/// Dịch vụ quản lý phiếu nghiệp vụ tồn kho — tạo nhập/xuất/điều chỉnh (Draft), duyệt và hủy phiếu.
/// </summary>
public class InventoryDocumentAppService : IInventoryDocumentAppService
{
    private readonly IInventoryDocumentRepository _inventoryDocumentRepository;
    private readonly IProductVariantRepository _productVariantRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IStockLedgerService _stockLedgerService;
    private readonly ITransactionAuditService _transactionAuditService;
    private readonly IAuditContext _auditContext;

    public InventoryDocumentAppService(
        IInventoryDocumentRepository inventoryDocumentRepository,
        IProductVariantRepository productVariantRepository,
        IWarehouseRepository warehouseRepository,
        IStockLedgerService stockLedgerService,
        ITransactionAuditService transactionAuditService,
        IAuditContext auditContext)
    {
        _inventoryDocumentRepository = inventoryDocumentRepository;
        _productVariantRepository = productVariantRepository;
        _warehouseRepository = warehouseRepository;
        _stockLedgerService = stockLedgerService;
        _transactionAuditService = transactionAuditService;
        _auditContext = auditContext;
    }

    /// <summary>Lấy chi tiết phiếu kèm danh sách dòng hàng.</summary>
    public async Task<InventoryDocumentDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _inventoryDocumentRepository.GetByIdWithLinesAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Inventory document '{id}' was not found.");

        return MapToDto(document);
    }

    /// <summary>Lấy danh sách phiếu có phân trang, có thể lọc theo loại.</summary>
    public async Task<PagedResultDto<InventoryDocumentDto>> GetListAsync(
        InventoryDocumentType? documentType = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var (skip, take, normalizedPage, normalizedPageSize) = PagingNormalizer.Normalize(page, pageSize);
        var (documents, totalCount) = await _inventoryDocumentRepository.GetPagedListAsync(
            documentType, skip, take, cancellationToken);

        var items = documents.Select(MapToDtoWithoutLines).ToList();
        return PagingNormalizer.Create(items, totalCount, normalizedPage, normalizedPageSize);
    }

    /// <summary>Tạo phiếu nhập kho ở trạng thái Draft — chưa tăng tồn.</summary>
    public async Task<InventoryDocumentDto> CreateStockInAsync(
        CreateStockInDto input,
        CancellationToken cancellationToken = default)
    {
        await EnsureWarehouseExistsAsync(input.DestinationWarehouseId, cancellationToken);
        await EnsureProductVariantsExistAsync(input.Lines, cancellationToken);
        ValidateLines(input.Lines);

        var now = DateTime.UtcNow;
        var document = await CreateDocumentAsync(
            InventoryDocumentType.StockIn,
            sourceWarehouseId: null,
            destinationWarehouseId: input.DestinationWarehouseId,
            documentDate: input.DocumentDate ?? now,
            referenceNo: input.ReferenceNo,
            sourceSystem: input.SourceSystem,
            note: input.Note,
            lines: input.Lines,
            now: now,
            cancellationToken);

        await _inventoryDocumentRepository.InsertAsync(document, cancellationToken);
        await _inventoryDocumentRepository.SaveChangesAsync(cancellationToken);

        var dto = await LoadDtoAsync(document.Id, cancellationToken);
        await _transactionAuditService.LogAsync(
            nameof(InventoryDocument), document.Id, AuditActionType.Create, null, dto, cancellationToken);

        return dto;
    }

    /// <summary>Tạo phiếu xuất kho ở trạng thái Draft — chưa giảm tồn.</summary>
    public async Task<InventoryDocumentDto> CreateStockOutAsync(
        CreateStockOutDto input,
        CancellationToken cancellationToken = default)
    {
        await EnsureWarehouseExistsAsync(input.SourceWarehouseId, cancellationToken);
        await EnsureProductVariantsExistAsync(input.Lines, cancellationToken);
        ValidateLines(input.Lines);

        var now = DateTime.UtcNow;
        var document = await CreateDocumentAsync(
            InventoryDocumentType.StockOut,
            sourceWarehouseId: input.SourceWarehouseId,
            destinationWarehouseId: null,
            documentDate: input.DocumentDate ?? now,
            referenceNo: input.ReferenceNo,
            sourceSystem: input.SourceSystem,
            note: input.Note,
            lines: input.Lines,
            now: now,
            cancellationToken);

        await _inventoryDocumentRepository.InsertAsync(document, cancellationToken);
        await _inventoryDocumentRepository.SaveChangesAsync(cancellationToken);

        var dto = await LoadDtoAsync(document.Id, cancellationToken);
        await _transactionAuditService.LogAsync(
            nameof(InventoryDocument), document.Id, AuditActionType.Create, null, dto, cancellationToken);

        return dto;
    }

    /// <summary>Tạo phiếu điều chỉnh tồn ở trạng thái Draft — số lượng dòng có dấu (+/-).</summary>
    public async Task<InventoryDocumentDto> CreateAdjustmentAsync(
        CreateAdjustmentDto input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Reason))
        {
            throw new InvalidOperationException("Adjustment reason is required.");
        }

        await EnsureWarehouseExistsAsync(input.WarehouseId, cancellationToken);
        ValidateAdjustmentLines(input.Lines);
        await EnsureAdjustmentVariantsExistAsync(input.Lines, cancellationToken);

        var documentLines = input.Lines.Select(line => new CreateInventoryDocumentLineDto
        {
            ProductVariantId = line.ProductVariantId,
            Quantity = line.AdjustmentQuantity,
            Note = line.Note
        }).ToList();

        var now = DateTime.UtcNow;
        var document = await CreateDocumentAsync(
            InventoryDocumentType.Adjustment,
            sourceWarehouseId: null,
            destinationWarehouseId: input.WarehouseId,
            documentDate: input.DocumentDate ?? now,
            referenceNo: input.ReferenceNo,
            sourceSystem: null,
            note: BuildAdjustmentNote(input.Reason, input.Note),
            lines: documentLines,
            now: now,
            cancellationToken);

        await _inventoryDocumentRepository.InsertAsync(document, cancellationToken);
        await _inventoryDocumentRepository.SaveChangesAsync(cancellationToken);

        var dto = await LoadDtoAsync(document.Id, cancellationToken);
        await _transactionAuditService.LogAsync(
            nameof(InventoryDocument), document.Id, AuditActionType.Create, null, dto, cancellationToken);

        return dto;
    }

    /// <summary>Tạo phiếu chuyển kho ở trạng thái Draft — chưa tác động tồn.</summary>
    public async Task<InventoryDocumentDto> CreateTransferAsync(
        CreateTransferDto input,
        CancellationToken cancellationToken = default)
    {
        if (input.SourceWarehouseId == input.DestinationWarehouseId)
        {
            throw new InvalidOperationException("Source and destination warehouse cannot be the same.");
        }

        await EnsureWarehouseExistsAsync(input.SourceWarehouseId, cancellationToken);
        await EnsureWarehouseExistsAsync(input.DestinationWarehouseId, cancellationToken);
        await EnsureProductVariantsExistAsync(input.Lines, cancellationToken);
        ValidateLines(input.Lines);

        var now = DateTime.UtcNow;
        var document = await CreateDocumentAsync(
            InventoryDocumentType.Transfer,
            sourceWarehouseId: input.SourceWarehouseId,
            destinationWarehouseId: input.DestinationWarehouseId,
            documentDate: input.DocumentDate ?? now,
            referenceNo: input.ReferenceNo,
            sourceSystem: null,
            note: input.Note,
            lines: input.Lines,
            now: now,
            cancellationToken);

        await _inventoryDocumentRepository.InsertAsync(document, cancellationToken);
        await _inventoryDocumentRepository.SaveChangesAsync(cancellationToken);

        var dto = await LoadDtoAsync(document.Id, cancellationToken);
        await _transactionAuditService.LogAsync(
            nameof(InventoryDocument), document.Id, AuditActionType.Create, null, dto, cancellationToken);

        return dto;
    }

    /// <summary>Tạo phiếu kiểm kê ở trạng thái Draft — dòng lưu số lượng kiểm thực tế.</summary>
    public async Task<InventoryDocumentDto> CreateStockCountAsync(
        CreateStockCountDto input,
        CancellationToken cancellationToken = default)
    {
        await EnsureWarehouseExistsAsync(input.WarehouseId, cancellationToken);
        ValidateStockCountLines(input.Lines);
        await EnsureStockCountVariantsExistAsync(input.Lines, cancellationToken);

        var documentLines = input.Lines.Select(line => new CreateInventoryDocumentLineDto
        {
            ProductVariantId = line.ProductVariantId,
            Quantity = line.CountedQuantity,
            Note = line.Note
        }).ToList();

        var now = DateTime.UtcNow;
        var document = await CreateDocumentAsync(
            InventoryDocumentType.StockCount,
            sourceWarehouseId: null,
            destinationWarehouseId: input.WarehouseId,
            documentDate: input.DocumentDate ?? now,
            referenceNo: input.ReferenceNo,
            sourceSystem: null,
            note: input.Note,
            lines: documentLines,
            now: now,
            cancellationToken);

        await _inventoryDocumentRepository.InsertAsync(document, cancellationToken);
        await _inventoryDocumentRepository.SaveChangesAsync(cancellationToken);

        var dto = await LoadDtoAsync(document.Id, cancellationToken);
        await _transactionAuditService.LogAsync(
            nameof(InventoryDocument), document.Id, AuditActionType.Create, null, dto, cancellationToken);

        return dto;
    }

    /// <summary>Cập nhật phiếu Draft — chỉ ngày, tham chiếu, ghi chú và dòng hàng.</summary>
    public async Task<InventoryDocumentDto> UpdateDraftAsync(
        Guid id,
        UpdateInventoryDocumentDraftDto input,
        CancellationToken cancellationToken = default)
    {
        var document = await _inventoryDocumentRepository.GetByIdWithLinesAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Inventory document '{id}' was not found.");

        if (document.Status is not InventoryDocumentStatus.Draft)
        {
            throw new InvalidOperationException("Only draft documents can be updated.");
        }

        var oldDto = MapToDto(document);

        if (input.DocumentDate.HasValue)
        {
            document.DocumentDate = input.DocumentDate.Value;
        }

        if (input.ReferenceNo is not null)
        {
            document.ReferenceNo = string.IsNullOrWhiteSpace(input.ReferenceNo) ? null : input.ReferenceNo.Trim();
        }

        if (input.Note is not null)
        {
            document.Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
        }

        if (input.Lines is not null)
        {
            ValidateDraftLines(document.DocumentType, input.Lines);
            await EnsureProductVariantsExistAsync(input.Lines, cancellationToken);

            await _inventoryDocumentRepository.RemoveLinesByDocumentIdAsync(document.Id, cancellationToken);

            document.Lines = input.Lines.Select(line => new InventoryDocumentLine
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                ProductVariantId = line.ProductVariantId,
                Quantity = line.Quantity,
                UnitCost = line.UnitCost,
                Note = line.Note?.Trim()
            }).ToList();
        }

        await _inventoryDocumentRepository.UpdateAsync(document, cancellationToken);
        await _inventoryDocumentRepository.SaveChangesAsync(cancellationToken);

        var newDto = await LoadDtoAsync(document.Id, cancellationToken);
        await _transactionAuditService.LogAsync(
            nameof(InventoryDocument), document.Id, AuditActionType.Update, oldDto, newDto, cancellationToken);

        return newDto;
    }

    /// <summary>Duyệt phiếu — gọi StockLedgerService sinh giao dịch và cập nhật tồn.</summary>
    public async Task<InventoryDocumentDto> ApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _inventoryDocumentRepository.GetByIdWithLinesAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Inventory document '{id}' was not found.");

        if (document.Status is InventoryDocumentStatus.Approved or InventoryDocumentStatus.Completed)
        {
            throw new InvalidOperationException("Document is already approved.");
        }

        if (document.Status is InventoryDocumentStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled documents cannot be approved.");
        }

        var oldDto = MapToDto(document);

        await _stockLedgerService.ProcessApprovedDocumentAsync(document, cancellationToken);

        var now = DateTime.UtcNow;
        document.Status = InventoryDocumentStatus.Approved;
        document.ApprovedBy = _auditContext.UserName;
        document.ApprovedAt = now;

        await _inventoryDocumentRepository.UpdateAsync(document, cancellationToken);
        await _inventoryDocumentRepository.SaveChangesAsync(cancellationToken);

        var newDto = await LoadDtoAsync(document.Id, cancellationToken);
        await _transactionAuditService.LogAsync(
            nameof(InventoryDocument), document.Id, AuditActionType.Approve, oldDto, newDto, cancellationToken);

        return newDto;
    }

    /// <summary>Hủy phiếu Draft — không tác động tồn kho.</summary>
    public async Task<InventoryDocumentDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _inventoryDocumentRepository.GetByIdWithLinesAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Inventory document '{id}' was not found.");

        if (document.Status is not InventoryDocumentStatus.Draft)
        {
            throw new InvalidOperationException("Only draft documents can be cancelled.");
        }

        var oldDto = MapToDto(document);
        document.Status = InventoryDocumentStatus.Cancelled;

        await _inventoryDocumentRepository.UpdateAsync(document, cancellationToken);
        await _inventoryDocumentRepository.SaveChangesAsync(cancellationToken);

        var newDto = await LoadDtoAsync(document.Id, cancellationToken);
        await _transactionAuditService.LogAsync(
            nameof(InventoryDocument), document.Id, AuditActionType.Cancel, oldDto, newDto, cancellationToken);

        return newDto;
    }

    /// <summary>Tạo entity phiếu kèm dòng hàng và sinh số phiếu tự động.</summary>
    private async Task<InventoryDocument> CreateDocumentAsync(
        InventoryDocumentType documentType,
        Guid? sourceWarehouseId,
        Guid? destinationWarehouseId,
        DateTime documentDate,
        string? referenceNo,
        string? sourceSystem,
        string? note,
        List<CreateInventoryDocumentLineDto> lines,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var documentNo = await GenerateDocumentNoAsync(documentType, now, cancellationToken);
        var documentId = Guid.NewGuid();

        var document = new InventoryDocument
        {
            Id = documentId,
            DocumentNo = documentNo,
            DocumentType = documentType,
            SourceWarehouseId = sourceWarehouseId,
            DestinationWarehouseId = destinationWarehouseId,
            Status = InventoryDocumentStatus.Draft,
            DocumentDate = documentDate,
            ReferenceNo = referenceNo?.Trim(),
            SourceSystem = sourceSystem?.Trim(),
            Note = note?.Trim(),
            CreatedBy = _auditContext.UserName,
            CreatedAt = now,
            Lines = lines.Select(line => new InventoryDocumentLine
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                ProductVariantId = line.ProductVariantId,
                Quantity = line.Quantity,
                UnitCost = line.UnitCost,
                Note = line.Note?.Trim()
            }).ToList()
        };

        return document;
    }

    /// <summary>Sinh mã phiếu theo loại và ngày, ví dụ SI-20250622-0001.</summary>
    private async Task<string> GenerateDocumentNoAsync(
        InventoryDocumentType documentType,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var typePrefix = documentType switch
        {
            InventoryDocumentType.StockIn => "SI",
            InventoryDocumentType.StockOut => "SO",
            InventoryDocumentType.Transfer => "TR",
            InventoryDocumentType.Adjustment => "AD",
            InventoryDocumentType.StockCount => "SC",
            _ => "DOC"
        };

        var prefix = $"{typePrefix}-{now:yyyyMMdd}-";
        var count = await _inventoryDocumentRepository.CountByTypeAndDatePrefixAsync(
            documentType, prefix, cancellationToken);

        return $"{prefix}{(count + 1).ToString().PadLeft(4, '0')}";
    }

    /// <summary>Kiểm tra phiếu điều chỉnh có ít nhất một dòng và số lượng khác 0.</summary>
    private static void ValidateAdjustmentLines(List<CreateAdjustmentLineDto> lines)
    {
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Document must contain at least one line.");
        }

        foreach (var line in lines)
        {
            if (line.AdjustmentQuantity == 0)
            {
                throw new InvalidOperationException("Adjustment quantity cannot be zero.");
            }
        }
    }

    private async Task EnsureAdjustmentVariantsExistAsync(
        List<CreateAdjustmentLineDto> lines,
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

    private static string BuildAdjustmentNote(string reason, string? note)
    {
        var trimmedReason = reason.Trim();
        return string.IsNullOrWhiteSpace(note)
            ? trimmedReason
            : $"{trimmedReason} | {note.Trim()}";
    }

    /// <summary>Kiểm tra dòng kiểm kê: ít nhất một dòng, số kiểm >= 0.</summary>
    private static void ValidateStockCountLines(List<CreateStockCountLineDto> lines)
    {
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Document must contain at least one line.");
        }

        foreach (var line in lines)
        {
            if (line.CountedQuantity < 0)
            {
                throw new InvalidOperationException("Counted quantity cannot be negative.");
            }
        }
    }

    private async Task EnsureStockCountVariantsExistAsync(
        List<CreateStockCountLineDto> lines,
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

    /// <summary>Kiểm tra dòng khi cập nhật Draft theo loại phiếu.</summary>
    private static void ValidateDraftLines(InventoryDocumentType documentType, List<CreateInventoryDocumentLineDto> lines)
    {
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Document must contain at least one line.");
        }

        switch (documentType)
        {
            case InventoryDocumentType.Adjustment:
                foreach (var line in lines)
                {
                    if (line.Quantity == 0)
                    {
                        throw new InvalidOperationException("Adjustment quantity cannot be zero.");
                    }
                }

                break;
            case InventoryDocumentType.StockCount:
                foreach (var line in lines)
                {
                    if (line.Quantity < 0)
                    {
                        throw new InvalidOperationException("Counted quantity cannot be negative.");
                    }
                }

                break;
            default:
                foreach (var line in lines)
                {
                    if (line.Quantity <= 0)
                    {
                        throw new InvalidOperationException("Line quantity must be greater than zero.");
                    }
                }

                break;
        }
    }

    /// <summary>Kiểm tra phiếu có ít nhất một dòng và số lượng > 0.</summary>
    private void ValidateLines(List<CreateInventoryDocumentLineDto> lines)
    {
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Document must contain at least one line.");
        }

        foreach (var line in lines)
        {
            if (line.Quantity <= 0)
            {
                throw new InvalidOperationException("Line quantity must be greater than zero.");
            }
        }
    }

    /// <summary>Đảm bảo tất cả SKU trong phiếu tồn tại trong hệ thống.</summary>
    private async Task EnsureProductVariantsExistAsync(
        List<CreateInventoryDocumentLineDto> lines,
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

    /// <summary>Đảm bảo kho nguồn/đích tồn tại.</summary>
    private async Task EnsureWarehouseExistsAsync(Guid warehouseId, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId, cancellationToken);
        if (warehouse is null)
        {
            throw new InvalidOperationException($"Warehouse '{warehouseId}' was not found.");
        }
    }

    /// <summary>Tải lại phiếu từ DB sau khi lưu để trả DTO đầy đủ.</summary>
    private async Task<InventoryDocumentDto> LoadDtoAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await _inventoryDocumentRepository.GetByIdWithLinesAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Inventory document '{id}' was not found.");

        return MapToDto(document);
    }

    /// <summary>Chuyển entity sang DTO kèm danh sách dòng hàng.</summary>
    private static InventoryDocumentDto MapToDto(InventoryDocument document) => new()
    {
        Id = document.Id,
        DocumentNo = document.DocumentNo,
        DocumentType = document.DocumentType,
        SourceWarehouseId = document.SourceWarehouseId,
        DestinationWarehouseId = document.DestinationWarehouseId,
        Status = document.Status,
        DocumentDate = document.DocumentDate,
        ReferenceNo = document.ReferenceNo,
        SourceSystem = document.SourceSystem,
        Note = document.Note,
        CreatedBy = document.CreatedBy,
        CreatedAt = document.CreatedAt,
        ApprovedBy = document.ApprovedBy,
        ApprovedAt = document.ApprovedAt,
        Lines = document.Lines.Select(line => new InventoryDocumentLineDto
        {
            Id = line.Id,
            ProductVariantId = line.ProductVariantId,
            Sku = line.ProductVariant?.Sku ?? string.Empty,
            Quantity = line.Quantity,
            UnitCost = line.UnitCost,
            Note = line.Note
        }).ToList()
    };

    /// <summary>Chuyển entity sang DTO không kèm dòng (dùng cho danh sách).</summary>
    private static InventoryDocumentDto MapToDtoWithoutLines(InventoryDocument document) => new()
    {
        Id = document.Id,
        DocumentNo = document.DocumentNo,
        DocumentType = document.DocumentType,
        SourceWarehouseId = document.SourceWarehouseId,
        DestinationWarehouseId = document.DestinationWarehouseId,
        Status = document.Status,
        DocumentDate = document.DocumentDate,
        ReferenceNo = document.ReferenceNo,
        SourceSystem = document.SourceSystem,
        Note = document.Note,
        CreatedBy = document.CreatedBy,
        CreatedAt = document.CreatedAt,
        ApprovedBy = document.ApprovedBy,
        ApprovedAt = document.ApprovedAt
    };
}
