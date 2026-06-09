namespace TrelloApi.DTOs.Attachment;

/// <summary>
/// Metadata sent alongside an IFormFile upload.
/// </summary>
public class UploadAttachmentDto
{
    public long TaskId { get; set; }
    // IFormFile is received separately via [FromForm] in the controller
}

/// <summary>
/// Response model for an uploaded attachment. Exposes safe metadata only.
/// </summary>
public class AttachmentResponseDto
{
    public long Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileSizeFormatted => FormatBytes(FileSizeBytes);
    public long TaskId { get; set; }
    public long UploadedByUserId { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string DownloadUrl { get; set; } = string.Empty; // e.g. /api/attachments/{id}/download

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F1} MB";
        if (bytes >= 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes} B";
    }
}
