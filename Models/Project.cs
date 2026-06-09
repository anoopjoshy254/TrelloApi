using System.ComponentModel.DataAnnotations;

namespace TrelloApi.Models;

/// <summary>
/// Represents a project belonging to a team. Projects contain tasks.
/// </summary>
public class Project
{
    public long Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Active"; // Active | Archived | Completed

    [MaxLength(20)]
    public string? Color { get; set; }

    public DateTime? DueDate { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Stores the ordered list of column names as a JSON string
    public string ColumnsJson { get; set; } = "[\"Todo\", \"InProgress\", \"Review\", \"Done\"]";

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string[] Columns 
    { 
        get => string.IsNullOrWhiteSpace(ColumnsJson) ? new[] { "Todo", "InProgress", "Review", "Done" } : System.Text.Json.JsonSerializer.Deserialize<string[]>(ColumnsJson) ?? new[] { "Todo", "InProgress", "Review", "Done" }; 
        set => ColumnsJson = System.Text.Json.JsonSerializer.Serialize(value); 
    }

    public long OwnerId { get; set; }
    public long? TeamId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public virtual User Owner { get; set; } = null!;
    public virtual Team? Team { get; set; }
    public virtual ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public virtual ICollection<Label> Labels { get; set; } = new List<Label>();
}
