using System.ComponentModel.DataAnnotations;

namespace TrelloApi.Models;

/// <summary>
/// Represents a comment posted by a user on a task.
/// Supports soft delete so task history is preserved.
/// </summary>
public class Comment
{
    public long Id { get; set; }

    [Required, MaxLength(5000)]
    public string Content { get; set; } = string.Empty;

    public bool IsEdited { get; set; } = false;
    public bool IsDeleted { get; set; } = false;

    public long TaskId { get; set; }
    public long UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public virtual TaskItem Task { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
