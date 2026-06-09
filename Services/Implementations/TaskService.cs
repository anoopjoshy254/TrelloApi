using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using TrelloApi.DTOs.Task;
using TrelloApi.Helpers;
using TrelloApi.Hubs;
using TrelloApi.Models;
using TrelloApi.Repositories.Interfaces;
using TrelloApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace TrelloApi.Services.Implementations;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly IUserRepository _userRepo;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;
    private readonly IActivityLogService _activityLog;
    private readonly Data.ApplicationDbContext _context;
    private readonly IHubContext<BoardHub> _hubContext;

    public TaskService(
        ITaskRepository taskRepo, IProjectRepository projectRepo,
        IUserRepository userRepo, IMapper mapper,
        INotificationService notificationService, IActivityLogService activityLog,
        Data.ApplicationDbContext context, IHubContext<BoardHub> hubContext)
    {
        _taskRepo            = taskRepo;
        _projectRepo         = projectRepo;
        _userRepo            = userRepo;
        _mapper              = mapper;
        _notificationService = notificationService;
        _activityLog         = activityLog;
        _context             = context;
        _hubContext          = hubContext;
    }

    public async Task<ApiResponse<TaskResponseDto>> CreateTaskAsync(
        CreateTaskDto dto, long createdByUserId, CancellationToken ct = default)
    {
        // Business Rule: User must be a project member
        if (!await _projectRepo.IsUserMemberAsync(dto.ProjectId, createdByUserId, ct))
            return ApiResponse<TaskResponseDto>.Fail("You are not a member of this project.");

        var task = _mapper.Map<TaskItem>(dto);
        task.CreatedByUserId = createdByUserId;

        await _taskRepo.AddAsync(task, ct);
        await _taskRepo.SaveChangesAsync(ct);

        var created = await _taskRepo.GetByIdWithDetailsAsync(task.Id, ct);
        await _activityLog.LogAsync(createdByUserId, "TaskCreated", $"Task '{task.Title}' created", "Task", task.Id, ct: ct);

        var responseDto = _mapper.Map<TaskResponseDto>(created!);
        await _hubContext.Clients.Group(task.ProjectId.ToString()).SendAsync("TaskCreated", responseDto, ct);

        return ApiResponse<TaskResponseDto>.Ok(responseDto, "Task created.");
    }

    public async Task<ApiResponse<PagedResult<TaskResponseDto>>> GetTasksByProjectAsync(
        long projectId, long userId, int page, int pageSize,
        string? status = null, string? priority = null, CancellationToken ct = default)
    {
        if (!await _projectRepo.IsUserMemberAsync(projectId, userId, ct))
            return ApiResponse<PagedResult<TaskResponseDto>>.Fail("You are not a member of this project.");

        var (items, total) = await _taskRepo.GetPagedAsync(projectId, page, pageSize, status, priority, ct);
        var result = new PagedResult<TaskResponseDto>
        {
            Items      = _mapper.Map<List<TaskResponseDto>>(items),
            TotalCount = total,
            PageNumber = page,
            PageSize   = pageSize
        };
        return ApiResponse<PagedResult<TaskResponseDto>>.Ok(result);
    }

    public async Task<ApiResponse<PagedResult<TaskResponseDto>>> GetMyAssignedTasksAsync(
        long userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Tasks
            .Include(t => t.Assignments)
            .Include(t => t.Project)
            .Where(t => !t.IsDeleted && t.Assignments.Any(a => a.UserId == userId));

        var totalCount = await query.CountAsync(ct);

        var tasks = await query
            .OrderBy(t => t.DueDate.HasValue ? 0 : 1)
            .ThenBy(t => t.DueDate)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var result = new PagedResult<TaskResponseDto>
        {
            Items = _mapper.Map<List<TaskResponseDto>>(tasks),
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
        return ApiResponse<PagedResult<TaskResponseDto>>.Ok(result);
    }

    public async Task<ApiResponse<TaskResponseDto>> GetTaskByIdAsync(long taskId, long requestingUserId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdWithDetailsAsync(taskId, ct);
        if (task is null) return ApiResponse<TaskResponseDto>.Fail("Task not found.");

        if (!await _projectRepo.IsUserMemberAsync(task.ProjectId, requestingUserId, ct))
            return ApiResponse<TaskResponseDto>.Fail("You do not have access to this task.");

        return ApiResponse<TaskResponseDto>.Ok(_mapper.Map<TaskResponseDto>(task));
    }

    public async Task<ApiResponse<TaskResponseDto>> UpdateTaskAsync(
        long taskId, UpdateTaskDto dto, long requestingUserId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdWithDetailsAsync(taskId, ct);
        if (task is null) return ApiResponse<TaskResponseDto>.Fail("Task not found.");

        // Business Rule: Completed tasks cannot be edited
        if (task.Status == "Done")
            return ApiResponse<TaskResponseDto>.Fail("Completed tasks cannot be edited.");

        if (!await _projectRepo.IsUserMemberAsync(task.ProjectId, requestingUserId, ct))
            return ApiResponse<TaskResponseDto>.Fail("You are not a member of this project.");

        _mapper.Map(dto, task);
        _taskRepo.Update(task);
        await _taskRepo.SaveChangesAsync(ct);

        await _activityLog.LogAsync(requestingUserId, "TaskUpdated", $"Task {taskId} updated", "Task", taskId, ct: ct);
        
        var responseDto = _mapper.Map<TaskResponseDto>(task);
        await _hubContext.Clients.Group(task.ProjectId.ToString()).SendAsync("TaskUpdated", responseDto, ct);
        
        return ApiResponse<TaskResponseDto>.Ok(responseDto);
    }

    public async Task<ApiResponse> DeleteTaskAsync(long taskId, long requestingUserId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdAsync(taskId, ct);
        if (task is null) return ApiResponse.Fail("Task not found.");

        if (task.CreatedByUserId != requestingUserId)
        {
            var membership = await _projectRepo.GetMembershipAsync(task.ProjectId, requestingUserId, ct);
            if (membership is null || membership.ProjectRole == "Member")
                return ApiResponse.Fail("You do not have permission to delete this task.");
        }

        task.IsDeleted = true;
        _taskRepo.Update(task);
        await _taskRepo.SaveChangesAsync(ct);

        await _activityLog.LogAsync(requestingUserId, "TaskDeleted", $"Task {taskId} deleted", "Task", taskId, ct: ct);
        
        await _hubContext.Clients.Group(task.ProjectId.ToString()).SendAsync("TaskDeleted", taskId, ct);
        
        return ApiResponse.Ok("Task deleted.");
    }

    public async Task<ApiResponse<TaskResponseDto>> AssignUserAsync(
        long taskId, AssignTaskDto dto, long requestingUserId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdWithDetailsAsync(taskId, ct);
        if (task is null) return ApiResponse<TaskResponseDto>.Fail("Task not found.");

        // Business Rule: Only project members can be assigned
        if (!await _projectRepo.IsUserMemberAsync(task.ProjectId, dto.UserId, ct))
            return ApiResponse<TaskResponseDto>.Fail("Assigned user is not a project member.");

        // Business Rule: User cannot be assigned twice
        if (task.Assignments.Any(a => a.UserId == dto.UserId))
            return ApiResponse<TaskResponseDto>.Fail("User is already assigned to this task.");

        var assignment = new TaskAssignment
        {
            TaskId = taskId, UserId = dto.UserId, AssignedByUserId = requestingUserId
        };
        await _context.TaskAssignments.AddAsync(assignment, ct);
        await _context.SaveChangesAsync(ct);

        // Send notification to assigned user
        await _notificationService.CreateNotificationAsync(
            dto.UserId, "TaskAssigned",
            $"You've been assigned to task: {task.Title}",
            entityType: "Task", entityId: taskId, sentByUserId: requestingUserId);

        await _activityLog.LogAsync(requestingUserId, "TaskAssigned", $"User {dto.UserId} assigned to task {taskId}", "Task", taskId, ct: ct);

        var updated = await _taskRepo.GetByIdWithDetailsAsync(taskId, ct);
        return ApiResponse<TaskResponseDto>.Ok(_mapper.Map<TaskResponseDto>(updated!));
    }

    public async Task<ApiResponse<TaskResponseDto>> UnassignUserAsync(
        long taskId, long userId, long requestingUserId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdWithDetailsAsync(taskId, ct);
        if (task is null) return ApiResponse<TaskResponseDto>.Fail("Task not found.");

        var assignment = task.Assignments.FirstOrDefault(a => a.UserId == userId);
        if (assignment is null)
            return ApiResponse<TaskResponseDto>.Fail("User is not assigned to this task.");

        _context.TaskAssignments.Remove(assignment);
        await _context.SaveChangesAsync(ct);

        await _activityLog.LogAsync(requestingUserId, "TaskUnassigned", $"User {userId} unassigned from task {taskId}", "Task", taskId, ct: ct);

        var updated = await _taskRepo.GetByIdWithDetailsAsync(taskId, ct);
        return ApiResponse<TaskResponseDto>.Ok(_mapper.Map<TaskResponseDto>(updated!));
    }

    public async Task<ApiResponse<TaskResponseDto>> UpdateStatusAsync(
        long taskId, UpdateTaskStatusDto dto, long requestingUserId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdWithDetailsAsync(taskId, ct);
        if (task is null) return ApiResponse<TaskResponseDto>.Fail("Task not found.");

        // Business Rule: Only assigned users can update task status
        var isAssigned = task.Assignments.Any(a => a.UserId == requestingUserId);
        var membership = await _projectRepo.GetMembershipAsync(task.ProjectId, requestingUserId, ct);
        if (!isAssigned && membership?.ProjectRole == "Member")
            return ApiResponse<TaskResponseDto>.Fail("Only assigned users or project admins can update task status.");

        var project = await _projectRepo.GetByIdAsync(task.ProjectId, ct);
        if (project == null || !project.Columns.Contains(dto.Status))
            return ApiResponse<TaskResponseDto>.Fail($"Invalid status. Valid values: {string.Join(", ", project?.Columns ?? Array.Empty<string>())}");

        task.Status = dto.Status;
        if (dto.Position.HasValue)
        {
            task.Position = dto.Position.Value;
        }
        task.UpdatedAt = DateTime.UtcNow;
        if (dto.Status == "Done") task.CompletedAt = DateTime.UtcNow;

        _taskRepo.Update(task);
        await _taskRepo.SaveChangesAsync(ct);

        if (dto.Status == "Done")
        {
            await _notificationService.CreateNotificationAsync(
                task.CreatedByUserId, "TaskCompleted",
                $"Task '{task.Title}' has been marked as Done",
                entityType: "Task", entityId: taskId, sentByUserId: requestingUserId);
        }

        var responseDto = _mapper.Map<TaskResponseDto>(task);
        await _hubContext.Clients.Group(task.ProjectId.ToString()).SendAsync("TaskMoved", responseDto, ct);

        return ApiResponse<TaskResponseDto>.Ok(responseDto);
    }

    public async Task<ApiResponse<TaskResponseDto>> ToggleAssignmentCompletionAsync(
        long taskId, long userId, ToggleAssignmentCompletionDto dto, long requestingUserId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdWithDetailsAsync(taskId, ct);
        if (task is null) return ApiResponse<TaskResponseDto>.Fail("Task not found.");

        var assignment = task.Assignments.FirstOrDefault(a => a.UserId == userId);
        if (assignment is null) return ApiResponse<TaskResponseDto>.Fail("User is not assigned to this task.");

        // Only the assigned user can complete their own assignment
        if (userId != requestingUserId) return ApiResponse<TaskResponseDto>.Fail("You can only complete your own assignment.");

        assignment.IsCompleted = dto.IsCompleted;
        assignment.CompletedAt = dto.IsCompleted ? DateTime.UtcNow : null;

        await _context.SaveChangesAsync(ct);

        await _activityLog.LogAsync(requestingUserId, "AssignmentToggled", $"User {userId} marked their assignment as {(dto.IsCompleted ? "Completed" : "Incomplete")}", "Task", taskId, ct: ct);

        var updatedTask = await _taskRepo.GetByIdWithDetailsAsync(taskId, ct);
        var responseDto = _mapper.Map<TaskResponseDto>(updatedTask!);

        if (_hubContext != null)
        {
            await _hubContext.Clients.Group(task.ProjectId.ToString()).SendAsync("TaskUpdated", responseDto, ct);
        }

        return ApiResponse<TaskResponseDto>.Ok(responseDto);
    }
}
