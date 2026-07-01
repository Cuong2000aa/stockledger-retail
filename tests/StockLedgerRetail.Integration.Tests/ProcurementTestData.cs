using StockLedgerRetail.Application.Seed;

namespace StockLedgerRetail.Integration.Tests;

internal static class ProcurementTestData
{
    public static readonly Guid DominosSupplierId = Guid.Parse("a5000001-0001-4001-8001-000000000001");
    public static readonly Guid SauceVariantId = Guid.Parse("c1000001-0001-4001-8001-000000000002");
    public static readonly Guid DominosStoreId = FbDataSeedService.DominosStoreId;

    public const decimal SauceUnitCost = 95_000m;
}
