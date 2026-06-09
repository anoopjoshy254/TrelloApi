using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrelloApi.Helpers;
using TrelloApi.Services.Interfaces;

namespace TrelloApi.Controllers;

[ApiController]
[Route("api/activitylogs")]
[Authorize]
[Produces("application/json")]
public class ActivityLogController : ControllerBase
{
    private readonly IActivityLogService _activityLogService;

    public ActivityLogController(IActivityLogService activityLogService)
    {
        _activityLogService = activityLogService;
    }

    /// <summary>GET /api/activitylogs/task/{taskId} — Get activity log for a task.</summary>
    [HttpGet("task/{taskId:long}")]
    public async Task<IActionResult> GetTaskLogs(long taskId, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _activityLogService.GetTaskLogsAsync(taskId, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
