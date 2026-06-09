using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrelloApi.DTOs.Project;
using TrelloApi.Helpers;
using TrelloApi.Services.Interfaces;

namespace TrelloApi.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
[Produces("application/json")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    public ProjectsController(IProjectService projectService) => _projectService = projectService;

    /// <summary>POST /api/projects — Create a project (must be authenticated).</summary>
    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto dto, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _projectService.CreateProjectAsync(dto, userId, ct);
        return result.Success ? CreatedAtAction(nameof(GetProject), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    /// <summary>GET /api/projects — Get all projects the current user is a member of.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyProjects(CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _projectService.GetMyProjectsAsync(userId, ct);
        return Ok(result);
    }

    /// <summary>GET /api/projects/{id} — Get project details (members only).</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetProject(long id, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _projectService.GetProjectByIdAsync(id, userId, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>PUT /api/projects/{id} — Update project (owner only).</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateProject(long id, [FromBody] UpdateProjectDto dto, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _projectService.UpdateProjectAsync(id, dto, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>DELETE /api/projects/{id} — Soft-delete project (owner only).</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteProject(long id, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _projectService.DeleteProjectAsync(id, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>PUT /api/projects/{id}/columns — Update custom columns (project members).</summary>
    [HttpPut("{id:long}/columns")]
    public async Task<IActionResult> UpdateColumns(long id, [FromBody] UpdateProjectColumnsDto dto, CancellationToken ct)
    {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        var result = await _projectService.UpdateProjectColumnsAsync(id, dto.Columns, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
