using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StockLedgerRetail.Application.Integration;
using StockLedgerRetail.Integration;
using Xunit;

namespace StockLedgerRetail.Application.Tests;

public class OmniChannelIntegrationTests
{
    [Fact]
    public void SalesIntegrationOptions_DefaultSafetyStockBuffer_DefaultsToZero()
    {
        var options = new SalesIntegrationOptions();
        Assert.Equal(0, options.DefaultSafetyStockBuffer);
    }

    [Fact]
    public async Task StockWebhookService_WithNoEndpoints_ReturnsDispatchedZero()
    {
        var options = Options.Create(new StockWebhookOptions
        {
            Enabled = true,
            Endpoints = []
        });

        var service = new StockWebhookService(options, NullLogger<StockWebhookService>.Instance);
        var result = await service.TestWebhookDispatchAsync();

        Assert.False(result.Success);
        Assert.Equal(0, result.DispatchedCount);
    }

    [Fact]
    public void StockDeltaDto_Properties_CanBeInstantiated()
    {
        var now = DateTime.UtcNow;
        var dto = new StockDeltaResponseDto
        {
            SinceUtc = now.AddHours(-1),
            GeneratedAtUtc = now,
            TotalChanges = 1,
            HasMore = false,
            Items =
            [
                new StockDeltaItemDto
                {
                    WarehouseId = Guid.NewGuid(),
                    WarehouseCode = "STORE-01",
                    WarehouseName = "Store 01",
                    ProductVariantId = Guid.NewGuid(),
                    Sku = "SKU-001",
                    ProductName = "T-Shirt",
                    OnHandQuantity = 10,
                    ReservedQuantity = 2,
                    AvailableQuantity = 8,
                    LastMovementAtUtc = now
                }
            ]
        };

        Assert.Single(dto.Items);
        Assert.Equal(8, dto.Items[0].AvailableQuantity);
        Assert.Equal(10, dto.Items[0].OnHandQuantity);
    }

    [Fact]
    public void BatchConfirmSaleDto_Properties_WorkCorrectly()
    {
        var batch = new BatchConfirmSaleResponseDto
        {
            TotalCount = 2,
            SuccessCount = 1,
            FailedCount = 1,
            Results =
            [
                new BatchConfirmSaleItemResultDto
                {
                    OrderReference = "ORD-001",
                    Success = true,
                    Data = new ConfirmSaleResponseDto { DocumentNo = "OUT-001" }
                },
                new BatchConfirmSaleItemResultDto
                {
                    OrderReference = "ORD-002",
                    Success = false,
                    ErrorMessage = "Insufficient stock"
                }
            ]
        };

        Assert.Equal(2, batch.TotalCount);
        Assert.Equal(1, batch.SuccessCount);
        Assert.Equal(1, batch.FailedCount);
        Assert.Equal("OUT-001", batch.Results[0].Data?.DocumentNo);
        Assert.Equal("Insufficient stock", batch.Results[1].ErrorMessage);
    }
}
