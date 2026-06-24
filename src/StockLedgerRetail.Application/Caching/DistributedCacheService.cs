using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockLedgerRetail.Caching;

namespace StockLedgerRetail.Application.Caching;

/// <summary>
/// Cache-aside qua IDistributedCache (Redis hoặc memory khi Redis tắt).
/// Lỗi cache không làm API fail — luôn fallback đọc DB ở tầng gọi phía trên.
/// </summary>
public class DistributedCacheService : ICacheService
{
    private const string LogScope = "DistributedCache";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IDistributedCache _distributedCache;
    private readonly CacheOptions _options;
    private readonly ILogger<DistributedCacheService> _logger;

    /// <summary>
    /// Chỉ dùng cho RemoveByPrefixAsync trên in-memory cache (một instance).
    /// Redis production: prefix delete cần SCAN — hiện invalidate theo prefix best-effort trên instance local.
    /// </summary>
    private readonly ConcurrentDictionary<string, byte> _knownKeysOnThisInstance = new();

    public DistributedCacheService(
        IDistributedCache distributedCache,
        IOptions<CacheOptions> options,
        ILogger<DistributedCacheService> logger)
    {
        _distributedCache = distributedCache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        if (!_options.Enabled)
        {
            if (_options.LogOperations)
            {
                CacheDiagnostics.LogDisabled(_logger, LogScope);
            }

            return null;
        }

        try
        {
            var bytes = await _distributedCache.GetAsync(key, cancellationToken);
            if (bytes is null || bytes.Length == 0)
            {
                if (_options.LogOperations)
                {
                    CacheDiagnostics.LogMiss(_logger, LogScope, key);
                }

                return null;
            }

            var value = JsonSerializer.Deserialize<T>(bytes, JsonOptions);
            if (_options.LogOperations && value is not null)
            {
                CacheDiagnostics.LogHit(_logger, LogScope, key);
            }

            return value;
        }
        catch (Exception ex)
        {
            CacheDiagnostics.LogReadFailed(_logger, LogScope, key, ex);
            return null;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (!_options.Enabled)
        {
            return;
        }

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            await _distributedCache.SetAsync(
                key,
                bytes,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
                cancellationToken);

            RememberKeyForLocalPrefixDelete(key);

            if (_options.LogOperations)
            {
                CacheDiagnostics.LogStored(_logger, LogScope, key, ttl);
            }
        }
        catch (Exception ex)
        {
            CacheDiagnostics.LogWriteFailed(_logger, LogScope, key, ex);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            _knownKeysOnThisInstance.TryRemove(key, out _);

            if (_options.LogOperations)
            {
                CacheDiagnostics.LogRemoved(_logger, LogScope, key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache remove failed for key {CacheKey}.", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var matchingKeys = _knownKeysOnThisInstance.Keys
            .Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();

        foreach (var key in matchingKeys)
        {
            await RemoveAsync(key, cancellationToken);
        }

        CacheDiagnostics.LogInvalidatedPrefix(_logger, LogScope, prefix, matchingKeys.Count);
    }

    private void RememberKeyForLocalPrefixDelete(string key) =>
        _knownKeysOnThisInstance.TryAdd(key, 0);
}
