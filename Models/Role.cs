using System.ComponentModel.DataAnnotations;

namespace TrelloApi.Models;

/// <summary>
/// Represents a user role (e.g., Admin, Member, Viewer).
/// Controls authorization within the system.
/// </summary>
public class Role
{
    public long Id { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
