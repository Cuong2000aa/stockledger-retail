using Microsoft.Extensions.Logging;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Seed;

/// <summary>Seed Domino's &amp; Popeyes — brand, kho, SKU nguyên liệu có lô/HSD, tồn mẫu.</summary>
public class FbDataSeedService : IFbDataSeedService
{
    public static readonly Guid DominosBrandId = Guid.Parse("a1000001-0001-4001-8001-000000000001");
    public static readonly Guid PopeyesBrandId = Guid.Parse("a1000002-0002-4002-8002-000000000002");

    public static readonly Guid DominosDcId = Guid.Parse("a2000001-0001-4001-8001-000000000001");
    public static readonly Guid DominosStoreId = Guid.Parse("a2000001-0001-4001-8001-000000000002");
    public static readonly Guid PopeyesDcId = Guid.Parse("a2000002-0002-4002-8002-000000000001");
    public static readonly Guid PopeyesStoreId = Guid.Parse("a2000002-0002-4002-8002-000000000002");

    private readonly IBrandRepository _brandRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductVariantRepository _productVariantRepository;
    private readonly ITransferPolicyRepository _transferPolicyRepository;
    private readonly IStockLotRepository _stockLotRepository;
    private readonly ILotStockRepository _lotStockRepository;
    private readonly ICurrentStockRepository _currentStockRepository;
    private readonly ILogger<FbDataSeedService> _logger;

    public FbDataSeedService(
        IBrandRepository brandRepository,
        IWarehouseRepository warehouseRepository,
        ISupplierRepository supplierRepository,
        IProductRepository productRepository,
        IProductVariantRepository productVariantRepository,
        ITransferPolicyRepository transferPolicyRepository,
        IStockLotRepository stockLotRepository,
        ILotStockRepository lotStockRepository,
        ICurrentStockRepository currentStockRepository,
        ILogger<FbDataSeedService> logger)
    {
        _brandRepository = brandRepository;
        _warehouseRepository = warehouseRepository;
        _supplierRepository = supplierRepository;
        _productRepository = productRepository;
        _productVariantRepository = productVariantRepository;
        _transferPolicyRepository = transferPolicyRepository;
        _stockLotRepository = stockLotRepository;
        _lotStockRepository = lotStockRepository;
        _currentStockRepository = currentStockRepository;
        _logger = logger;
    }

    public async Task EnsureSeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _brandRepository.GetByCodeAsync("DOMINOS", cancellationToken) is not null)
        {
            _logger.LogDebug("F&B seed skipped — DOMINOS brand already exists.");
            return;
        }

        var now = DateTime.UtcNow;
        _logger.LogInformation("Seeding F&B demo data (Domino's, Popeyes)...");

        await SeedBrandsAsync(now, cancellationToken);
        await SeedWarehousesAsync(now, cancellationToken);
        await SeedSuppliersAsync(now, cancellationToken);
        await SeedTransferPolicyAsync(cancellationToken);
        await SeedProductsAndVariantsAsync(now, cancellationToken);
        await SeedLotsAndStockAsync(now, cancellationToken);

        await _currentStockRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("F&B seed completed.");
    }

    private async Task SeedBrandsAsync(DateTime now, CancellationToken cancellationToken)
    {
        await _brandRepository.InsertAsync(new Brand
        {
            Id = DominosBrandId,
            Code = "DOMINOS",
            Name = "Domino's Pizza",
            Status = BrandStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);

        await _brandRepository.InsertAsync(new Brand
        {
            Id = PopeyesBrandId,
            Code = "POPEYES",
            Name = "Popeyes",
            Status = BrandStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);

        await _brandRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedWarehousesAsync(DateTime now, CancellationToken cancellationToken)
    {
        var warehouses = new[]
        {
            new Warehouse
            {
                Id = DominosDcId,
                Code = "DOMINOS-DC-HCM",
                Name = "Domino's DC HCM",
                Type = WarehouseType.Dc,
                Status = WarehouseStatus.Active,
                BrandId = DominosBrandId,
                RegionCode = "HCM",
                FulfillmentPriority = 1,
                Province = "Ho Chi Minh",
                FullAddress = "Domino's DC, Thu Duc, HCM",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Warehouse
            {
                Id = DominosStoreId,
                Code = "DOMINOS-ST-Q1",
                Name = "Domino's Store Q1",
                Type = WarehouseType.Store,
                Status = WarehouseStatus.Active,
                BrandId = DominosBrandId,
                RegionCode = "HCM",
                FulfillmentPriority = 2,
                Province = "Ho Chi Minh",
                FullAddress = "Domino's Q1, HCM",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Warehouse
            {
                Id = PopeyesDcId,
                Code = "POPEYES-DC-HCM",
                Name = "Popeyes DC HCM",
                Type = WarehouseType.Dc,
                Status = WarehouseStatus.Active,
                BrandId = PopeyesBrandId,
                RegionCode = "HCM",
                FulfillmentPriority = 1,
                Province = "Ho Chi Minh",
                FullAddress = "Popeyes DC, Binh Tan, HCM",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Warehouse
            {
                Id = PopeyesStoreId,
                Code = "POPEYES-ST-DD",
                Name = "Popeyes Store Dien Bien Phu",
                Type = WarehouseType.Store,
                Status = WarehouseStatus.Active,
                BrandId = PopeyesBrandId,
                RegionCode = "HCM",
                FulfillmentPriority = 2,
                Province = "Ho Chi Minh",
                FullAddress = "Popeyes Dien Bien Phu, HCM",
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        foreach (var warehouse in warehouses)
        {
            await _warehouseRepository.InsertAsync(warehouse, cancellationToken);
        }

        await _warehouseRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSuppliersAsync(DateTime now, CancellationToken cancellationToken)
    {
        await _supplierRepository.InsertAsync(new Supplier
        {
            Id = Guid.Parse("a5000001-0001-4001-8001-000000000001"),
            Code = "FNBSUP-DOM",
            Name = "Domino's Food Supply VN",
            ContactName = "Nguyen Van A",
            Phone = "0281234567",
            Email = "supply@dominos-vn.local",
            Address = "HCM",
            Status = SupplierStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);

        await _supplierRepository.InsertAsync(new Supplier
        {
            Id = Guid.Parse("a5000002-0002-4002-8002-000000000002"),
            Code = "FNBSUP-POP",
            Name = "Popeyes Ingredients VN",
            ContactName = "Tran Thi B",
            Phone = "0287654321",
            Email = "supply@popeyes-vn.local",
            Address = "HCM",
            Status = SupplierStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);

        await _supplierRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedTransferPolicyAsync(CancellationToken cancellationToken)
    {
        await _transferPolicyRepository.InsertAsync(new TransferPolicy
        {
            Id = Guid.Parse("a9000001-0001-4001-8001-000000000001"),
            SourceBrandId = DominosBrandId,
            DestinationBrandId = PopeyesBrandId,
            AllowCrossBrand = true,
            IsActive = true,
            Note = "F&B shared logistics — emergency cross-brand transfer"
        }, cancellationToken);

        await _transferPolicyRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedProductsAndVariantsAsync(DateTime now, CancellationToken cancellationToken)
    {
        var catalog = new (Guid ProductId, Guid VariantId, Guid BrandId, string ProductCode, string ProductName, string Sku, string Unit, decimal Cost, decimal Price)[]
        {
            (Guid.Parse("b1000001-0001-4001-8001-000000000001"), Guid.Parse("c1000001-0001-4001-8001-000000000001"), DominosBrandId,
                "FNB-DOM-CHEESE", "Mozzarella Cheese Block", "DOM-CHEESE-1KG", "kg", 180_000m, 0m),
            (Guid.Parse("b1000001-0001-4001-8001-000000000002"), Guid.Parse("c1000001-0001-4001-8001-000000000002"), DominosBrandId,
                "FNB-DOM-SAUCE", "Pizza Tomato Sauce", "DOM-SAUCE-3KG", "tub", 95_000m, 0m),
            (Guid.Parse("b1000001-0001-4001-8001-000000000003"), Guid.Parse("c1000001-0001-4001-8001-000000000003"), DominosBrandId,
                "FNB-DOM-DOUGH", "Dough Ball 12 inch", "DOM-DOUGH-12", "pcs", 8_500m, 0m),
            (Guid.Parse("b1000002-0002-4002-8002-000000000001"), Guid.Parse("c1000002-0002-4002-8002-000000000001"), PopeyesBrandId,
                "FNB-POP-CHICK", "Spicy Chicken Fillet", "POP-CHICK-FIL", "pcs", 22_000m, 0m),
            (Guid.Parse("b1000002-0002-4002-8002-000000000002"), Guid.Parse("c1000002-0002-4002-8002-000000000002"), PopeyesBrandId,
                "FNB-POP-BREAD", "Breading Mix", "POP-BREAD-5KG", "bag", 120_000m, 0m),
            (Guid.Parse("b1000002-0002-4002-8002-000000000003"), Guid.Parse("c1000002-0002-4002-8002-000000000003"), PopeyesBrandId,
                "FNB-POP-OIL", "Fryer Oil", "POP-OIL-20L", "can", 450_000m, 0m),
        };

        foreach (var item in catalog)
        {
            await _productRepository.InsertAsync(new Product
            {
                Id = item.ProductId,
                ProductCode = item.ProductCode,
                Name = item.ProductName,
                Brand = item.BrandId == DominosBrandId ? "Domino's" : "Popeyes",
                BrandId = item.BrandId,
                Category = "F&B Ingredients",
                Status = ProductStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            }, cancellationToken);

            await _productVariantRepository.InsertAsync(new ProductVariant
            {
                Id = item.VariantId,
                ProductId = item.ProductId,
                BrandId = item.BrandId,
                Sku = item.Sku,
                Unit = item.Unit,
                Status = ProductStatus.Active,
                CostPrice = item.Cost,
                SellingPrice = item.Price > 0 ? item.Price : null,
                CostSource = CostSource.Manual,
                TrackLotExpiry = true,
                CreatedAt = now,
                UpdatedAt = now
            }, cancellationToken);
        }

        await _productVariantRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedLotsAndStockAsync(DateTime now, CancellationToken cancellationToken)
    {
        var cheeseVariantId = Guid.Parse("c1000001-0001-4001-8001-000000000001");
        var chickenVariantId = Guid.Parse("c1000002-0002-4002-8002-000000000001");

        var cheeseLotNearId = Guid.Parse("d1000001-0001-4001-8001-000000000001");
        var cheeseLotOkId = Guid.Parse("d1000001-0001-4001-8001-000000000002");
        var chickenLotNearId = Guid.Parse("d1000002-0002-4002-8002-000000000001");

        await _stockLotRepository.InsertAsync(new StockLot
        {
            Id = cheeseLotNearId,
            ProductVariantId = cheeseVariantId,
            LotCode = "DOM-CHZ-20260601",
            ExpiryDate = now.Date.AddDays(5),
            ReceivedAt = now.AddDays(-10)
        }, cancellationToken);

        await _stockLotRepository.InsertAsync(new StockLot
        {
            Id = cheeseLotOkId,
            ProductVariantId = cheeseVariantId,
            LotCode = "DOM-CHZ-20260615",
            ExpiryDate = now.Date.AddDays(25),
            ReceivedAt = now.AddDays(-3)
        }, cancellationToken);

        await _stockLotRepository.InsertAsync(new StockLot
        {
            Id = chickenLotNearId,
            ProductVariantId = chickenVariantId,
            LotCode = "POP-CHK-20260605",
            ExpiryDate = now.Date.AddDays(3),
            ReceivedAt = now.AddDays(-2)
        }, cancellationToken);

        await _stockLotRepository.SaveChangesAsync(cancellationToken);

        await _lotStockRepository.InsertAsync(new LotStock
        {
            Id = Guid.Parse("e1000001-0001-4001-8001-000000000001"),
            StockLotId = cheeseLotNearId,
            WarehouseId = DominosDcId,
            QuantityOnHand = 40,
            LastUpdatedAt = now
        }, cancellationToken);

        await _lotStockRepository.InsertAsync(new LotStock
        {
            Id = Guid.Parse("e1000001-0001-4001-8001-000000000002"),
            StockLotId = cheeseLotOkId,
            WarehouseId = DominosDcId,
            QuantityOnHand = 60,
            LastUpdatedAt = now
        }, cancellationToken);

        await _lotStockRepository.InsertAsync(new LotStock
        {
            Id = Guid.Parse("e1000002-0002-4002-8002-000000000001"),
            StockLotId = chickenLotNearId,
            WarehouseId = PopeyesDcId,
            QuantityOnHand = 120,
            LastUpdatedAt = now
        }, cancellationToken);

        await _lotStockRepository.SaveChangesAsync(cancellationToken);

        await _currentStockRepository.InsertAsync(new CurrentStock
        {
            Id = Guid.Parse("f1000001-0001-4001-8001-000000000001"),
            ProductVariantId = cheeseVariantId,
            WarehouseId = DominosDcId,
            QuantityOnHand = 100,
            QuantityReserved = 0,
            QuantityAvailable = 100,
            LastUpdatedAt = now
        }, cancellationToken);

        await _currentStockRepository.InsertAsync(new CurrentStock
        {
            Id = Guid.Parse("f1000002-0002-4002-8002-000000000001"),
            ProductVariantId = chickenVariantId,
            WarehouseId = PopeyesDcId,
            QuantityOnHand = 120,
            QuantityReserved = 0,
            QuantityAvailable = 120,
            LastUpdatedAt = now
        }, cancellationToken);

        // Sauce & dough at store — with matching lot stock
        var sauceId = Guid.Parse("c1000001-0001-4001-8001-000000000002");
        var doughId = Guid.Parse("c1000001-0001-4001-8001-000000000003");
        var sauceLotId = Guid.Parse("d1000001-0001-4001-8001-000000000003");
        var doughLotId = Guid.Parse("d1000001-0001-4001-8001-000000000004");

        await _stockLotRepository.InsertAsync(new StockLot
        {
            Id = sauceLotId,
            ProductVariantId = sauceId,
            LotCode = "DOM-SAUCE-20260610",
            ExpiryDate = now.Date.AddDays(20),
            ReceivedAt = now.AddDays(-5)
        }, cancellationToken);

        await _stockLotRepository.InsertAsync(new StockLot
        {
            Id = doughLotId,
            ProductVariantId = doughId,
            LotCode = "DOM-DOUGH-20260612",
            ExpiryDate = now.Date.AddDays(14),
            ReceivedAt = now.AddDays(-1)
        }, cancellationToken);

        await _stockLotRepository.SaveChangesAsync(cancellationToken);

        await _lotStockRepository.InsertAsync(new LotStock
        {
            Id = Guid.Parse("e1000001-0001-4001-8001-000000000003"),
            StockLotId = sauceLotId,
            WarehouseId = DominosStoreId,
            QuantityOnHand = 30,
            LastUpdatedAt = now
        }, cancellationToken);

        await _lotStockRepository.InsertAsync(new LotStock
        {
            Id = Guid.Parse("e1000001-0001-4001-8001-000000000004"),
            StockLotId = doughLotId,
            WarehouseId = DominosStoreId,
            QuantityOnHand = 200,
            LastUpdatedAt = now
        }, cancellationToken);

        await _lotStockRepository.SaveChangesAsync(cancellationToken);

        await _currentStockRepository.InsertAsync(new CurrentStock
        {
            Id = Guid.Parse("f1000001-0001-4001-8001-000000000002"),
            ProductVariantId = sauceId,
            WarehouseId = DominosStoreId,
            QuantityOnHand = 30,
            QuantityReserved = 0,
            QuantityAvailable = 30,
            LastUpdatedAt = now
        }, cancellationToken);

        await _currentStockRepository.InsertAsync(new CurrentStock
        {
            Id = Guid.Parse("f1000001-0001-4001-8001-000000000003"),
            ProductVariantId = doughId,
            WarehouseId = DominosStoreId,
            QuantityOnHand = 200,
            QuantityReserved = 0,
            QuantityAvailable = 200,
            LastUpdatedAt = now
        }, cancellationToken);
    }
}
