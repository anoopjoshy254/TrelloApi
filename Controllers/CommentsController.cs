using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrelloApi.DTOs.Comment;
using TrelloApi.Helpers;
using TrelloApi.Services.Interfaces;

namespace TrelloApi.Controllers;

[ApiController]
[Route("api/comments")]
[Authorize]
[Produces("application/json")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;
    public CommentsController(ICommentService commentService) => _commentService = commentService;

    /// <summary>POST /api/comments — Post a comment on a task.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateComment([FromBody] CreateCommentDto dto, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _commentService.CreateCommentAsync(dto, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>GET /api/comments/task/{taskId} — Get all comments for a task.</summary>
    [HttpGet("task/{taskId:long}")]
    public async Task<IActionResult> GetCommentsByTask(long taskId, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _commentService.GetCommentsByTaskAsync(taskId, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>PUT /api/comments/{id} — Edit a comment (owner only).</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateComment(long id, [FromBody] UpdateCommentDto dto, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _commentService.UpdateCommentAsync(id, dto, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>DELETE /api/comments/{id} — Delete a comment (owner only).</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteComment(long id, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _commentService.DeleteCommentAsync(id, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
