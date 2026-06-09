using System.ComponentModel.DataAnnotations;

namespace TrelloApi.Models;

/// <summary>
/// Represents a file attached to a task. Supports images, PDFs, and documents.
/// Files are stored on the local file system; metadata lives in the database.
/// </summary>
public class Attachment
{
    public long Id { get; set; }

    [Required, MaxLength(500)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string StoredFileName { get; set; } = string.Empty; // GUID-based unique name on disk

    [Required, MaxLength(1000)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(10)]
    public string FileExtension { get; set; } = string.Empty;

    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public bool IsDeleted { get; set; } = false;

    public long TaskId { get; set; }
    public long UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual TaskItem Task { get; set; } = null!;
    public virtual User UploadedByUser { get; set; } = null!;
}
