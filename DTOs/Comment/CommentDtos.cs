namespace TrelloApi.DTOs.Comment;

/// <summary>
/// Request body for posting a new comment on a task.
/// </summary>
public class CreateCommentDto
{
    public long TaskId { get; set; }
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Request body for editing an existing comment (only owner may do this).
/// </summary>
public class UpdateCommentDto
{
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Response model for a comment — includes author information.
/// </summary>
public class CommentResponseDto
{
    public long Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsEdited { get; set; }
    public long TaskId { get; set; }
    public long UserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
