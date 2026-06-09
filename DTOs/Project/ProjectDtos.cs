namespace TrelloApi.DTOs.Project;

/// <summary>
/// Request body for creating a new project.
/// </summary>
public class CreateProjectDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? TeamId { get; set; }
    public string? Color { get; set; }
    public DateTime? DueDate { get; set; }
}

/// <summary>
/// Request body for updating an existing project.
/// </summary>
public class UpdateProjectDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Active";
    public string? Color { get; set; }
    public DateTime? DueDate { get; set; }
}

/// <summary>
/// Response model for a project — includes member count and task stats.
/// </summary>
public class ProjectResponseDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Color { get; set; }
    public DateTime? DueDate { get; set; }
    public long OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public long? TeamId { get; set; }
    public string? TeamName { get; set; }
    public int MemberCount { get; set; }
    public int TaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string[] Columns { get; set; } = new string[] { "Todo", "InProgress", "Review", "Done" };
}

public class UpdateProjectColumnsDto
{
    public string[] Columns { get; set; } = Array.Empty<string>();
}
