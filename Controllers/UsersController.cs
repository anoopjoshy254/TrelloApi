using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrelloApi.DTOs.User;
using TrelloApi.Helpers;
using TrelloApi.Services.Interfaces;

namespace TrelloApi.Controllers;

/// <summary>
/// Users controller — manages user profiles.
/// All endpoints require authentication. Deletion requires Admin role.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    public UsersController(IUserService userService) => _userService = userService;

    /// <summary>GET /api/users — Paginated list of all users.</summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _userService.GetUsersAsync(page, pageSize, search, ct);
        return Ok(result);
    }

    /// <summary>GET /api/users/{id} — Get user by ID.</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetUser(long id, CancellationToken ct)
    {
        var result = await _userService.GetUserByIdAsync(id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>PUT /api/users/{id} — Update user profile (own profile only).</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateUser(long id, [FromBody] UpdateUserDto dto, CancellationToken ct)
    {
        var requestingUserId = JwtHelper.GetUserIdFromClaims(User);

        // Business Rule: Users can only update their own profile
        if (id != requestingUserId)
            return Forbid();

        var result = await _userService.UpdateUserAsync(id, dto, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>DELETE /api/users/{id} — Soft-delete a user. Admin only.</summary>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(long id, CancellationToken ct)
    {
        var requestingUserId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _userService.DeleteUserAsync(id, requestingUserId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
