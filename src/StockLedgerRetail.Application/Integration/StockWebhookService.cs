using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockLedgerRetail.Integration;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Integration;

public class StockWebhookService : IStockWebhookService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    private readonly StockWebhookOptions _options;
    private readonly ILogger<StockWebhookService> _logger;

    public StockWebhookService(
        IOptions<StockWebhookOptions> options,
        ILogger<StockWebhookService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task NotifyStockChangedAsync(
        StockChangedEventDto eventDto,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || _options.Endpoints.Count == 0)
        {
            return Task.CompletedTask;
        }

        return NotifyBatchStockChangedAsync([eventDto], cancellationToken);
    }

    public async Task NotifyBatchStockChangedAsync(
        IEnumerable<StockChangedEventDto> eventDtos,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || _options.Endpoints.Count == 0)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(eventDtos);

        foreach (var endpoint in _options.Endpoints)
        {
            if (string.IsNullOrWhiteSpace(endpoint)) continue;

            // Fire and dispatch to external webhook endpoints
            _ = DispatchPayloadAsync(endpoint, payload);
        }

        await Task.CompletedTask;
    }

    public async Task<DispatchWebhookTestResponseDto> TestWebhookDispatchAsync(
        string? targetUrl = null,
        CancellationToken cancellationToken = default)
    {
        var sampleEvent = new StockChangedEventDto
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = "stock.changed.test",
            Sku = "TEST-SKU-001",
            WarehouseCode = "STORE-001",
            OnHandQuantity = 100,
            AvailableQuantity = 95,
            ChangeDelta = -5,
            Reason = "Webhook delivery test",
            SourceSystem = "TEST",
            ReferenceNo = "TEST-REF-001",
            OccurredAtUtc = DateTime.UtcNow
        };

        var endpoints = !string.IsNullOrWhiteSpace(targetUrl)
            ? [targetUrl]
            : _options.Endpoints;

        if (endpoints.Count == 0)
        {
            return new DispatchWebhookTestResponseDto
            {
                Success = false,
                DispatchedCount = 0,
                Message = "No webhook endpoints configured in Integration:Webhooks:Endpoints."
            };
        }

        var payload = JsonSerializer.Serialize(new[] { sampleEvent });
        var successCount = 0;

        foreach (var endpoint in endpoints)
        {
            var ok = await DispatchPayloadAsync(endpoint, payload);
            if (ok) successCount++;
        }

        return new DispatchWebhookTestResponseDto
        {
            Success = successCount > 0,
            DispatchedCount = successCount,
            Message = $"Dispatched test event to {successCount}/{endpoints.Count} endpoint(s)."
        };
    }

    private async Task<bool> DispatchPayloadAsync(string url, string jsonPayload)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(_options.SecretKey))
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SecretKey));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(jsonPayload));
                request.Headers.Add("X-StockLedger-Signature", Convert.ToHexString(hash).ToLowerInvariant());
            }

            request.Headers.Add("X-StockLedger-Event", "stock.changed");

            var response = await HttpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Successfully dispatched stock webhook to {Url}", url);
                return true;
            }

            _logger.LogWarning("Webhook to {Url} responded with status code {StatusCode}", url, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dispatch stock webhook to {Url}", url);
            return false;
        }
    }
}
