using StockLedgerRetail.Authorization;

namespace StockLedgerRetail.Identity;

public class CurrentUserDto
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public List<string> PermissionCodes { get; set; } = new();

    public List<string> GroupCodes { get; set; } = new();
}

public class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public List<string> PermissionCodes { get; set; } = new();

    public List<string> GroupCodes { get; set; } = new();
}

public class AppUserDto
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public List<string> GroupCodes { get; set; } = new();

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class CreateAppUserDto
{
    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public List<string> GroupCodes { get; set; } = new();
}

public class UpdateAppUserDto
{
    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public List<string> GroupCodes { get; set; } = new();
}

public class PermissionDto
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Category { get; set; }
}

public class PermissionGroupDto
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public List<string> PermissionCodes { get; set; } = new();
}

public class TeamDto
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public Guid LeaderUserId { get; set; }

    public string LeaderEmail { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public List<TeamMemberDto> Members { get; set; } = new();
}

public class TeamMemberDto
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}

public class CreateTeamDto
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public Guid LeaderUserId { get; set; }

    public List<Guid> MemberUserIds { get; set; } = new();
}

public class UpdateTeamDto
{
    public string Name { get; set; } = string.Empty;

    public Guid LeaderUserId { get; set; }

    public bool IsActive { get; set; }

    public List<Guid> MemberUserIds { get; set; } = new();
}
