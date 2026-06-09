using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrelloApi.DTOs.Task;
using TrelloApi.Helpers;
using TrelloApi.Services.Interfaces;

namespace TrelloApi.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    public TasksController(ITaskService taskService) => _taskService = taskService;

    /// <summary>POST /api/tasks — Create a new task within a project.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto dto, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _taskService.CreateTaskAsync(dto, userId, ct);
        return result.Success ? CreatedAtAction(nameof(GetTask), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    /// <summary>GET /api/tasks?projectId=1 — Get paginated tasks for a project (members only).</summary>
    [HttpGet]
    public async Task<IActionResult> GetTasks(
        [FromQuery] long projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        CancellationToken ct = default)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _taskService.GetTasksByProjectAsync(projectId, userId, page, pageSize, status, priority, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>GET /api/tasks/my-tasks — Get tasks assigned to the current user.</summary>
    [HttpGet("my-tasks")]
    public async Task<IActionResult> GetMyTasks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _taskService.GetMyAssignedTasksAsync(userId, page, pageSize, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>GET /api/tasks/{id} — Get task by ID.</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetTask(long id, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _taskService.GetTaskByIdAsync(id, userId, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>PUT /api/tasks/{id} — Update task details (project member; completed tasks locked).</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateTask(long id, [FromBody] UpdateTaskDto dto, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _taskService.UpdateTaskAsync(id, dto, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>DELETE /api/tasks/{id} — Delete task (creator or project admin).</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteTask(long id, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _taskService.DeleteTaskAsync(id, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>PUT /api/tasks/{id}/assign — Assign user to task (project members only).</summary>
    [HttpPut("{id:long}/assign")]
    public async Task<IActionResult> AssignUser(long id, [FromBody] AssignTaskDto dto, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _taskService.AssignUserAsync(id, dto, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>DELETE /api/tasks/{id}/assign/{assignedUserId} — Unassign user from task.</summary>
    [HttpDelete("{id:long}/assign/{assignedUserId:long}")]
    public async Task<IActionResult> UnassignUser(long id, long assignedUserId, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _taskService.UnassignUserAsync(id, assignedUserId, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>PUT /api/tasks/{id}/status — Update task status (assigned user or project admin only).</summary>
    [HttpPut("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateTaskStatusDto dto, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _taskService.UpdateStatusAsync(id, dto, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>PUT /api/tasks/{id}/assignments/complete — Toggle completion for the current user's assignment.</summary>
    [HttpPut("{id:long}/assignments/complete")]
    public async Task<IActionResult> ToggleAssignmentCompletion(long id, [FromBody] ToggleAssignmentCompletionDto dto, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _taskService.ToggleAssignmentCompletionAsync(id, userId, dto, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
