using System.ComponentModel.DataAnnotations;

namespace TrelloApi.Models;

/// <summary>
/// Represents an in-app notification sent to a user for system events.
/// </summary>
public class Notification
{
    public long Id { get; set; }

    [Required, MaxLength(100)]
    public string Type { get; set; } = string.Empty;
    // TaskAssigned | TaskCompleted | ProjectUpdated | CommentAdded | TeamInvitation

    [Required, MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Message { get; set; }

    public bool IsRead { get; set; } = false;

    [MaxLength(50)]
    public string? EntityType { get; set; } // "Task" | "Project" | "Team" | "Comment"

    public long? EntityId { get; set; }

    public long UserId { get; set; } // Recipient
    public long? SentByUserId { get; set; } // Sender (nullable for system notifications)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }

    // Navigation Properties
    public virtual User User { get; set; } = null!;
}
