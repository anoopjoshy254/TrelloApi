using TrelloApi.Models;

namespace TrelloApi.Repositories.Interfaces;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdWithRoleAsync(long id, CancellationToken ct = default);
    Task<User?> GetByResetTokenAsync(string token, CancellationToken ct = default);
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<(IEnumerable<User> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search = null, CancellationToken ct = default);
}

public interface ITeamRepository : IGenericRepository<Team>
{
    Task<Team?> GetByIdWithMembersAsync(long teamId, CancellationToken ct = default);
    Task<IEnumerable<Team>> GetTeamsByUserIdAsync(long userId, CancellationToken ct = default);
    Task<TeamMember?> GetMembershipAsync(long teamId, long userId, CancellationToken ct = default);
    Task<bool> IsUserMemberAsync(long teamId, long userId, CancellationToken ct = default);
    Task<bool> IsUserAdminAsync(long teamId, long userId, CancellationToken ct = default);
}

public interface IProjectRepository : IGenericRepository<Project>
{
    Task<Project?> GetByIdWithDetailsAsync(long projectId, CancellationToken ct = default);
    Task<IEnumerable<Project>> GetProjectsByUserIdAsync(long userId, CancellationToken ct = default);
    Task<IEnumerable<Project>> GetProjectsByTeamIdAsync(long teamId, CancellationToken ct = default);
    Task<ProjectMember?> GetMembershipAsync(long projectId, long userId, CancellationToken ct = default);
    Task<bool> IsUserMemberAsync(long projectId, long userId, CancellationToken ct = default);
}

public interface ITaskRepository : IGenericRepository<TaskItem>
{
    Task<TaskItem?> GetByIdWithDetailsAsync(long taskId, CancellationToken ct = default);
    Task<IEnumerable<TaskItem>> GetTasksByProjectIdAsync(long projectId, CancellationToken ct = default);
    Task<IEnumerable<TaskItem>> GetTasksByAssigneeIdAsync(long userId, CancellationToken ct = default);
    Task<(IEnumerable<TaskItem> Items, int TotalCount)> GetPagedAsync(long projectId, int page, int pageSize, string? status = null, string? priority = null, CancellationToken ct = default);
}

public interface ICommentRepository : IGenericRepository<Comment>
{
    Task<IEnumerable<Comment>> GetCommentsByTaskIdAsync(long taskId, CancellationToken ct = default);
    Task<Comment?> GetByIdWithUserAsync(long commentId, CancellationToken ct = default);
}

public interface IAttachmentRepository : IGenericRepository<Attachment>
{
    Task<IEnumerable<Attachment>> GetAttachmentsByTaskIdAsync(long taskId, CancellationToken ct = default);
    Task<Attachment?> GetByIdWithDetailsAsync(long attachmentId, CancellationToken ct = default);
}

public interface INotificationRepository : IGenericRepository<Notification>
{
    Task<IEnumerable<Notification>> GetNotificationsByUserIdAsync(long userId, bool unreadOnly = false, CancellationToken ct = default);
    Task MarkAllAsReadAsync(long userId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default);
}

public interface IActivityLogRepository : IGenericRepository<ActivityLog>
{
    Task<IEnumerable<ActivityLog>> GetLogsByUserIdAsync(long userId, int limit = 50, CancellationToken ct = default);
    Task<IEnumerable<ActivityLog>> GetLogsByEntityAsync(string entityType, long entityId, CancellationToken ct = default);
}
