using System.ComponentModel.DataAnnotations;

namespace TrelloApi.Models;

/// <summary>
/// Represents a task (card) within a project. Named TaskItem to avoid
/// conflict with System.Threading.Tasks.Task.
/// </summary>
public class TaskItem
{
    public long Id { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Todo"; // Todo | InProgress | Review | Done | Cancelled

    [MaxLength(20)]
    public string Priority { get; set; } = "Medium"; // Low | Medium | High | Critical

    public DateTime? DueDate { get; set; }
    public int? EstimatedHours { get; set; }
    public int? ActualHours { get; set; }
    public int Position { get; set; } = 0; // Ordering within the board column

    public bool IsDeleted { get; set; } = false;

    public long ProjectId { get; set; }
    public long CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation Properties
    public virtual Project Project { get; set; } = null!;
    public virtual User CreatedByUser { get; set; } = null!;
    public virtual ICollection<TaskAssignment> Assignments { get; set; } = new List<TaskAssignment>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public virtual ICollection<TaskLabel> TaskLabels { get; set; } = new List<TaskLabel>();
    public virtual ICollection<Checklist> Checklists { get; set; } = new List<Checklist>();
}
