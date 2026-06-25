using StockLedgerRetail.Audit;
using StockLedgerRetail.Application.Inventory;
using StockLedgerRetail.Common;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.ProductVariants;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.ProductVariants;

/// <summary>
/// Dịch vụ nghiệp vụ quản lý SKU (ProductVariant) — đơn vị tồn kho thực tế.
/// </summary>
public class ProductVariantAppService : IProductVariantAppService
{
    private readonly IProductVariantRepository _productVariantRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductCostHistoryRepository _productCostHistoryRepository;
    private readonly IProductPriceRepository _productPriceRepository;
    private readonly IAuditContext _auditContext;
    private readonly ITransactionAuditService _transactionAuditService;

    public ProductVariantAppService(
        IProductVariantRepository productVariantRepository,
        IProductRepository productRepository,
        IProductCostHistoryRepository productCostHistoryRepository,
        IProductPriceRepository productPriceRepository,
        IAuditContext auditContext,
        ITransactionAuditService transactionAuditService)
    {
        _productVariantRepository = productVariantRepository;
        _productRepository = productRepository;
        _productCostHistoryRepository = productCostHistoryRepository;
        _productPriceRepository = productPriceRepository;
        _auditContext = auditContext;
        _transactionAuditService = transactionAuditService;
    }

    /// <summary>Lấy toàn bộ danh sách SKU.</summary>
    public async Task<PagedResultDto<ProductVariantDto>> GetListAsync(
        int? page = null,
        int? pageSize = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var (skip, take, normalizedPage, normalizedPageSize) = PagingNormalizer.Normalize(page, pageSize);
        var (variants, totalCount) = await _productVariantRepository.GetPagedListAsync(skip, take, search, cancellationToken);
        var items = variants.Select(MapToDto).ToList();
        return PagingNormalizer.Create(items, totalCount, normalizedPage, normalizedPageSize);
    }

    /// <summary>Lấy các SKU thuộc một sản phẩm cha.</summary>
    public async Task<List<ProductVariantDto>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var variants = await _productVariantRepository.GetByProductIdAsync(productId, cancellationToken);
        return variants.Select(MapToDto).ToList();
    }

    /// <summary>Lấy chi tiết SKU theo Id.</summary>
    public async Task<ProductVariantDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var variant = await _productVariantRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product variant '{id}' was not found.");

        return MapToDto(variant);
    }

    /// <summary>Tạo SKU mới, kiểm tra product cha tồn tại và SKU không trùng.</summary>
    public async Task<ProductVariantDto> CreateAsync(CreateProductVariantDto input, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(input.ProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"Product '{input.ProductId}' was not found.");

        var brandId = input.BrandId ?? product.BrandId;
        var existingSku = await _productVariantRepository.GetByBrandIdAndSkuAsync(
            brandId,
            input.Sku,
            cancellationToken);
        if (existingSku is not null)
        {
            throw new InvalidOperationException($"SKU '{input.Sku}' already exists for this brand scope.");
        }

        ValidateValuation(input.CostPrice, input.SellingPrice, input.SellingPriceBeforeVat, input.SellingPriceAfterVat, input.VatRate);

        var now = DateTime.UtcNow;
        var (costPrice, costSource) = NormalizeCost(input.CostPrice, input.CostSource);
        var priceInput = NormalizeSellingPrice(input.SellingPrice, input.SellingPriceBeforeVat, input.SellingPriceAfterVat, input.VatRate);
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            BrandId = brandId,
            Sku = input.Sku.Trim(),
            Barcode = input.Barcode?.Trim(),
            Color = input.Color?.Trim(),
            Size = input.Size?.Trim(),
            Season = input.Season?.Trim(),
            Unit = input.Unit?.Trim(),
            Status = input.Status,
            CostPrice = costPrice,
            CurrentCostPrice = costPrice,
            CurrentCostSource = costSource,
            CurrentCostEffectiveFrom = costPrice.HasValue ? now : null,
            SellingPrice = priceInput.CurrentSellingPrice,
            CurrentSellingPrice = priceInput.CurrentSellingPrice,
            CurrentSellingPriceBeforeVat = priceInput.PriceBeforeVat,
            CurrentSellingPriceAfterVat = priceInput.PriceAfterVat,
            VatRate = priceInput.VatRate,
            CurrentPriceEffectiveFrom = priceInput.CurrentSellingPrice.HasValue ? now : null,
            CostSource = costSource,
            TrackLotExpiry = input.TrackLotExpiry,
            IsBarcode = input.IsBarcode,
            CreatedAt = now,
            UpdatedAt = now
        };
        AuditUserStamp.OnCreate(variant, _auditContext, now);

        await _productVariantRepository.InsertAsync(variant, cancellationToken);
        await UpsertManualCostHistoryAsync(variant, now, cancellationToken);
        await UpsertSellingPriceHistoryAsync(variant, now, cancellationToken);
        await _productVariantRepository.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(variant);
        await _transactionAuditService.LogAsync(nameof(ProductVariant), variant.Id, AuditActionType.Create, null, dto, cancellationToken);

        return dto;
    }

    /// <summary>Cập nhật thuộc tính SKU (không đổi mã SKU).</summary>
    public async Task<ProductVariantDto> UpdateAsync(Guid id, UpdateProductVariantDto input, CancellationToken cancellationToken = default)
    {
        var variant = await _productVariantRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product variant '{id}' was not found.");

        var oldDto = MapToDto(variant);

        ValidateValuation(input.CostPrice, input.SellingPrice, input.SellingPriceBeforeVat, input.SellingPriceAfterVat, input.VatRate);

        variant.Barcode = input.Barcode?.Trim();
        variant.Color = input.Color?.Trim();
        variant.Size = input.Size?.Trim();
        variant.Season = input.Season?.Trim();
        variant.Unit = input.Unit?.Trim();
        variant.Status = input.Status;
        var (costPrice, costSource) = NormalizeCost(input.CostPrice, input.CostSource);
        var priceInput = NormalizeSellingPrice(input.SellingPrice, input.SellingPriceBeforeVat, input.SellingPriceAfterVat, input.VatRate);
        variant.CostPrice = costPrice;
        variant.CurrentCostPrice = costPrice;
        variant.CurrentCostSource = costSource;
        variant.CurrentCostEffectiveFrom = costPrice.HasValue ? DateTime.UtcNow : null;
        variant.SellingPrice = priceInput.CurrentSellingPrice;
        variant.CurrentSellingPrice = priceInput.CurrentSellingPrice;
        variant.CurrentSellingPriceBeforeVat = priceInput.PriceBeforeVat;
        variant.CurrentSellingPriceAfterVat = priceInput.PriceAfterVat;
        variant.VatRate = priceInput.VatRate;
        variant.CurrentPriceEffectiveFrom = priceInput.CurrentSellingPrice.HasValue ? DateTime.UtcNow : null;
        variant.CostSource = costSource;
        variant.TrackLotExpiry = input.TrackLotExpiry;
        variant.IsBarcode = input.IsBarcode;
        AuditUserStamp.OnUpdate(variant, _auditContext, DateTime.UtcNow);

        await UpsertManualCostHistoryAsync(variant, variant.UpdatedAt, cancellationToken);
        await UpsertSellingPriceHistoryAsync(variant, variant.UpdatedAt, cancellationToken);

        await _productVariantRepository.UpdateAsync(variant, cancellationToken);
        await _productVariantRepository.SaveChangesAsync(cancellationToken);

        var newDto = MapToDto(variant);
        await _transactionAuditService.LogAsync(nameof(ProductVariant), variant.Id, AuditActionType.Update, oldDto, newDto, cancellationToken);

        return newDto;
    }

    public async Task<List<ProductPriceDto>> GetPricesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _ = await _productVariantRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product variant '{id}' was not found.");

        var prices = await _productPriceRepository.GetByVariantAsync(id, cancellationToken);
        return prices.Select(MapPriceToDto).ToList();
    }

    public async Task<ProductPriceDto> UpsertPriceAsync(Guid id, UpsertProductPriceDto input, CancellationToken cancellationToken = default)
    {
        var variant = await _productVariantRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product variant '{id}' was not found.");

        if (input.PriceBeforeVat < 0)
        {
            throw new InvalidOperationException("Price before VAT cannot be negative.");
        }

        if (input.VatRate is < 0 or > 100)
        {
            throw new InvalidOperationException("VAT rate must be between 0 and 100.");
        }

        var now = DateTime.UtcNow;
        var priceAfterVat = input.PriceAfterVat ?? RoundCurrency(input.PriceBeforeVat * (1 + input.VatRate / 100m));
        var currentPrice = await _productPriceRepository.GetCurrentByVariantAndTypeAsync(id, input.PriceType, cancellationToken);
        if (currentPrice is not null)
        {
            currentPrice.EffectiveTo = input.EffectiveFrom;
            currentPrice.IsCurrent = false;
            AuditUserStamp.OnUpdate(currentPrice, _auditContext, now);
            await _productPriceRepository.UpdateAsync(currentPrice, cancellationToken);
        }

        var price = new ProductPrice
        {
            Id = Guid.NewGuid(),
            ProductVariantId = id,
            PriceType = input.PriceType,
            PriceBeforeVat = input.PriceBeforeVat,
            PriceAfterVat = priceAfterVat,
            VatRate = input.VatRate,
            Currency = string.IsNullOrWhiteSpace(input.Currency) ? "VND" : input.Currency.Trim().ToUpperInvariant(),
            EffectiveFrom = input.EffectiveFrom,
            EffectiveTo = input.EffectiveTo,
            IsCurrent = !input.EffectiveTo.HasValue || input.EffectiveTo.Value >= input.EffectiveFrom,
            ChannelCode = input.ChannelCode?.Trim(),
            ReferenceType = input.ReferenceType?.Trim(),
            ReferenceId = variant.Id
        };
        AuditUserStamp.OnCreate(price, _auditContext, now);
        await _productPriceRepository.InsertAsync(price, cancellationToken);

        if (price.IsCurrent && price.PriceType == PriceType.Regular)
        {
            variant.SellingPrice = price.PriceAfterVat;
            variant.CurrentSellingPrice = price.PriceAfterVat;
            variant.CurrentSellingPriceBeforeVat = price.PriceBeforeVat;
            variant.CurrentSellingPriceAfterVat = price.PriceAfterVat;
            variant.VatRate = price.VatRate;
            variant.CurrentPriceEffectiveFrom = price.EffectiveFrom;
            AuditUserStamp.OnUpdate(variant, _auditContext, now);
            await _productVariantRepository.UpdateAsync(variant, cancellationToken);
        }

        await _productVariantRepository.SaveChangesAsync(cancellationToken);
        return MapPriceToDto(price);
    }

    /// <summary>Xóa SKU.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var variant = await _productVariantRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product variant '{id}' was not found.");

        var oldDto = MapToDto(variant);

        await _productVariantRepository.DeleteAsync(variant, cancellationToken);
        await _productVariantRepository.SaveChangesAsync(cancellationToken);

        await _transactionAuditService.LogAsync(nameof(ProductVariant), variant.Id, AuditActionType.Delete, oldDto, null, cancellationToken);
    }

    /// <summary>Chuyển entity ProductVariant sang DTO.</summary>
    private static ProductVariantDto MapToDto(ProductVariant variant) => new()
    {
        Id = variant.Id,
        ProductId = variant.ProductId,
        BrandId = variant.BrandId,
        Sku = variant.Sku,
        Barcode = variant.Barcode,
        Color = variant.Color,
        Size = variant.Size,
        Season = variant.Season,
        Unit = variant.Unit,
        Status = variant.Status,
        CostPrice = variant.CostPrice,
        SellingPrice = variant.SellingPrice,
        CurrentCostPrice = variant.CurrentCostPrice,
        CurrentSellingPrice = variant.CurrentSellingPrice,
        SellingPriceBeforeVat = variant.CurrentSellingPriceBeforeVat,
        SellingPriceAfterVat = variant.CurrentSellingPriceAfterVat,
        VatRate = variant.VatRate,
        CostSource = variant.CostSource,
        CurrentCostSource = variant.CurrentCostSource,
        TrackLotExpiry = variant.TrackLotExpiry,
        IsBarcode = variant.IsBarcode,
        CreatedAt = variant.CreatedAt,
        UpdatedAt = variant.UpdatedAt
    };

    private static void ValidateValuation(
        decimal? costPrice,
        decimal? sellingPrice,
        decimal? sellingPriceBeforeVat,
        decimal? sellingPriceAfterVat,
        decimal? vatRate)
    {
        if (costPrice is < 0)
        {
            throw new InvalidOperationException("Cost price cannot be negative.");
        }

        if (sellingPrice is < 0 || sellingPriceBeforeVat is < 0 || sellingPriceAfterVat is < 0)
        {
            throw new InvalidOperationException("Selling price cannot be negative.");
        }

        if (vatRate is < 0 or > 100)
        {
            throw new InvalidOperationException("VAT rate must be between 0 and 100.");
        }
    }

    /// <summary>Giá vốn null thì xóa nguồn; có giá mà không chỉ nguồn thì mặc định Manual.</summary>
    private static (decimal? CostPrice, CostSource? CostSource) NormalizeCost(
        decimal? costPrice,
        CostSource? costSource)
    {
        if (costPrice is null)
        {
            return (null, null);
        }

        return (costPrice, costSource ?? CostSource.Manual);
    }

    private async Task UpsertManualCostHistoryAsync(
        ProductVariant variant,
        DateTime effectiveAt,
        CancellationToken cancellationToken)
    {
        if (!variant.CurrentCostPrice.HasValue || !variant.CurrentCostSource.HasValue)
        {
            return;
        }

        var activeHistory = await _productCostHistoryRepository.GetActiveByVariantAsync(variant.Id, cancellationToken);
        if (activeHistory is not null
            && activeHistory.CostPrice == variant.CurrentCostPrice.Value
            && activeHistory.CostSource == variant.CurrentCostSource.Value
            && activeHistory.ValuationMethod == ValuationMethod.Manual)
        {
            return;
        }

        if (activeHistory is not null)
        {
            activeHistory.EffectiveTo = effectiveAt;
            activeHistory.IsCurrent = false;
            await _productCostHistoryRepository.UpdateAsync(activeHistory, cancellationToken);
        }

        await _productCostHistoryRepository.InsertAsync(new ProductCostHistory
        {
            Id = Guid.NewGuid(),
            ProductVariantId = variant.Id,
            CostPrice = variant.CurrentCostPrice.Value,
            CostSource = variant.CurrentCostSource.Value,
            ValuationMethod = ValuationMethod.Manual,
            Currency = "VND",
            ReferenceType = "ProductVariant",
            ReferenceId = variant.Id,
            EffectiveFrom = effectiveAt,
            IsCurrent = true
        }, cancellationToken);
    }

    private async Task UpsertSellingPriceHistoryAsync(
        ProductVariant variant,
        DateTime effectiveAt,
        CancellationToken cancellationToken)
    {
        if (!variant.CurrentSellingPrice.HasValue
            || !variant.CurrentSellingPriceBeforeVat.HasValue
            || !variant.CurrentSellingPriceAfterVat.HasValue
            || !variant.VatRate.HasValue)
        {
            return;
        }

        var currentPrice = await _productPriceRepository.GetCurrentByVariantAsync(variant.Id, cancellationToken);
        if (currentPrice is not null
            && currentPrice.PriceBeforeVat == variant.CurrentSellingPriceBeforeVat.Value
            && currentPrice.PriceAfterVat == variant.CurrentSellingPriceAfterVat.Value
            && currentPrice.VatRate == variant.VatRate.Value
            && currentPrice.PriceType == PriceType.Regular)
        {
            return;
        }

        if (currentPrice is not null)
        {
            currentPrice.EffectiveTo = effectiveAt;
            currentPrice.IsCurrent = false;
            AuditUserStamp.OnUpdate(currentPrice, _auditContext, effectiveAt);
            await _productPriceRepository.UpdateAsync(currentPrice, cancellationToken);
        }

        var newPrice = new ProductPrice
        {
            Id = Guid.NewGuid(),
            ProductVariantId = variant.Id,
            PriceType = PriceType.Regular,
            PriceBeforeVat = variant.CurrentSellingPriceBeforeVat.Value,
            PriceAfterVat = variant.CurrentSellingPriceAfterVat.Value,
            VatRate = variant.VatRate.Value,
            Currency = "VND",
            EffectiveFrom = effectiveAt,
            IsCurrent = true,
            ReferenceType = "ProductVariant",
            ReferenceId = variant.Id
        };
        AuditUserStamp.OnCreate(newPrice, _auditContext, effectiveAt);
        await _productPriceRepository.InsertAsync(newPrice, cancellationToken);
    }

    private static PricingInput NormalizeSellingPrice(
        decimal? sellingPrice,
        decimal? sellingPriceBeforeVat,
        decimal? sellingPriceAfterVat,
        decimal? vatRate)
    {
        var normalizedVatRate = vatRate ?? 0m;
        var legacyOrAfterVat = sellingPriceAfterVat ?? sellingPrice;

        if (sellingPriceBeforeVat.HasValue && !legacyOrAfterVat.HasValue)
        {
            legacyOrAfterVat = RoundCurrency(sellingPriceBeforeVat.Value * (1 + normalizedVatRate / 100m));
        }
        else if (!sellingPriceBeforeVat.HasValue && legacyOrAfterVat.HasValue)
        {
            var divisor = 1 + normalizedVatRate / 100m;
            sellingPriceBeforeVat = divisor == 0
                ? legacyOrAfterVat.Value
                : RoundCurrency(legacyOrAfterVat.Value / divisor);
        }

        return new PricingInput(
            sellingPriceBeforeVat,
            legacyOrAfterVat,
            legacyOrAfterVat,
            vatRate);
    }

    private static decimal RoundCurrency(decimal value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);

    private sealed record PricingInput(
        decimal? PriceBeforeVat,
        decimal? PriceAfterVat,
        decimal? CurrentSellingPrice,
        decimal? VatRate);

    private static ProductPriceDto MapPriceToDto(ProductPrice price) => new()
    {
        Id = price.Id,
        ProductVariantId = price.ProductVariantId,
        PriceType = price.PriceType,
        PriceBeforeVat = price.PriceBeforeVat,
        VatRate = price.VatRate,
        PriceAfterVat = price.PriceAfterVat,
        Currency = price.Currency,
        EffectiveFrom = price.EffectiveFrom,
        EffectiveTo = price.EffectiveTo,
        IsCurrent = price.IsCurrent,
        ChannelCode = price.ChannelCode,
        ReferenceType = price.ReferenceType
    };
}
