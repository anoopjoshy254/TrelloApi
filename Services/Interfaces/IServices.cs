using TrelloApi.DTOs.Auth;
using TrelloApi.DTOs.User;
using TrelloApi.DTOs.Team;
using TrelloApi.DTOs.Project;
using TrelloApi.DTOs.Task;
using TrelloApi.DTOs.Comment;
using TrelloApi.DTOs.Attachment;
using TrelloApi.DTOs.Notification;
using TrelloApi.Helpers;
using TrelloApi.Models;

namespace TrelloApi.Services.Interfaces;

// ─────────────────────────────────────────────────────────
// AUTH SERVICE
// Handles registration, login, token operations, password mgmt
// ─────────────────────────────────────────────────────────
public interface IAuthService
{
    Task<ApiResponse<JwtResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
    Task<ApiResponse<JwtResponseDto>> LoginAsync(LoginDto dto, string? ipAddress = null, CancellationToken ct = default);
    Task<ApiResponse> LogoutAsync(long userId, CancellationToken ct = default);
    Task<ApiResponse<JwtResponseDto>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<ApiResponse> ChangePasswordAsync(long userId, ChangePasswordDto dto, CancellationToken ct = default);
    Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct = default);
    Task<ApiResponse> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default);
}

// ─────────────────────────────────────────────────────────
// USER SERVICE
// ─────────────────────────────────────────────────────────
public interface IUserService
{
    Task<ApiResponse<PagedResult<UserResponseDto>>> GetUsersAsync(int page, int pageSize, string? search = null, CancellationToken ct = default);
    Task<ApiResponse<UserResponseDto>> GetUserByIdAsync(long id, CancellationToken ct = default);
    Task<ApiResponse<UserResponseDto>> UpdateUserAsync(long userId, UpdateUserDto dto, CancellationToken ct = default);
    Task<ApiResponse> DeleteUserAsync(long userId, long requestingUserId, CancellationToken ct = default);
}

// ─────────────────────────────────────────────────────────
// TEAM SERVICE
// ─────────────────────────────────────────────────────────
public interface ITeamService
{
    Task<ApiResponse<TeamResponseDto>> CreateTeamAsync(CreateTeamDto dto, long ownerId, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<TeamResponseDto>>> GetMyTeamsAsync(long userId, CancellationToken ct = default);
    Task<ApiResponse<TeamResponseDto>> GetTeamByIdAsync(long teamId, long requestingUserId, CancellationToken ct = default);
    Task<ApiResponse<TeamResponseDto>> UpdateTeamAsync(long teamId, UpdateTeamDto dto, long requestingUserId, CancellationToken ct = default);
    Task<ApiResponse> DeleteTeamAsync(long teamId, long requestingUserId, CancellationToken ct = default);
    Task<ApiResponse<TeamMemberResponseDto>> AddMemberAsync(long teamId, AddTeamMemberDto dto, long requestingUserId, CancellationToken ct = default);
    Task<ApiResponse> RemoveMemberAsync(long teamId, long userId, long requestingUserId, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<TeamMemberResponseDto>>> GetMembersAsync(long teamId, long requestingUserId, CancellationToken ct = default);
    Task<ApiResponse> InviteUserByEmailAsync(long teamId, string email, long requestingUserId, CancellationToken ct = default);
    Task<ApiResponse> AcceptInvitationAsync(string token, long userId, CancellationToken ct = default);
}

// ─────────────────────────────────────────────────────────
// PROJECT SERVICE
// ─────────────────────────────────────────────────────────
public interface IProjectService
{
    Task<ApiResponse<ProjectResponseDto>> CreateProjectAsync(CreateProjectDto dto, long ownerId, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<ProjectResponseDto>>> GetMyProjectsAsync(long userId, CancellationToken ct = default);
    Task<ApiResponse<ProjectResponseDto>> GetProjectByIdAsync(long projectId, long requestingUserId, CancellationToken ct = default);
    Task<ApiResponse<ProjectResponseDto>> UpdateProjectAsync(long projectId, UpdateProjectDto dto, long requestingUserId, CancellationToken ct = default);
    Task<ApiResponse<ProjectResponseDto>> UpdateProjectColumnsAsync(long projectId, string[] columns, long requestingUserId, CancellationToken ct = default);
    Task<ApiResponse> DeleteProjectAsync(long projectId, long requestingUserId, CancellationToken ct = default);
}

// ─────────────────────────────────────────────────────────
// TASK SERVICE
// ─────────────────────────────────────────────────────────
public interface ITaskService
{
    Task<ApiResponse<TaskResponseDto>> CreateTaskAsync(CreateTaskDto dto, long createdByUserId, CancellationToken ct = default);
    Task<ApiResponse<PagedResult<TaskResponseDto>>> GetTasksByProjectAsync(long projectId, long userId, int page, int pageSize, string? status = null, string? priority = null, CancellationToken ct = default);
    Task<ApiResponse<PagedResult<TaskResponseDto>>> GetMyAssignedTasksAsync(long userId, int page, int pageSize, CancellationToken ct = default);
    Task<ApiResponse<TaskResponseDto>> GetTaskByIdAsync(long taskId, long requestingUserId, CancellationToken ct = default);
    Task<ApiResponse<TaskResponseDto>> UpdateTaskAsync(long taskId, UpdateTaskDto dto, long requestingUserId, CancellationToken ct = default);
    Task<ApiResponse> DeleteTaskAsync(long taskId, long requestingUserId, CancellationToken ct = default);
    Task<ApiResponse<TaskResponseDto>> AssignUserAsync(long taskId, AssignTaskDto dto, long requestingUserId, CancellationToken ct = default);
    Task<ApiResponse<TaskResponseDto>> UnassignUserAsync(long taskId, long userId, long requestingUserId, CancellationToken ct = default);
    Task<ApiResponse<TaskResponseDto>> UpdateStatusAsync(long taskId, UpdateTaskStatusDto dto, long requestingUserId, CancellationToken ct = default);
    Task<ApiResponse<TaskResponseDto>> ToggleAssignmentCompletionAsync(long taskId, long userId, ToggleAssignmentCompletionDto dto, long requestingUserId, CancellationToken ct = default);
}

// ─────────────────────────────────────────────────────────
// COMMENT SERVICE
// ─────────────────────────────────────────────────────────
public interface ICommentService
{
    Task<ApiResponse<CommentResponseDto>> CreateCommentAsync(CreateCommentDto dto, long userId, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<CommentResponseDto>>> GetCommentsByTaskAsync(long taskId, long requestingUserId, CancellationToken ct = default);
    Task<ApiResponse<CommentResponseDto>> UpdateCommentAsync(long commentId, UpdateCommentDto dto, long requestingUserId, CancellationToken ct = default);
    Task<ApiResponse> DeleteCommentAsync(long commentId, long requestingUserId, CancellationToken ct = default);
}

// ─────────────────────────────────────────────────────────
// ATTACHMENT SERVICE
// ─────────────────────────────────────────────────────────
public interface IAttachmentService
{
    Task<ApiResponse<AttachmentResponseDto>> UploadAttachmentAsync(IFormFile file, long taskId, long uploadedByUserId, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<AttachmentResponseDto>>> GetAttachmentsByTaskAsync(long taskId, long requestingUserId, CancellationToken ct = default);
    Task<ApiResponse<AttachmentResponseDto>> GetAttachmentByIdAsync(long attachmentId, long requestingUserId, CancellationToken ct = default);
    Task<ApiResponse> DeleteAttachmentAsync(long attachmentId, long requestingUserId, CancellationToken ct = default);
}

// ─────────────────────────────────────────────────────────
// NOTIFICATION SERVICE
// ─────────────────────────────────────────────────────────
public interface INotificationService
{
    Task<ApiResponse<IEnumerable<NotificationResponseDto>>> GetMyNotificationsAsync(long userId, bool unreadOnly = false, CancellationToken ct = default);
    Task<ApiResponse> MarkAsReadAsync(long notificationId, long userId, CancellationToken ct = default);
    Task<ApiResponse> MarkAllAsReadAsync(long userId, CancellationToken ct = default);
    Task<ApiResponse> DeleteNotificationAsync(long notificationId, long userId, CancellationToken ct = default);
    Task CreateNotificationAsync(long recipientId, string type, string title, string? message = null, string? entityType = null, long? entityId = null, long? sentByUserId = null);
}

// ─────────────────────────────────────────────────────────
// ACTIVITY LOG SERVICE
// ─────────────────────────────────────────────────────────
public interface IActivityLogService
{
    Task LogAsync(long userId, string action, string? description = null, string? entityType = null, long? entityId = null, string? ipAddress = null, CancellationToken ct = default);
    Task<IEnumerable<ActivityLog>> GetUserLogsAsync(long userId, int limit = 50, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<ActivityLog>>> GetTaskLogsAsync(long taskId, long requestingUserId, CancellationToken ct = default);
}
