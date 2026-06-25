using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Identity;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>
/// API xác thực và thông tin người dùng hiện tại.
/// Phiên bản hiện tại dùng login stub/header-based để demo RBAC và admin UI.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthAppService _authAppService;

    public AuthController(IAuthAppService authAppService)
    {
        _authAppService = authAppService;
    }

    /// <summary>Đăng nhập và trả về thông tin phiên làm việc dùng cho frontend.</summary>
    [HttpPost("login")]
    public Task<LoginResponseDto> LoginAsync(
        [FromBody] LoginRequestDto input,
        CancellationToken cancellationToken) =>
        _authAppService.LoginAsync(input, cancellationToken);

    /// <summary>Lấy thông tin người dùng hiện tại và quyền đang có.</summary>
    [HttpGet("me")]
    public Task<CurrentUserDto> GetCurrentUserAsync(CancellationToken cancellationToken) =>
        _authAppService.GetCurrentUserAsync(cancellationToken);
}

/// <summary>
/// API admin quản lý người dùng ứng dụng và gán nhóm quyền/đội nhóm.
/// </summary>
[ApiController]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IAppUserAppService _appUserAppService;

    public AdminUsersController(IAppUserAppService appUserAppService)
    {
        _appUserAppService = appUserAppService;
    }

    /// <summary>Lấy danh sách toàn bộ người dùng ứng dụng.</summary>
    [HttpGet]
    public Task<List<AppUserDto>> GetListAsync(CancellationToken cancellationToken) =>
        _appUserAppService.GetListAsync(cancellationToken);

    /// <summary>Lấy chi tiết một người dùng theo Id.</summary>
    [HttpGet("{id:guid}")]
    public Task<AppUserDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _appUserAppService.GetAsync(id, cancellationToken);

    /// <summary>Tạo người dùng mới để dùng cho đăng nhập header-based và RBAC.</summary>
    [HttpPost]
    public Task<AppUserDto> CreateAsync([FromBody] CreateAppUserDto input, CancellationToken cancellationToken) =>
        _appUserAppService.CreateAsync(input, cancellationToken);

    /// <summary>Cập nhật thông tin người dùng, nhóm quyền hoặc team theo Id.</summary>
    [HttpPut("{id:guid}")]
    public Task<AppUserDto> UpdateAsync(Guid id, [FromBody] UpdateAppUserDto input, CancellationToken cancellationToken) =>
        _appUserAppService.UpdateAsync(id, input, cancellationToken);
}

/// <summary>
/// API admin tra cứu danh mục quyền và nhóm quyền đang cấu hình trong hệ thống.
/// </summary>
[ApiController]
[Route("api/admin/permissions")]
public class AdminPermissionsController : ControllerBase
{
    private readonly IPermissionAdminAppService _permissionAdminAppService;

    public AdminPermissionsController(IPermissionAdminAppService permissionAdminAppService)
    {
        _permissionAdminAppService = permissionAdminAppService;
    }

    /// <summary>Lấy danh sách quyền chi tiết.</summary>
    [HttpGet]
    public Task<List<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken) =>
        _permissionAdminAppService.GetPermissionsAsync(cancellationToken);

    /// <summary>Lấy danh sách nhóm quyền (permission groups).</summary>
    [HttpGet("groups")]
    public Task<List<PermissionGroupDto>> GetGroupsAsync(CancellationToken cancellationToken) =>
        _permissionAdminAppService.GetGroupsAsync(cancellationToken);
}

/// <summary>
/// API admin quản lý team nội bộ để phân quyền leader/member trên chứng từ.
/// </summary>
[ApiController]
[Route("api/admin/teams")]
public class AdminTeamsController : ControllerBase
{
    private readonly ITeamAppService _teamAppService;

    public AdminTeamsController(ITeamAppService teamAppService)
    {
        _teamAppService = teamAppService;
    }

    /// <summary>Lấy danh sách team.</summary>
    [HttpGet]
    public Task<List<TeamDto>> GetListAsync(CancellationToken cancellationToken) =>
        _teamAppService.GetListAsync(cancellationToken);

    /// <summary>Lấy chi tiết một team theo Id.</summary>
    [HttpGet("{id:guid}")]
    public Task<TeamDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _teamAppService.GetAsync(id, cancellationToken);

    /// <summary>Tạo team mới.</summary>
    [HttpPost]
    public Task<TeamDto> CreateAsync([FromBody] CreateTeamDto input, CancellationToken cancellationToken) =>
        _teamAppService.CreateAsync(input, cancellationToken);

    /// <summary>Cập nhật thông tin team theo Id.</summary>
    [HttpPut("{id:guid}")]
    public Task<TeamDto> UpdateAsync(Guid id, [FromBody] UpdateTeamDto input, CancellationToken cancellationToken) =>
        _teamAppService.UpdateAsync(id, input, cancellationToken);
}
