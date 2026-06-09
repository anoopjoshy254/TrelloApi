using Microsoft.EntityFrameworkCore;
using TrelloApi.Data;
using TrelloApi.Models;
using TrelloApi.Repositories.Interfaces;

namespace TrelloApi.Repositories.Implementations;

// ═══════════════════════════════════════════════════════════════════
// USER REPOSITORY
// ═══════════════════════════════════════════════════════════════════
public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _dbSet.Include(u => u.Role)
                       .FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<User?> GetByIdWithRoleAsync(long id, CancellationToken ct = default)
        => await _dbSet.Include(u => u.Role)
                       .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByResetTokenAsync(string token, CancellationToken ct = default)
        => await _dbSet.IgnoreQueryFilters()
                       .FirstOrDefaultAsync(u => u.PasswordResetToken == token, ct);

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
        => await _dbSet.Include(u => u.Role)
                       .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, ct);

    public async Task<(IEnumerable<User> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search = null, CancellationToken ct = default)
    {
        var query = _dbSet.Include(u => u.Role).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u =>
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search) ||
                u.Email.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(u => u.LastName)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync(ct);
        return (items, total);
    }
}

// ═══════════════════════════════════════════════════════════════════
// TEAM REPOSITORY
// ═══════════════════════════════════════════════════════════════════
public class TeamRepository : GenericRepository<Team>, ITeamRepository
{
    public TeamRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Team?> GetByIdWithMembersAsync(long teamId, CancellationToken ct = default)
        => await _dbSet
            .Include(t => t.Owner)
            .Include(t => t.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == teamId, ct);

    public async Task<IEnumerable<Team>> GetTeamsByUserIdAsync(long userId, CancellationToken ct = default)
        => await _dbSet
            .Include(t => t.Owner)
            .Include(t => t.Members)
            .Where(t => t.OwnerId == userId || t.Members.Any(m => m.UserId == userId))
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

    public async Task<TeamMember?> GetMembershipAsync(long teamId, long userId, CancellationToken ct = default)
        => await _context.TeamMembers
            .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId, ct);

    public async Task<bool> IsUserMemberAsync(long teamId, long userId, CancellationToken ct = default)
        => await _context.TeamMembers.AnyAsync(tm => tm.TeamId == teamId && tm.UserId == userId, ct);

    public async Task<bool> IsUserAdminAsync(long teamId, long userId, CancellationToken ct = default)
        => await _context.TeamMembers.AnyAsync(tm =>
            tm.TeamId == teamId && tm.UserId == userId && tm.TeamRole == "Admin", ct);
}

// ═══════════════════════════════════════════════════════════════════
// PROJECT REPOSITORY
// ═══════════════════════════════════════════════════════════════════
public class ProjectRepository : GenericRepository<Project>, IProjectRepository
{
    public ProjectRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Project?> GetByIdWithDetailsAsync(long projectId, CancellationToken ct = default)
        => await _dbSet
            .Include(p => p.Owner)
            .Include(p => p.Team)
            .Include(p => p.Members).ThenInclude(m => m.User)
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct);

    public async Task<IEnumerable<Project>> GetProjectsByUserIdAsync(long userId, CancellationToken ct = default)
        => await _dbSet
            .Include(p => p.Owner)
            .Include(p => p.Team)
            .Include(p => p.Members)
            .Include(p => p.Tasks)
            .Where(p => p.OwnerId == userId || p.Members.Any(m => m.UserId == userId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<Project>> GetProjectsByTeamIdAsync(long teamId, CancellationToken ct = default)
        => await _dbSet
            .Include(p => p.Owner)
            .Where(p => p.TeamId == teamId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task<ProjectMember?> GetMembershipAsync(long projectId, long userId, CancellationToken ct = default)
        => await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId, ct);

    public async Task<bool> IsUserMemberAsync(long projectId, long userId, CancellationToken ct = default)
    {
        var isDirectMember = await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId, ct);
        if (isDirectMember) return true;

        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project != null && project.TeamId.HasValue)
        {
            return await _context.TeamMembers.AnyAsync(tm => tm.TeamId == project.TeamId.Value && tm.UserId == userId, ct);
        }

        return false;
    }
}

// ═══════════════════════════════════════════════════════════════════
// TASK REPOSITORY
// ═══════════════════════════════════════════════════════════════════
public class TaskRepository : GenericRepository<TaskItem>, ITaskRepository
{
    public TaskRepository(ApplicationDbContext context) : base(context) { }

    public async Task<TaskItem?> GetByIdWithDetailsAsync(long taskId, CancellationToken ct = default)
        => await _dbSet
            .Include(t => t.Project)
            .Include(t => t.CreatedByUser)
            .Include(t => t.Assignments).ThenInclude(a => a.User)
            .Include(t => t.Comments).ThenInclude(c => c.User)
            .Include(t => t.Attachments).ThenInclude(a => a.UploadedByUser)
            .Include(t => t.TaskLabels).ThenInclude(tl => tl.Label)
            .Include(t => t.Checklists).ThenInclude(c => c.Items)
            .FirstOrDefaultAsync(t => t.Id == taskId, ct);

    public async Task<IEnumerable<TaskItem>> GetTasksByProjectIdAsync(long projectId, CancellationToken ct = default)
        => await _dbSet
            .Include(t => t.CreatedByUser)
            .Include(t => t.Assignments).ThenInclude(a => a.User)
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.Position)
            .ToListAsync(ct);

    public async Task<IEnumerable<TaskItem>> GetTasksByAssigneeIdAsync(long userId, CancellationToken ct = default)
        => await _dbSet
            .Include(t => t.Project)
            .Include(t => t.Assignments)
            .Where(t => t.Assignments.Any(a => a.UserId == userId))
            .OrderBy(t => t.DueDate)
            .ToListAsync(ct);

    public async Task<(IEnumerable<TaskItem> Items, int TotalCount)> GetPagedAsync(
        long projectId, int page, int pageSize,
        string? status = null, string? priority = null,
        CancellationToken ct = default)
    {
        var query = _dbSet
            .Include(t => t.Assignments).ThenInclude(a => a.User)
            .Where(t => t.ProjectId == projectId).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);
        if (!string.IsNullOrWhiteSpace(priority))
            query = query.Where(t => t.Priority == priority);

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(t => t.Position)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync(ct);
        return (items, total);
    }
}

// ═══════════════════════════════════════════════════════════════════
// COMMENT REPOSITORY
// ═══════════════════════════════════════════════════════════════════
public class CommentRepository : GenericRepository<Comment>, ICommentRepository
{
    public CommentRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Comment>> GetCommentsByTaskIdAsync(long taskId, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.User)
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<Comment?> GetByIdWithUserAsync(long commentId, CancellationToken ct = default)
        => await _dbSet.Include(c => c.User)
                       .FirstOrDefaultAsync(c => c.Id == commentId, ct);
}

// ═══════════════════════════════════════════════════════════════════
// ATTACHMENT REPOSITORY
// ═══════════════════════════════════════════════════════════════════
public class AttachmentRepository : GenericRepository<Attachment>, IAttachmentRepository
{
    public AttachmentRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Attachment>> GetAttachmentsByTaskIdAsync(long taskId, CancellationToken ct = default)
        => await _dbSet
            .Include(a => a.UploadedByUser)
            .Where(a => a.TaskId == taskId)
            .OrderByDescending(a => a.UploadedAt)
            .ToListAsync(ct);

    public async Task<Attachment?> GetByIdWithDetailsAsync(long attachmentId, CancellationToken ct = default)
        => await _dbSet
            .Include(a => a.UploadedByUser)
            .Include(a => a.Task)
            .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);
}

// ═══════════════════════════════════════════════════════════════════
// NOTIFICATION REPOSITORY
// ═══════════════════════════════════════════════════════════════════
public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
{
    public NotificationRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Notification>> GetNotificationsByUserIdAsync(
        long userId, bool unreadOnly = false, CancellationToken ct = default)
    {
        var query = _dbSet.Where(n => n.UserId == userId);
        if (unreadOnly) query = query.Where(n => !n.IsRead);
        return await query.OrderByDescending(n => n.CreatedAt).Take(50).ToListAsync(ct);
    }

    public async Task MarkAllAsReadAsync(long userId, CancellationToken ct = default)
    {
        var unread = await _dbSet.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(ct);
        unread.ForEach(n => { n.IsRead = true; n.ReadAt = DateTime.UtcNow; });
        await _context.SaveChangesAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default)
        => await _dbSet.CountAsync(n => n.UserId == userId && !n.IsRead, ct);
}

// ═══════════════════════════════════════════════════════════════════
// ACTIVITY LOG REPOSITORY
// ═══════════════════════════════════════════════════════════════════
public class ActivityLogRepository : GenericRepository<ActivityLog>, IActivityLogRepository
{
    public ActivityLogRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<ActivityLog>> GetLogsByUserIdAsync(
        long userId, int limit = 50, CancellationToken ct = default)
        => await _dbSet
            .Where(al => al.UserId == userId)
            .OrderByDescending(al => al.Timestamp)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<IEnumerable<ActivityLog>> GetLogsByEntityAsync(
        string entityType, long entityId, CancellationToken ct = default)
        => await _dbSet
            .Where(al => al.EntityType == entityType && al.EntityId == entityId)
            .OrderByDescending(al => al.Timestamp)
            .ToListAsync(ct);
}
