using Microsoft.Extensions.Logging;
using StockLedgerRetail.Application.Identity;
using StockLedgerRetail.Application.Seed;
using StockLedgerRetail.Authorization;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Seed;

public class DemoUserSeedService : IDemoUserSeedService
{
    private const string DemoClerkEmail = "clerk@stockledger.local";

    private readonly IAppUserRepository _appUserRepository;
    private readonly IPermissionGroupRepository _permissionGroupRepository;
    private readonly IUserWarehouseAssignmentRepository _userWarehouseAssignmentRepository;
    private readonly ILogger<DemoUserSeedService> _logger;

    public DemoUserSeedService(
        IAppUserRepository appUserRepository,
        IPermissionGroupRepository permissionGroupRepository,
        IUserWarehouseAssignmentRepository userWarehouseAssignmentRepository,
        ILogger<DemoUserSeedService> logger)
    {
        _appUserRepository = appUserRepository;
        _permissionGroupRepository = permissionGroupRepository;
        _userWarehouseAssignmentRepository = userWarehouseAssignmentRepository;
        _logger = logger;
    }

    public async Task EnsureDemoClerkAsync(CancellationToken cancellationToken = default)
    {
        var clerkGroup = await _permissionGroupRepository.GetByCodeAsync(
            PermissionGroupCodes.WarehouseClerk,
            cancellationToken);
        if (clerkGroup is null)
        {
            return;
        }

        var user = await _appUserRepository.GetByEmailAsync(DemoClerkEmail, cancellationToken);
        if (user is null)
        {
            var now = DateTime.UtcNow;
            user = new AppUser
            {
                Id = Guid.NewGuid(),
                Email = DemoClerkEmail,
                DisplayName = "Warehouse Clerk",
                PasswordHash = UserPasswordHasher.Hash("1234"),
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _appUserRepository.InsertAsync(user, cancellationToken);
            await _permissionGroupRepository.AssignUserToGroupAsync(user.Id, clerkGroup.Id, cancellationToken);
            await _appUserRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Demo clerk user created at {Email}", DemoClerkEmail);
        }

        var withAssignments = await _appUserRepository.GetByIdWithAssignmentsAsync(user.Id, cancellationToken);
        if (withAssignments is null || withAssignments.WarehouseAssignments.Count > 0)
        {
            return;
        }

        await _userWarehouseAssignmentRepository.ReplaceForUserAsync(
            user.Id,
            [(FbDataSeedService.DominosStoreId, true)],
            cancellationToken);

        _logger.LogInformation(
            "Assigned demo warehouse {WarehouseId} to {Email}",
            FbDataSeedService.DominosStoreId,
            DemoClerkEmail);
    }
}
