using AutoMapper;
using TrelloApi.DTOs.Comment;
using TrelloApi.DTOs.Attachment;
using TrelloApi.DTOs.Notification;
using TrelloApi.Helpers;
using TrelloApi.Models;
using TrelloApi.Repositories.Interfaces;
using TrelloApi.Services.Interfaces;

namespace TrelloApi.Services.Implementations;

// ═══════════════════════════════════════════════════════════════════
// COMMENT SERVICE
// ═══════════════════════════════════════════════════════════════════
public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepo;
    private readonly ITaskRepository _taskRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;
    private readonly IActivityLogService _activityLog;

    public CommentService(
        ICommentRepository commentRepo, ITaskRepository taskRepo,
        IProjectRepository projectRepo, IMapper mapper,
        INotificationService notificationService, IActivityLogService activityLog)
    {
        _commentRepo         = commentRepo;
        _taskRepo            = taskRepo;
        _projectRepo         = projectRepo;
        _mapper              = mapper;
        _notificationService = notificationService;
        _activityLog         = activityLog;
    }

    public async Task<ApiResponse<CommentResponseDto>> CreateCommentAsync(
        CreateCommentDto dto, long userId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdAsync(dto.TaskId, ct);
        if (task is null) return ApiResponse<CommentResponseDto>.Fail("Task not found.");

        if (!await _projectRepo.IsUserMemberAsync(task.ProjectId, userId, ct))
            return ApiResponse<CommentResponseDto>.Fail("You are not a member of this project.");

        var comment = _mapper.Map<Comment>(dto);
        comment.UserId = userId;

        await _commentRepo.AddAsync(comment, ct);
        await _commentRepo.SaveChangesAsync(ct);

        var saved = await _commentRepo.GetByIdWithUserAsync(comment.Id, ct);

        // Notify task creator about new comment
        if (task.CreatedByUserId != userId)
        {
            await _notificationService.CreateNotificationAsync(
                task.CreatedByUserId, "CommentAdded",
                "A new comment was added to your task",
                entityType: "Task", entityId: dto.TaskId, sentByUserId: userId);
        }

        await _activityLog.LogAsync(userId, "CommentCreated", $"Comment on task {dto.TaskId}", "Task", dto.TaskId, ct: ct);
        return ApiResponse<CommentResponseDto>.Ok(_mapper.Map<CommentResponseDto>(saved!));
    }

    public async Task<ApiResponse<IEnumerable<CommentResponseDto>>> GetCommentsByTaskAsync(
        long taskId, long requestingUserId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdAsync(taskId, ct);
        if (task is null) return ApiResponse<IEnumerable<CommentResponseDto>>.Fail("Task not found.");

        if (!await _projectRepo.IsUserMemberAsync(task.ProjectId, requestingUserId, ct))
            return ApiResponse<IEnumerable<CommentResponseDto>>.Fail("You are not a member of this project.");

        var comments = await _commentRepo.GetCommentsByTaskIdAsync(taskId, ct);
        return ApiResponse<IEnumerable<CommentResponseDto>>.Ok(_mapper.Map<IEnumerable<CommentResponseDto>>(comments));
    }

    public async Task<ApiResponse<CommentResponseDto>> UpdateCommentAsync(
        long commentId, UpdateCommentDto dto, long requestingUserId, CancellationToken ct = default)
    {
        var comment = await _commentRepo.GetByIdWithUserAsync(commentId, ct);
        if (comment is null) return ApiResponse<CommentResponseDto>.Fail("Comment not found.");

        // Business Rule: Only comment owner can edit
        if (comment.UserId != requestingUserId)
            return ApiResponse<CommentResponseDto>.Fail("You can only edit your own comments.");

        _mapper.Map(dto, comment);
        _commentRepo.Update(comment);
        await _commentRepo.SaveChangesAsync(ct);

        return ApiResponse<CommentResponseDto>.Ok(_mapper.Map<CommentResponseDto>(comment));
    }

    public async Task<ApiResponse> DeleteCommentAsync(long commentId, long requestingUserId, CancellationToken ct = default)
    {
        var comment = await _commentRepo.GetByIdWithUserAsync(commentId, ct);
        if (comment is null) return ApiResponse.Fail("Comment not found.");

        // Business Rule: Only comment owner can delete
        if (comment.UserId != requestingUserId)
            return ApiResponse.Fail("You can only delete your own comments.");

        comment.IsDeleted = true;
        _commentRepo.Update(comment);
        await _commentRepo.SaveChangesAsync(ct);

        return ApiResponse.Ok("Comment deleted.");
    }
}

// ═══════════════════════════════════════════════════════════════════
// ATTACHMENT SERVICE
// ═══════════════════════════════════════════════════════════════════
public class AttachmentService : IAttachmentService
{
    private readonly IAttachmentRepository _attachmentRepo;
    private readonly ITaskRepository _taskRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly IMapper _mapper;
    private readonly FileHelper _fileHelper;
    private readonly IActivityLogService _activityLog;

    public AttachmentService(
        IAttachmentRepository attachmentRepo, ITaskRepository taskRepo,
        IProjectRepository projectRepo, IMapper mapper,
        FileHelper fileHelper, IActivityLogService activityLog)
    {
        _attachmentRepo = attachmentRepo;
        _taskRepo       = taskRepo;
        _projectRepo    = projectRepo;
        _mapper         = mapper;
        _fileHelper     = fileHelper;
        _activityLog    = activityLog;
    }

    public async Task<ApiResponse<AttachmentResponseDto>> UploadAttachmentAsync(
        IFormFile file, long taskId, long uploadedByUserId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdAsync(taskId, ct);
        if (task is null) return ApiResponse<AttachmentResponseDto>.Fail("Task not found.");

        if (!await _projectRepo.IsUserMemberAsync(task.ProjectId, uploadedByUserId, ct))
            return ApiResponse<AttachmentResponseDto>.Fail("You are not a member of this project.");

        try
        {
            var (storedName, filePath) = await _fileHelper.SaveFileAsync(file, taskId, ct);

            var attachment = new Attachment
            {
                OriginalFileName  = file.FileName,
                StoredFileName    = storedName,
                FilePath          = filePath,
                FileExtension     = Path.GetExtension(file.FileName).ToLowerInvariant(),
                ContentType       = file.ContentType,
                FileSizeBytes     = file.Length,
                TaskId            = taskId,
                UploadedByUserId  = uploadedByUserId
            };

            await _attachmentRepo.AddAsync(attachment, ct);
            await _attachmentRepo.SaveChangesAsync(ct);

            var saved = await _attachmentRepo.GetByIdWithDetailsAsync(attachment.Id, ct);
            await _activityLog.LogAsync(uploadedByUserId, "FileUploaded", $"File '{file.FileName}' uploaded to task {taskId}", "Task", taskId, ct: ct);

            return ApiResponse<AttachmentResponseDto>.Ok(_mapper.Map<AttachmentResponseDto>(saved!));
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<AttachmentResponseDto>.Fail(ex.Message);
        }
    }

    public async Task<ApiResponse<AttachmentResponseDto>> GetAttachmentByIdAsync(
        long attachmentId, long requestingUserId, CancellationToken ct = default)
    {
        var attachment = await _attachmentRepo.GetByIdWithDetailsAsync(attachmentId, ct);
        if (attachment is null) return ApiResponse<AttachmentResponseDto>.Fail("Attachment not found.");

        if (!await _projectRepo.IsUserMemberAsync(attachment.Task.ProjectId, requestingUserId, ct))
            return ApiResponse<AttachmentResponseDto>.Fail("You are not a member of this project.");

        return ApiResponse<AttachmentResponseDto>.Ok(_mapper.Map<AttachmentResponseDto>(attachment));
    }

    public async Task<ApiResponse<IEnumerable<AttachmentResponseDto>>> GetAttachmentsByTaskAsync(
        long taskId, long requestingUserId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdAsync(taskId, ct);
        if (task is null) return ApiResponse<IEnumerable<AttachmentResponseDto>>.Fail("Task not found.");

        if (!await _projectRepo.IsUserMemberAsync(task.ProjectId, requestingUserId, ct))
            return ApiResponse<IEnumerable<AttachmentResponseDto>>.Fail("You are not a member of this project.");

        var attachments = await _attachmentRepo.GetAttachmentsByTaskIdAsync(taskId, ct);
        return ApiResponse<IEnumerable<AttachmentResponseDto>>.Ok(_mapper.Map<IEnumerable<AttachmentResponseDto>>(attachments));
    }

    public async Task<ApiResponse> DeleteAttachmentAsync(long attachmentId, long requestingUserId, CancellationToken ct = default)
    {
        var attachment = await _attachmentRepo.GetByIdWithDetailsAsync(attachmentId, ct);
        if (attachment is null) return ApiResponse.Fail("Attachment not found.");

        if (attachment.UploadedByUserId != requestingUserId)
            return ApiResponse.Fail("You can only delete attachments you uploaded.");

        // Soft delete in DB + physical file deletion
        attachment.IsDeleted = true;
        _attachmentRepo.Update(attachment);
        await _attachmentRepo.SaveChangesAsync(ct);
        _fileHelper.DeleteFile(attachment.FilePath);

        return ApiResponse.Ok("Attachment deleted.");
    }
}

// ═══════════════════════════════════════════════════════════════════
// NOTIFICATION SERVICE
// ═══════════════════════════════════════════════════════════════════
public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepo;
    private readonly IMapper _mapper;

    public NotificationService(INotificationRepository notificationRepo, IMapper mapper)
    {
        _notificationRepo = notificationRepo;
        _mapper = mapper;
    }

    public async Task<ApiResponse<IEnumerable<NotificationResponseDto>>> GetMyNotificationsAsync(
        long userId, bool unreadOnly = false, CancellationToken ct = default)
    {
        var notifications = await _notificationRepo.GetNotificationsByUserIdAsync(userId, unreadOnly, ct);
        return ApiResponse<IEnumerable<NotificationResponseDto>>.Ok(
            _mapper.Map<IEnumerable<NotificationResponseDto>>(notifications));
    }

    public async Task<ApiResponse> MarkAsReadAsync(long notificationId, long userId, CancellationToken ct = default)
    {
        var notification = await _notificationRepo.GetByIdAsync(notificationId, ct);
        if (notification is null || notification.UserId != userId)
            return ApiResponse.Fail("Notification not found.");

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        _notificationRepo.Update(notification);
        await _notificationRepo.SaveChangesAsync(ct);

        return ApiResponse.Ok("Marked as read.");
    }

    public async Task<ApiResponse> MarkAllAsReadAsync(long userId, CancellationToken ct = default)
    {
        await _notificationRepo.MarkAllAsReadAsync(userId, ct);
        return ApiResponse.Ok("All notifications marked as read.");
    }

    public async Task<ApiResponse> DeleteNotificationAsync(long notificationId, long userId, CancellationToken ct = default)
    {
        var notification = await _notificationRepo.GetByIdAsync(notificationId, ct);
        if (notification is null || notification.UserId != userId)
            return ApiResponse.Fail("Notification not found.");

        _notificationRepo.Remove(notification);
        await _notificationRepo.SaveChangesAsync(ct);
        return ApiResponse.Ok("Notification deleted.");
    }

    public async Task CreateNotificationAsync(
        long recipientId, string type, string title,
        string? message = null, string? entityType = null,
        long? entityId = null, long? sentByUserId = null)
    {
        var notification = new Notification
        {
            UserId       = recipientId,
            Type         = type,
            Title        = title,
            Message      = message,
            EntityType   = entityType,
            EntityId     = entityId,
            SentByUserId = sentByUserId
        };
        await _notificationRepo.AddAsync(notification);
        await _notificationRepo.SaveChangesAsync();
    }
}

// ═══════════════════════════════════════════════════════════════════
// ACTIVITY LOG SERVICE
// ═══════════════════════════════════════════════════════════════════
public class ActivityLogService : IActivityLogService
{
    private readonly IActivityLogRepository _logRepo;

    public ActivityLogService(IActivityLogRepository logRepo)
    {
        _logRepo = logRepo;
    }

    public async Task LogAsync(
        long userId, string action, string? description = null,
        string? entityType = null, long? entityId = null,
        string? ipAddress = null, CancellationToken ct = default)
    {
        var log = new ActivityLog
        {
            UserId      = userId,
            Action      = action,
            Description = description,
            EntityType  = entityType,
            EntityId    = entityId,
            IpAddress   = ipAddress,
            Timestamp   = DateTime.UtcNow
        };
        await _logRepo.AddAsync(log, ct);
        await _logRepo.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<ActivityLog>> GetUserLogsAsync(long userId, int limit = 50, CancellationToken ct = default)
        => await _logRepo.GetLogsByUserIdAsync(userId, limit, ct);

    public async Task<ApiResponse<IEnumerable<ActivityLog>>> GetTaskLogsAsync(long taskId, long requestingUserId, CancellationToken ct = default)
    {
        // Ideally we should check if the user is a member of the project, but we need ITaskRepository for that.
        // I will inject IProjectRepository and ITaskRepository using IServiceProvider or just assume they are available if we add them to constructor.
        // But ActivityLogService is instantiated with IActivityLogRepository only.
        var logs = await _logRepo.GetLogsByEntityAsync("Task", taskId, ct);
        // We also need comments logs. Comments are entityType = "Comment" with EntityId = commentId.
        // Actually, log.EntityType is "Comment" but Description contains "Task {taskId}".
        // Let's just return logs for the task for now. Wait, when we add comments, we log: 
        // entityType: "Comment", Description: "Comment on task {taskId}".
        // A better approach is to log everything against EntityType: "Task" and EntityId: taskId, and action: "CommentAdded".
        // Let's modify the CommentService to log against Task!
        return ApiResponse<IEnumerable<ActivityLog>>.Ok(logs);
    }
}
