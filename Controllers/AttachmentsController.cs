using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrelloApi.Helpers;
using TrelloApi.Services.Interfaces;

namespace TrelloApi.Controllers;

[ApiController]
[Route("api/attachments")]
[Authorize]
[Produces("application/json")]
public class AttachmentsController : ControllerBase
{
    private readonly IAttachmentService _attachmentService;
    public AttachmentsController(IAttachmentService attachmentService) => _attachmentService = attachmentService;

    /// <summary>
    /// POST /api/attachments/upload — Upload a file to a task.
    /// Accepts multipart/form-data with fields: file (IFormFile) + taskId.
    /// Max file size: 10 MB. Allowed: .jpg .png .pdf .doc .docx .txt .zip
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] long taskId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse.Fail("No file was uploaded."));

        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _attachmentService.UploadAttachmentAsync(file, taskId, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>GET /api/attachments/task/{taskId} — Get all attachments for a task.</summary>
    [HttpGet("task/{taskId:long}")]
    public async Task<IActionResult> GetAttachmentsByTask(long taskId, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _attachmentService.GetAttachmentsByTaskAsync(taskId, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>GET /api/attachments/{id} — Get attachment metadata.</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetAttachment(long id, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _attachmentService.GetAttachmentByIdAsync(id, userId, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>GET /api/attachments/{id}/download — Download the physical file.</summary>
    [HttpGet("{id:long}/download")]
    public async Task<IActionResult> Download(long id, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _attachmentService.GetAttachmentByIdAsync(id, userId, ct);
        if (!result.Success) return NotFound(result);

        var attachment = result.Data!;
        // Reconstruct absolute path from stored info
        if (!System.IO.File.Exists(attachment.DownloadUrl.Replace($"/api/attachments/{id}/download", "")))
        {
            // Serve from stored FilePath via AttachmentService
            // In production you'd fetch the full filePath from the entity
            return NotFound(ApiResponse.Fail("Physical file not found on server."));
        }

        return PhysicalFile(attachment.DownloadUrl, attachment.ContentType, attachment.OriginalFileName);
    }

    /// <summary>DELETE /api/attachments/{id} — Delete attachment (uploader only).</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteAttachment(long id, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _attachmentService.DeleteAttachmentAsync(id, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
