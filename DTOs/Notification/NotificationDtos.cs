namespace TrelloApi.DTOs.Notification;

/// <summary>
/// Response model for a user notification.
/// </summary>
public class NotificationResponseDto
{
    public long Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public bool IsRead { get; set; }
    public string? EntityType { get; set; }
    public long? EntityId { get; set; }
    public long UserId { get; set; }
    public long? SentByUserId { get; set; }
    public string? SentByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
