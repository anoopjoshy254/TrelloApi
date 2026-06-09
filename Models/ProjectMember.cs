using System.ComponentModel.DataAnnotations;

namespace TrelloApi.Models;

/// <summary>
/// Join table: represents a user's membership in a project with a specific role.
/// Many-to-Many between User and Project with extra payload.
/// </summary>
public class ProjectMember
{
    public long Id { get; set; }

    public long ProjectId { get; set; }
    public long UserId { get; set; }

    [Required, MaxLength(50)]
    public string ProjectRole { get; set; } = "Member"; // Owner | Admin | Member | Viewer

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Project Project { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
