namespace StockLedgerRetail.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class;

    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
        where T : class;

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}

public interface IInventoryCacheInvalidator
{
    Task InvalidateStockAsync(Guid warehouseId, Guid productVariantId, CancellationToken cancellationToken = default);

    Task InvalidateWarehouseReportsAsync(Guid? warehouseId, CancellationToken cancellationToken = default);
}

public interface IUserAuthCacheService
{
    Task<UserAuthCacheEntry?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task InvalidateUserAsync(string email, CancellationToken cancellationToken = default);

    Task InvalidateAllUsersAsync(CancellationToken cancellationToken = default);
}

public class UserAuthCacheEntry
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public List<string> PermissionCodes { get; set; } = new();
}
