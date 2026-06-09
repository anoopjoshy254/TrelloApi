namespace TrelloApi.DTOs.Team;

/// <summary>
/// Request body for creating a new team.
/// </summary>
public class CreateTeamDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// Request body for updating an existing team.
/// </summary>
public class UpdateTeamDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// Response model for a team — includes member count and owner info.
/// </summary>
public class TeamResponseDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; }
    public long OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request body for adding a member to a team.
/// </summary>
public class AddTeamMemberDto
{
    public long UserId { get; set; }
    public string TeamRole { get; set; } = "Member"; // Admin | Member | Viewer
}

/// <summary>
/// Response model for a single team member entry.
/// </summary>
public class TeamMemberResponseDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string TeamRole { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

public class InviteEmailDto
{
    public string Email { get; set; } = string.Empty;
}

public class AcceptInviteDto
{
    public string Token { get; set; } = string.Empty;
}
