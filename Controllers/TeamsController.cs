using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrelloApi.DTOs.Team;
using TrelloApi.Helpers;
using TrelloApi.Services.Interfaces;

namespace TrelloApi.Controllers;

/// <summary>
/// Teams controller — full CRUD for teams and team membership management.
/// All endpoints require JWT authentication.
/// </summary>
[ApiController]
[Route("api/teams")]
[Authorize]
[Produces("application/json")]
public class TeamsController : ControllerBase
{
    private readonly ITeamService _teamService;
    public TeamsController(ITeamService teamService) => _teamService = teamService;

    /// <summary>POST /api/teams — Create a new team. Authenticated user becomes Admin.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamDto dto, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _teamService.CreateTeamAsync(dto, userId, ct);
        return result.Success ? CreatedAtAction(nameof(GetTeam), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    /// <summary>GET /api/teams — Get all teams the current user belongs to.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyTeams(CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _teamService.GetMyTeamsAsync(userId, ct);
        return Ok(result);
    }

    /// <summary>GET /api/teams/{id} — Get team by ID (members only).</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetTeam(long id, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _teamService.GetTeamByIdAsync(id, userId, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>PUT /api/teams/{id} — Update team (team admin only).</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateTeam(long id, [FromBody] UpdateTeamDto dto, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _teamService.UpdateTeamAsync(id, dto, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>DELETE /api/teams/{id} — Delete team (owner only).</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteTeam(long id, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _teamService.DeleteTeamAsync(id, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>GET /api/teams/{id}/members — List all team members.</summary>
    [HttpGet("{id:long}/members")]
    public async Task<IActionResult> GetMembers(long id, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _teamService.GetMembersAsync(id, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>POST /api/teams/{id}/members — Add a user to the team (admin only).</summary>
    [HttpPost("{id:long}/members")]
    public async Task<IActionResult> AddMember(long id, [FromBody] AddTeamMemberDto dto, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _teamService.AddMemberAsync(id, dto, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>DELETE /api/teams/{id}/members/{userId} — Remove a user from the team (admin only).</summary>
    [HttpDelete("{id:long}/members/{userId:long}")]
    public async Task<IActionResult> RemoveMember(long id, long userId, CancellationToken ct)
    {
        var requestingUserId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _teamService.RemoveMemberAsync(id, userId, requestingUserId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>POST /api/teams/{id}/invite — Invite a user by email.</summary>
    [HttpPost("{id:long}/invite")]
    public async Task<IActionResult> InviteUser(long id, [FromBody] InviteEmailDto dto, CancellationToken ct)
    {
        var requestingUserId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _teamService.InviteUserByEmailAsync(id, dto.Email, requestingUserId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>POST /api/teams/accept-invite — Accept an invitation token.</summary>
    [HttpPost("accept-invite")]
    public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteDto dto, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _teamService.AcceptInvitationAsync(dto.Token, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
