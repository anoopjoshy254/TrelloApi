using System.ComponentModel.DataAnnotations;

namespace TrelloApi.Models;

/// <summary>
/// Represents an invitation for a user to join a team/workspace via email.
/// </summary>
public class TeamInvitation
{
    public long Id { get; set; }

    public long TeamId { get; set; }

    [Required, MaxLength(256), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Token { get; set; } = Guid.NewGuid().ToString();

    public long InvitedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);

    public bool IsAccepted { get; set; } = false;

    // Navigation Properties
    public virtual Team Team { get; set; } = null!;
    public virtual User InvitedByUser { get; set; } = null!;
}
