using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Identity;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthAppService _authAppService;

    public AuthController(IAuthAppService authAppService)
    {
        _authAppService = authAppService;
    }

    [HttpPost("login")]
    public Task<LoginResponseDto> LoginAsync(
        [FromBody] LoginRequestDto input,
        CancellationToken cancellationToken) =>
        _authAppService.LoginAsync(input, cancellationToken);

    [HttpGet("me")]
    public Task<CurrentUserDto> GetCurrentUserAsync(CancellationToken cancellationToken) =>
        _authAppService.GetCurrentUserAsync(cancellationToken);
}

[ApiController]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IAppUserAppService _appUserAppService;

    public AdminUsersController(IAppUserAppService appUserAppService)
    {
        _appUserAppService = appUserAppService;
    }

    [HttpGet]
    public Task<List<AppUserDto>> GetListAsync(CancellationToken cancellationToken) =>
        _appUserAppService.GetListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<AppUserDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _appUserAppService.GetAsync(id, cancellationToken);

    [HttpPost]
    public Task<AppUserDto> CreateAsync([FromBody] CreateAppUserDto input, CancellationToken cancellationToken) =>
        _appUserAppService.CreateAsync(input, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<AppUserDto> UpdateAsync(Guid id, [FromBody] UpdateAppUserDto input, CancellationToken cancellationToken) =>
        _appUserAppService.UpdateAsync(id, input, cancellationToken);
}

[ApiController]
[Route("api/admin/permissions")]
public class AdminPermissionsController : ControllerBase
{
    private readonly IPermissionAdminAppService _permissionAdminAppService;

    public AdminPermissionsController(IPermissionAdminAppService permissionAdminAppService)
    {
        _permissionAdminAppService = permissionAdminAppService;
    }

    [HttpGet]
    public Task<List<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken) =>
        _permissionAdminAppService.GetPermissionsAsync(cancellationToken);

    [HttpGet("groups")]
    public Task<List<PermissionGroupDto>> GetGroupsAsync(CancellationToken cancellationToken) =>
        _permissionAdminAppService.GetGroupsAsync(cancellationToken);
}

[ApiController]
[Route("api/admin/teams")]
public class AdminTeamsController : ControllerBase
{
    private readonly ITeamAppService _teamAppService;

    public AdminTeamsController(ITeamAppService teamAppService)
    {
        _teamAppService = teamAppService;
    }

    [HttpGet]
    public Task<List<TeamDto>> GetListAsync(CancellationToken cancellationToken) =>
        _teamAppService.GetListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<TeamDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _teamAppService.GetAsync(id, cancellationToken);

    [HttpPost]
    public Task<TeamDto> CreateAsync([FromBody] CreateTeamDto input, CancellationToken cancellationToken) =>
        _teamAppService.CreateAsync(input, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<TeamDto> UpdateAsync(Guid id, [FromBody] UpdateTeamDto input, CancellationToken cancellationToken) =>
        _teamAppService.UpdateAsync(id, input, cancellationToken);
}
