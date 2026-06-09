using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrelloApi.Data;
using TrelloApi.DTOs.Task;
using TrelloApi.Helpers;
using TrelloApi.Models;

namespace TrelloApi.Controllers;

[ApiController]
[Route("api/labels")]
[Authorize]
[Produces("application/json")]
public class LabelsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public LabelsController(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper  = mapper;
    }

    /// <summary>GET /api/labels?projectId=1 — Get all labels for a project.</summary>
    [HttpGet]
    public async Task<IActionResult> GetLabels([FromQuery] long projectId, CancellationToken ct)
    {
        var labels = await _context.Labels
            .Where(l => l.ProjectId == projectId)
            .ToListAsync(ct);

        return Ok(ApiResponse<List<LabelDto>>.Ok(_mapper.Map<List<LabelDto>>(labels)));
    }

    /// <summary>POST /api/labels — Create a label for a project.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateLabel([FromBody] CreateLabelDto dto, CancellationToken ct)
    {
        var label = _mapper.Map<Label>(dto);
        await _context.Labels.AddAsync(label, ct);
        await _context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetLabels), new { projectId = dto.ProjectId },
            ApiResponse<LabelDto>.Ok(_mapper.Map<LabelDto>(label), "Label created."));
    }

    /// <summary>PUT /api/labels/{id} — Update a label.</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateLabel(long id, [FromBody] CreateLabelDto dto, CancellationToken ct)
    {
        var label = await _context.Labels.FindAsync(new object[] { id }, ct);
        if (label is null) return NotFound(ApiResponse.Fail("Label not found."));

        label.Name  = dto.Name;
        label.Color = dto.Color;
        _context.Labels.Update(label);
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<LabelDto>.Ok(_mapper.Map<LabelDto>(label), "Label updated."));
    }

    /// <summary>DELETE /api/labels/{id} — Delete a label.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteLabel(long id, CancellationToken ct)
    {
        var label = await _context.Labels.FindAsync(new object[] { id }, ct);
        if (label is null) return NotFound(ApiResponse.Fail("Label not found."));

        _context.Labels.Remove(label);
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Label deleted."));
    }

    /// <summary>POST /api/labels/{labelId}/tasks/{taskId} — Apply a label to a task.</summary>
    [HttpPost("{labelId:long}/tasks/{taskId:long}")]
    public async Task<IActionResult> ApplyLabel(long labelId, long taskId, CancellationToken ct)
    {
        var exists = await _context.TaskLabels
            .AnyAsync(tl => tl.TaskId == taskId && tl.LabelId == labelId, ct);
        if (exists) return BadRequest(ApiResponse.Fail("Label already applied to this task."));

        await _context.TaskLabels.AddAsync(new TaskLabel { TaskId = taskId, LabelId = labelId }, ct);
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Label applied."));
    }

    /// <summary>DELETE /api/labels/{labelId}/tasks/{taskId} — Remove a label from a task.</summary>
    [HttpDelete("{labelId:long}/tasks/{taskId:long}")]
    public async Task<IActionResult> RemoveLabel(long labelId, long taskId, CancellationToken ct)
    {
        var taskLabel = await _context.TaskLabels
            .FirstOrDefaultAsync(tl => tl.TaskId == taskId && tl.LabelId == labelId, ct);
        if (taskLabel is null) return NotFound(ApiResponse.Fail("Label not applied to this task."));

        _context.TaskLabels.Remove(taskLabel);
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Label removed."));
    }
}
