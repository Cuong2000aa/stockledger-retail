using Microsoft.Extensions.Logging;

namespace StockLedgerRetail.Application.Caching;

/// <summary>
/// Log cache theo format thống nhất — bật bằng Cache:LogOperations hoặc mức Debug.
/// Khi debug treo/chậm: tìm "Cache MISS" / "Cache HIT" trong log để biết có đang hit DB hay không.
/// </summary>
internal static class CacheDiagnostics
{
    public static void LogDisabled(ILogger logger, string scope) =>
        logger.LogDebug("Cache DISABLED [{Scope}] — every read goes to the database.", scope);

    public static void LogHit(ILogger logger, string scope, string cacheKey) =>
        logger.LogDebug("Cache HIT [{Scope}] key={CacheKey}", scope, cacheKey);

    public static void LogMiss(ILogger logger, string scope, string cacheKey) =>
        logger.LogDebug(
            "Cache MISS [{Scope}] key={CacheKey} — loading fresh data from database.",
            scope,
            cacheKey);

    public static void LogStored(
        ILogger logger,
        string scope,
        string cacheKey,
        TimeSpan ttl) =>
        logger.LogDebug(
            "Cache STORED [{Scope}] key={CacheKey} ttlMinutes={TtlMinutes}",
            scope,
            cacheKey,
            ttl.TotalMinutes);

    public static void LogRemoved(ILogger logger, string scope, string cacheKey) =>
        logger.LogInformation("Cache REMOVED [{Scope}] key={CacheKey}", scope, cacheKey);

    public static void LogInvalidatedPrefix(
        ILogger logger,
        string scope,
        string prefix,
        int keyCount) =>
        logger.LogInformation(
            "Cache INVALIDATED [{Scope}] prefix={CachePrefix} keysRemoved={KeyCount}",
            scope,
            prefix,
            keyCount);

    public static void LogReadFailed(ILogger logger, string scope, string cacheKey, Exception exception) =>
        logger.LogWarning(
            exception,
            "Cache READ FAILED [{Scope}] key={CacheKey} — falling back to database.",
            scope,
            cacheKey);

    public static void LogWriteFailed(ILogger logger, string scope, string cacheKey, Exception exception) =>
        logger.LogWarning(
            exception,
            "Cache WRITE FAILED [{Scope}] key={CacheKey} — response still returned but will not be cached.",
            scope,
            cacheKey);
}
