namespace TrelloApi.Models;

/// <summary>
/// Join table: represents which user is assigned to which task.
/// Many-to-Many between User and TaskItem with assignment metadata.
/// </summary>
public class TaskAssignment
{
    public long Id { get; set; }

    public long TaskId { get; set; }
    public long UserId { get; set; }
    public long AssignedByUserId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAt { get; set; }

    // Navigation Properties
    public virtual TaskItem Task { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual User AssignedByUser { get; set; } = null!;
}
