using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrelloApi.Helpers;
using TrelloApi.Services.Interfaces;

namespace TrelloApi.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    public NotificationsController(INotificationService notificationService) => _notificationService = notificationService;

    /// <summary>GET /api/notifications — Get current user's notifications. Add ?unreadOnly=true for unread only.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications([FromQuery] bool unreadOnly = false, CancellationToken ct = default)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _notificationService.GetMyNotificationsAsync(userId, unreadOnly, ct);
        return Ok(result);
    }

    /// <summary>PUT /api/notifications/{id}/read — Mark a single notification as read.</summary>
    [HttpPut("{id:long}/read")]
    public async Task<IActionResult> MarkAsRead(long id, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _notificationService.MarkAsReadAsync(id, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>PUT /api/notifications/read-all — Mark ALL notifications as read.</summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _notificationService.MarkAllAsReadAsync(userId, ct);
        return Ok(result);
    }

    /// <summary>DELETE /api/notifications/{id} — Delete a notification.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteNotification(long id, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _notificationService.DeleteNotificationAsync(id, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
