using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Authorization;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public PermissionRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<Permission>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Permissions.OrderBy(x => x.Category).ThenBy(x => x.Code).ToListAsync(cancellationToken);

    public async Task<List<string>> GetPermissionCodesByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.UserGroupAssignments
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Group.IsActive)
            .SelectMany(x => x.Group.Permissions.Select(gp => gp.Permission.Code))
            .Distinct()
            .ToListAsync(cancellationToken);

    public async Task EnsureSeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _dbContext.Permissions.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var permissions = PermissionCodes.All.Select(code => new Permission
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = code,
            Category = code.Split('.')[0]
        }).ToList();

        await _dbContext.Permissions.AddRangeAsync(permissions, cancellationToken);

        var permissionMap = permissions.ToDictionary(x => x.Code, x => x.Id);

        var groups = new List<PermissionGroup>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Code = PermissionGroupCodes.SystemAdmin,
                Name = "System Administrator",
                Description = "Full access",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Code = PermissionGroupCodes.TeamLeader,
                Name = "Team Leader",
                Description = "Manage and approve team members' inventory documents",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Code = PermissionGroupCodes.WarehouseClerk,
                Name = "Warehouse Clerk",
                Description = "Create and edit own draft documents",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Code = PermissionGroupCodes.Viewer,
                Name = "Viewer",
                Description = "Read-only access",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        await _dbContext.PermissionGroups.AddRangeAsync(groups, cancellationToken);

        var groupPermissions = new List<GroupPermission>();

        void AddGroupPerms(PermissionGroup group, params string[] codes)
        {
            foreach (var code in codes)
            {
                groupPermissions.Add(new GroupPermission
                {
                    GroupId = group.Id,
                    PermissionId = permissionMap[code]
                });
            }
        }

        var admin = groups.Single(x => x.Code == PermissionGroupCodes.SystemAdmin);
        AddGroupPerms(admin, PermissionCodes.SystemAdmin);

        var leader = groups.Single(x => x.Code == PermissionGroupCodes.TeamLeader);
        AddGroupPerms(
            leader,
            PermissionCodes.InventoryDocumentsView,
            PermissionCodes.InventoryDocumentsCreate,
            PermissionCodes.InventoryDocumentsUpdate,
            PermissionCodes.InventoryDocumentsCancel,
            PermissionCodes.InventoryDocumentsApproveTeam,
            PermissionCodes.InventoryDocumentsReceiveTransfer);

        var clerk = groups.Single(x => x.Code == PermissionGroupCodes.WarehouseClerk);
        AddGroupPerms(
            clerk,
            PermissionCodes.InventoryDocumentsView,
            PermissionCodes.InventoryDocumentsCreate,
            PermissionCodes.InventoryDocumentsUpdate,
            PermissionCodes.InventoryDocumentsCancel);

        var viewer = groups.Single(x => x.Code == PermissionGroupCodes.Viewer);
        AddGroupPerms(viewer, PermissionCodes.InventoryDocumentsView);

        await _dbContext.GroupPermissions.AddRangeAsync(groupPermissions, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
