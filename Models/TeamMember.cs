using System.ComponentModel.DataAnnotations;

namespace TrelloApi.Models;

/// <summary>
/// Join table: represents a user's membership in a team with a specific role.
/// Many-to-Many between User and Team with extra payload (TeamRole, JoinedAt).
/// </summary>
public class TeamMember
{
    public long Id { get; set; }

    public long TeamId { get; set; }
    public long UserId { get; set; }

    [Required, MaxLength(50)]
    public string TeamRole { get; set; } = "Member"; // Admin | Member | Viewer

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Team Team { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
