using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.Caching.Tests.Support;

public sealed class FakeAppUserRepository : IAppUserRepository
{
    private readonly Dictionary<string, AppUser> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);

    public int GetByEmailWithPermissionsCallCount { get; private set; }

    public void Reset()
    {
        GetByEmailWithPermissionsCallCount = 0;
        _usersByEmail.Clear();
    }

    public void SetUser(AppUser user) => _usersByEmail[user.Email] = user;

    public Task<AppUser?> GetByEmailWithPermissionsAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        GetByEmailWithPermissionsCallCount++;
        _usersByEmail.TryGetValue(email, out var user);
        return Task.FromResult(user);
    }

    public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<AppUser?> GetByIdWithPermissionsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_usersByEmail.Values.FirstOrDefault(u => u.Id == id));

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<AppUser?> GetByIdWithAssignmentsAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<List<AppUser>> GetListAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task InsertAsync(AppUser user, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task UpdateAsync(AppUser user, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
