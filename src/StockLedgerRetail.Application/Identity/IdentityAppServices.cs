using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StockLedgerRetail.Audit;
using StockLedgerRetail.Authorization;
using StockLedgerRetail.Caching;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Identity;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Identity;

public class AuthAppService : IAuthAppService
{
    private readonly ICurrentUserContext _currentUser;
    private readonly IAppUserRepository _appUserRepository;
    private readonly StubAuthOptions _stubAuth;
    private readonly string? _bootstrapAdminEmail;

    public AuthAppService(
        ICurrentUserContext currentUser,
        IAppUserRepository appUserRepository,
        IOptions<StubAuthOptions> stubAuth,
        IConfiguration configuration)
    {
        _currentUser = currentUser;
        _appUserRepository = appUserRepository;
        _stubAuth = stubAuth.Value;
        _bootstrapAdminEmail = configuration["Auth:BootstrapAdminEmail"];
    }

    public async Task<LoginResponseDto> LoginAsync(
        LoginRequestDto input,
        CancellationToken cancellationToken = default)
    {
        if (!_stubAuth.Enabled)
        {
            throw new InvalidOperationException("Stub login is disabled.");
        }

        if (!string.Equals(input.Username?.Trim(), _stubAuth.Username, StringComparison.OrdinalIgnoreCase)
            || input.Password != _stubAuth.Password)
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        var email = (_stubAuth.UserEmail ?? _bootstrapAdminEmail)?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Stub login user email is not configured.");
        }

        var user = await _appUserRepository.GetByEmailWithPermissionsAsync(email, cancellationToken)
            ?? throw new UnauthorizedAccessException(
                $"User '{email}' is not registered. Start the API once to bootstrap admin.");

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("User is inactive.");
        }

        return MapLoginResponse(user);
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Not authenticated.");
        }

        var user = await _appUserRepository.GetByIdAsync(_currentUser.UserId.Value, cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found.");

        return new CurrentUserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            PermissionCodes = _currentUser.PermissionCodes.ToList(),
            GroupCodes = user.GroupAssignments.Select(x => x.Group.Code).ToList()
        };
    }

    private static LoginResponseDto MapLoginResponse(AppUser user) => new()
    {
        Email = user.Email,
        DisplayName = user.DisplayName,
        PermissionCodes = user.GroupAssignments
            .Where(x => x.Group.IsActive)
            .SelectMany(x => x.Group.Permissions)
            .Select(x => x.Permission.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList(),
        GroupCodes = user.GroupAssignments
            .Where(x => x.Group.IsActive)
            .Select(x => x.Group.Code)
            .ToList()
    };
}

public class AppUserAppService : IAppUserAppService
{
    private readonly IAppUserRepository _appUserRepository;
    private readonly IPermissionGroupRepository _permissionGroupRepository;
    private readonly IPermissionAuthorizationService _authorizationService;
    private readonly IUserAuthCacheService _userAuthCacheService;

    public AppUserAppService(
        IAppUserRepository appUserRepository,
        IPermissionGroupRepository permissionGroupRepository,
        IPermissionAuthorizationService authorizationService,
        IUserAuthCacheService userAuthCacheService)
    {
        _appUserRepository = appUserRepository;
        _permissionGroupRepository = permissionGroupRepository;
        _authorizationService = authorizationService;
        _userAuthCacheService = userAuthCacheService;
    }

    public async Task<List<AppUserDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        _authorizationService.EnsureAdminUsersManage();
        var users = await _appUserRepository.GetListAsync(cancellationToken);
        return users.Select(MapToDto).ToList();
    }

    public async Task<AppUserDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _authorizationService.EnsureAdminUsersManage();
        var user = await _appUserRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"User '{id}' was not found.");
        return MapToDto(user);
    }

    public async Task<AppUserDto> CreateAsync(CreateAppUserDto input, CancellationToken cancellationToken = default)
    {
        _authorizationService.EnsureAdminUsersManage();

        var email = NormalizeEmail(input.Email);
        var existing = await _appUserRepository.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"User '{email}' already exists.");
        }

        var now = DateTime.UtcNow;
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = input.DisplayName.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _appUserRepository.InsertAsync(user, cancellationToken);
        await SyncGroupsAsync(user.Id, input.GroupCodes, cancellationToken);
        await _appUserRepository.SaveChangesAsync(cancellationToken);
        await _userAuthCacheService.InvalidateUserAsync(email, cancellationToken);

        return MapToDto((await _appUserRepository.GetByIdAsync(user.Id, cancellationToken))!);
    }

    public async Task<AppUserDto> UpdateAsync(Guid id, UpdateAppUserDto input, CancellationToken cancellationToken = default)
    {
        _authorizationService.EnsureAdminUsersManage();

        var user = await _appUserRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"User '{id}' was not found.");

        user.DisplayName = input.DisplayName.Trim();
        user.IsActive = input.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _appUserRepository.UpdateAsync(user, cancellationToken);
        await SyncGroupsAsync(user.Id, input.GroupCodes, cancellationToken);
        await _appUserRepository.SaveChangesAsync(cancellationToken);
        await _userAuthCacheService.InvalidateUserAsync(user.Email, cancellationToken);

        return MapToDto((await _appUserRepository.GetByIdAsync(user.Id, cancellationToken))!);
    }

    private async Task SyncGroupsAsync(Guid userId, List<string> groupCodes, CancellationToken cancellationToken)
    {
        var user = await _appUserRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException($"User '{userId}' was not found.");

        var desiredCodes = groupCodes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToUpperInvariant())
            .Distinct()
            .ToHashSet();

        var allGroups = await _permissionGroupRepository.GetListAsync(cancellationToken);
        var desiredGroupIds = allGroups
            .Where(x => desiredCodes.Contains(x.Code))
            .Select(x => x.Id)
            .ToHashSet();

        foreach (var assignment in user.GroupAssignments.ToList())
        {
            if (!desiredGroupIds.Contains(assignment.GroupId))
            {
                await _permissionGroupRepository.RemoveUserFromGroupAsync(
                    userId, assignment.GroupId, cancellationToken);
            }
        }

        foreach (var groupId in desiredGroupIds)
        {
            await _permissionGroupRepository.AssignUserToGroupAsync(userId, groupId, cancellationToken);
        }

        await _permissionGroupRepository.SaveChangesAsync(cancellationToken);
    }

    private static AppUserDto MapToDto(AppUser user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        DisplayName = user.DisplayName,
        IsActive = user.IsActive,
        GroupCodes = user.GroupAssignments.Select(x => x.Group.Code).ToList(),
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Email is required.");
        }

        return email.Trim().ToLowerInvariant();
    }
}

public class PermissionAdminAppService : IPermissionAdminAppService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IPermissionGroupRepository _permissionGroupRepository;
    private readonly IPermissionAuthorizationService _authorizationService;

    public PermissionAdminAppService(
        IPermissionRepository permissionRepository,
        IPermissionGroupRepository permissionGroupRepository,
        IPermissionAuthorizationService authorizationService)
    {
        _permissionRepository = permissionRepository;
        _permissionGroupRepository = permissionGroupRepository;
        _authorizationService = authorizationService;
    }

    public async Task<List<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        _authorizationService.EnsureAdminGroupsManage();
        var permissions = await _permissionRepository.GetAllAsync(cancellationToken);
        return permissions.Select(x => new PermissionDto
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            Category = x.Category
        }).ToList();
    }

    public async Task<List<PermissionGroupDto>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        _authorizationService.EnsureAdminGroupsManage();
        var groups = await _permissionGroupRepository.GetListAsync(cancellationToken);
        return groups.Select(x => new PermissionGroupDto
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            IsActive = x.IsActive,
            PermissionCodes = x.Permissions.Select(p => p.Permission.Code).ToList()
        }).ToList();
    }
}

public class TeamAppService : ITeamAppService
{
    private readonly ITeamRepository _teamRepository;
    private readonly IAppUserRepository _appUserRepository;
    private readonly IPermissionAuthorizationService _authorizationService;

    public TeamAppService(
        ITeamRepository teamRepository,
        IAppUserRepository appUserRepository,
        IPermissionAuthorizationService authorizationService)
    {
        _teamRepository = teamRepository;
        _appUserRepository = appUserRepository;
        _authorizationService = authorizationService;
    }

    public async Task<List<TeamDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        _authorizationService.EnsureAdminTeamsManage();
        var teams = await _teamRepository.GetListAsync(cancellationToken);
        return teams.Select(MapToDto).ToList();
    }

    public async Task<TeamDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _authorizationService.EnsureAdminTeamsManage();
        var team = await _teamRepository.GetByIdWithMembersAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Team '{id}' was not found.");
        return MapToDto(team);
    }

    public async Task<TeamDto> CreateAsync(CreateTeamDto input, CancellationToken cancellationToken = default)
    {
        _authorizationService.EnsureAdminTeamsManage();
        await EnsureUsersExistAsync(input.LeaderUserId, input.MemberUserIds, cancellationToken);

        var now = DateTime.UtcNow;
        var teamId = Guid.NewGuid();
        var members = BuildMembers(input.LeaderUserId, input.MemberUserIds, now);
        foreach (var member in members)
        {
            member.TeamId = teamId;
        }

        var team = new Team
        {
            Id = teamId,
            Code = input.Code.Trim().ToUpperInvariant(),
            Name = input.Name.Trim(),
            LeaderUserId = input.LeaderUserId,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            Members = members
        };

        await _teamRepository.InsertAsync(team, cancellationToken);
        await _teamRepository.SaveChangesAsync(cancellationToken);

        return MapToDto((await _teamRepository.GetByIdWithMembersAsync(team.Id, cancellationToken))!);
    }

    public async Task<TeamDto> UpdateAsync(Guid id, UpdateTeamDto input, CancellationToken cancellationToken = default)
    {
        _authorizationService.EnsureAdminTeamsManage();

        var team = await _teamRepository.GetByIdWithMembersAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Team '{id}' was not found.");

        await EnsureUsersExistAsync(input.LeaderUserId, input.MemberUserIds, cancellationToken);

        team.Name = input.Name.Trim();
        team.LeaderUserId = input.LeaderUserId;
        team.IsActive = input.IsActive;
        team.UpdatedAt = DateTime.UtcNow;

        team.Members.Clear();
        foreach (var member in BuildMembers(input.LeaderUserId, input.MemberUserIds, DateTime.UtcNow))
        {
            member.TeamId = team.Id;
            team.Members.Add(member);
        }

        await _teamRepository.UpdateAsync(team, cancellationToken);
        await _teamRepository.SaveChangesAsync(cancellationToken);

        return MapToDto((await _teamRepository.GetByIdWithMembersAsync(team.Id, cancellationToken))!);
    }

    private async Task EnsureUsersExistAsync(
        Guid leaderUserId,
        IReadOnlyCollection<Guid> memberUserIds,
        CancellationToken cancellationToken)
    {
        var allIds = memberUserIds.Append(leaderUserId).Distinct().ToList();
        foreach (var userId in allIds)
        {
            _ = await _appUserRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new InvalidOperationException($"User '{userId}' was not found.");
        }
    }

    private static List<TeamMember> BuildMembers(
        Guid leaderUserId,
        IReadOnlyCollection<Guid> memberUserIds,
        DateTime joinedAt)
    {
        var memberIds = memberUserIds
            .Append(leaderUserId)
            .Distinct()
            .ToList();

        return memberIds.Select(userId => new TeamMember
        {
            UserId = userId,
            JoinedAt = joinedAt
        }).ToList();
    }

    private static TeamDto MapToDto(Team team) => new()
    {
        Id = team.Id,
        Code = team.Code,
        Name = team.Name,
        LeaderUserId = team.LeaderUserId,
        LeaderEmail = team.Leader?.Email ?? string.Empty,
        IsActive = team.IsActive,
        Members = team.Members.Select(m => new TeamMemberDto
        {
            UserId = m.UserId,
            Email = m.User.Email,
            DisplayName = m.User.DisplayName
        }).ToList()
    };
}
