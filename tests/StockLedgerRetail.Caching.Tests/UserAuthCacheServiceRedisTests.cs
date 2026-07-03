using Microsoft.Extensions.DependencyInjection;
using StockLedgerRetail.Authorization;
using StockLedgerRetail.Caching;
using StockLedgerRetail.Domain.Entities;
using Xunit;

namespace StockLedgerRetail.Caching.Tests;

[Collection(RedisCollection.Name)]
public sealed class UserAuthCacheServiceRedisTests(RedisTestFixture fixture) : RedisTestBase(fixture)
{
    [Fact]
    public async Task GetByEmailAsync_SecondCallUsesRedisWithoutDbQuery()
    {
        RequireRedis();

        Fixture.FakeUserRepository.Reset();
        var email = $"redis-user-{Guid.NewGuid():N}@stockledger.local";
        Fixture.FakeUserRepository.SetUser(CreateUser(email));

        await using var scope = Fixture.CreateScope();
        var authCache = scope.ServiceProvider.GetRequiredService<IUserAuthCacheService>();

        var first = await authCache.GetByEmailAsync(email);
        var second = await authCache.GetByEmailAsync(email);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first.UserId, second.UserId);
        Assert.Equal(1, Fixture.FakeUserRepository.GetByEmailWithPermissionsCallCount);

        await authCache.InvalidateUserAsync(email);
    }

    [Fact]
    public async Task InvalidateUserAsync_ForcesDatabaseReload()
    {
        RequireRedis();

        Fixture.FakeUserRepository.Reset();
        var email = $"invalidate-{Guid.NewGuid():N}@stockledger.local";
        Fixture.FakeUserRepository.SetUser(CreateUser(email));

        await using var scope = Fixture.CreateScope();
        var authCache = scope.ServiceProvider.GetRequiredService<IUserAuthCacheService>();

        await authCache.GetByEmailAsync(email);
        await authCache.InvalidateUserAsync(email);
        await authCache.GetByEmailAsync(email);

        Assert.Equal(2, Fixture.FakeUserRepository.GetByEmailWithPermissionsCallCount);
    }

    [Fact]
    public async Task InvalidateAllUsersAsync_ClearsTrackedAuthEntries()
    {
        RequireRedis();

        Fixture.FakeUserRepository.Reset();
        var emailA = $"bulk-a-{Guid.NewGuid():N}@stockledger.local";
        var emailB = $"bulk-b-{Guid.NewGuid():N}@stockledger.local";
        Fixture.FakeUserRepository.SetUser(CreateUser(emailA));
        Fixture.FakeUserRepository.SetUser(CreateUser(emailB));

        await using var scope = Fixture.CreateScope();
        var authCache = scope.ServiceProvider.GetRequiredService<IUserAuthCacheService>();

        await authCache.GetByEmailAsync(emailA);
        await authCache.GetByEmailAsync(emailB);
        await authCache.InvalidateAllUsersAsync();

        await authCache.GetByEmailAsync(emailA);
        await authCache.GetByEmailAsync(emailB);

        Assert.Equal(4, Fixture.FakeUserRepository.GetByEmailWithPermissionsCallCount);
    }

    private static AppUser CreateUser(string email) =>
        new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = "Redis Test User",
            IsActive = true,
            GroupAssignments =
            [
                new UserGroupAssignment
                {
                    Group = new PermissionGroup
                    {
                        IsActive = true,
                        Permissions =
                        [
                            new GroupPermission
                            {
                                Permission = new Permission
                                {
                                    Code = PermissionCodes.SystemAdmin
                                }
                            }
                        ]
                    }
                }
            ]
        };
}
