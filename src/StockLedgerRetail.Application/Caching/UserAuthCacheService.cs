using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockLedgerRetail.Caching;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.Application.Caching;

/// <summary>
/// Cache quyền user sau middleware auth — giảm JOIN permission mỗi HTTP request.
/// </summary>
public class UserAuthCacheService : IUserAuthCacheService
{
    private const string LogScope = "Auth";

    private readonly IAppUserRepository _appUserRepository;
    private readonly ICacheService _cacheService;
    private readonly CacheOptions _options;
    private readonly ILogger<UserAuthCacheService> _logger;

    public UserAuthCacheService(
        IAppUserRepository appUserRepository,
        ICacheService cacheService,
        IOptions<CacheOptions> options,
        ILogger<UserAuthCacheService> logger)
    {
        _appUserRepository = appUserRepository;
        _cacheService = cacheService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<UserAuthCacheEntry?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var cacheKey = CacheKeys.AuthUser(normalizedEmail);

        var cachedEntry = await _cacheService.GetAsync<UserAuthCacheEntry>(cacheKey, cancellationToken);
        if (cachedEntry is not null)
        {
            if (_options.LogOperations)
            {
                _logger.LogDebug(
                    "Auth resolved from cache for {Email} with {PermissionCount} permission(s).",
                    normalizedEmail,
                    cachedEntry.PermissionCodes.Count);
            }

            return cachedEntry;
        }

        if (_options.LogOperations)
        {
            _logger.LogDebug(
                "Auth cache miss for {Email} — querying database (GetByEmailWithPermissionsAsync).",
                normalizedEmail);
        }

        var userFromDatabase = await _appUserRepository.GetByEmailWithPermissionsAsync(
            normalizedEmail,
            cancellationToken);

        if (userFromDatabase is null)
        {
            _logger.LogDebug("Auth lookup: user {Email} not found in database.", normalizedEmail);
            return null;
        }

        var entryToCache = MapUserToCacheEntry(userFromDatabase);
        var ttl = TimeSpan.FromMinutes(_options.AuthTtlMinutes);

        await _cacheService.SetAsync(cacheKey, entryToCache, ttl, cancellationToken);

        if (_options.LogOperations)
        {
            _logger.LogDebug(
                "Auth cached for {Email}: userId={UserId}, permissionCount={PermissionCount}, ttlMinutes={TtlMinutes}.",
                normalizedEmail,
                entryToCache.UserId,
                entryToCache.PermissionCodes.Count,
                ttl.TotalMinutes);
        }

        return entryToCache;
    }

    public async Task InvalidateUserAsync(string email, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.AuthUser(NormalizeEmail(email));
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);
        _logger.LogInformation("Auth cache invalidated for {Email} (key={CacheKey}).", email, cacheKey);
    }

    public async Task InvalidateAllUsersAsync(CancellationToken cancellationToken = default)
    {
        await _cacheService.RemoveByPrefixAsync(CacheKeys.AuthUserPrefix, cancellationToken);
        _logger.LogInformation("Auth cache invalidated for all users (prefix={CachePrefix}).", CacheKeys.AuthUserPrefix);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static UserAuthCacheEntry MapUserToCacheEntry(AppUser user)
    {
        var activePermissionCodes = user.GroupAssignments
            .Where(assignment => assignment.Group.IsActive)
            .SelectMany(assignment => assignment.Group.Permissions)
            .Select(groupPermission => groupPermission.Permission.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new UserAuthCacheEntry
        {
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            IsActive = user.IsActive,
            PermissionCodes = activePermissionCodes,
            WarehouseIds = user.WarehouseAssignments
                .Select(x => x.WarehouseId)
                .Distinct()
                .ToList(),
            PrimaryWarehouseId = user.WarehouseAssignments
                .FirstOrDefault(x => x.IsPrimary)?.WarehouseId
                ?? user.WarehouseAssignments.Select(x => x.WarehouseId).FirstOrDefault()
        };
    }
}
