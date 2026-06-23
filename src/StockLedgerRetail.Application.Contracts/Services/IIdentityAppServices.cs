using StockLedgerRetail.Identity;

namespace StockLedgerRetail.Services;

public interface IAuthAppService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto input, CancellationToken cancellationToken = default);

    Task<CurrentUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}

public interface IAppUserAppService
{
    Task<List<AppUserDto>> GetListAsync(CancellationToken cancellationToken = default);

    Task<AppUserDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AppUserDto> CreateAsync(CreateAppUserDto input, CancellationToken cancellationToken = default);

    Task<AppUserDto> UpdateAsync(Guid id, UpdateAppUserDto input, CancellationToken cancellationToken = default);
}

public interface IPermissionAdminAppService
{
    Task<List<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default);

    Task<List<PermissionGroupDto>> GetGroupsAsync(CancellationToken cancellationToken = default);
}

public interface ITeamAppService
{
    Task<List<TeamDto>> GetListAsync(CancellationToken cancellationToken = default);

    Task<TeamDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TeamDto> CreateAsync(CreateTeamDto input, CancellationToken cancellationToken = default);

    Task<TeamDto> UpdateAsync(Guid id, UpdateTeamDto input, CancellationToken cancellationToken = default);
}
