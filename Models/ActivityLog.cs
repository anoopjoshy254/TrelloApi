using System.ComponentModel.DataAnnotations;

namespace TrelloApi.Models;

/// <summary>
/// Immutable audit trail for all significant user actions in the system.
/// </summary>
public class ActivityLog
{
    public long Id { get; set; }

    [Required, MaxLength(100)]
    public string Action { get; set; } = string.Empty;
    // Login | Logout | TeamCreated | ProjectCreated | ProjectUpdated
    // TaskCreated | TaskAssigned | CommentCreated | FileUploaded | etc.

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? EntityType { get; set; } // "Task" | "Project" | "Team" | "Comment"

    public long? EntityId { get; set; }

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    [MaxLength(300)]
    public string? UserAgent { get; set; }

    public long UserId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual User User { get; set; } = null!;
}
