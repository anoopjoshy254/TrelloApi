using System.ComponentModel.DataAnnotations;

namespace TrelloApi.Models;

/// <summary>
/// Represents a team that groups multiple users together to collaborate on projects.
/// </summary>
public class Team
{
    public long Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    public bool IsDeleted { get; set; } = false;

    public long OwnerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public virtual User Owner { get; set; } = null!;
    public virtual ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
}
