namespace TrelloApi.DTOs.Task;

/// <summary>
/// Request body for creating a new task within a project.
/// </summary>
public class CreateTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long ProjectId { get; set; }
    public string Priority { get; set; } = "Medium";
    public DateTime? DueDate { get; set; }
    public int? EstimatedHours { get; set; }
}

/// <summary>
/// Request body for updating an existing task.
/// </summary>
public class UpdateTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Priority { get; set; } = "Medium";
    public DateTime? DueDate { get; set; }
    public int? EstimatedHours { get; set; }
    public int? ActualHours { get; set; }
    public int? Position { get; set; }
}

/// <summary>
/// Request body for updating only the status of a task (e.g., drag-drop on board).
/// </summary>
public class UpdateTaskStatusDto
{
    public string Status { get; set; } = string.Empty; // Todo | InProgress | Review | Done | Cancelled
    public int? Position { get; set; }
}

/// <summary>
/// Request body for assigning a user to a task.
/// </summary>
public class AssignTaskDto
{
    public long UserId { get; set; }
}

/// <summary>
/// Request body for toggling a user's task assignment completion status.
/// </summary>
public class ToggleAssignmentCompletionDto
{
    public bool IsCompleted { get; set; }
}

/// <summary>
/// Full task response model with assignee and comment count.
/// </summary>
public class TaskResponseDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public int? EstimatedHours { get; set; }
    public int? ActualHours { get; set; }
    public int Position { get; set; }
    public long ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public long CreatedByUserId { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public List<TaskAssigneeDto> Assignees { get; set; } = new();
    public List<LabelDto> Labels { get; set; } = new();
    public List<ChecklistDto> Checklists { get; set; } = new();
    public int CommentCount { get; set; }
    public int AttachmentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Minimal assignee info embedded in task responses.
/// </summary>
public class TaskAssigneeDto
{
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
}
