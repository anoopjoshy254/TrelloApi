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
[Route("api/checklists")]
[Authorize]
[Produces("application/json")]
public class ChecklistsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public ChecklistsController(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper  = mapper;
    }

    /// <summary>GET /api/checklists?taskId=1 — Get all checklists for a task.</summary>
    [HttpGet]
    public async Task<IActionResult> GetChecklists([FromQuery] long taskId, CancellationToken ct)
    {
        var checklists = await _context.Checklists
            .Include(c => c.Items.OrderBy(i => i.Position))
            .Where(c => c.TaskId == taskId)
            .ToListAsync(ct);

        return Ok(ApiResponse<List<ChecklistDto>>.Ok(_mapper.Map<List<ChecklistDto>>(checklists)));
    }

    /// <summary>POST /api/checklists — Create a checklist for a task.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateChecklist([FromBody] CreateChecklistDto dto, CancellationToken ct)
    {
        var checklist = _mapper.Map<Checklist>(dto);
        await _context.Checklists.AddAsync(checklist, ct);
        await _context.SaveChangesAsync(ct);

        var created = await _context.Checklists
            .Include(c => c.Items)
            .FirstAsync(c => c.Id == checklist.Id, ct);

        return CreatedAtAction(nameof(GetChecklists), new { taskId = dto.TaskId },
            ApiResponse<ChecklistDto>.Ok(_mapper.Map<ChecklistDto>(created), "Checklist created."));
    }

    /// <summary>DELETE /api/checklists/{id} — Delete a checklist.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteChecklist(long id, CancellationToken ct)
    {
        var checklist = await _context.Checklists.FindAsync(new object[] { id }, ct);
        if (checklist is null) return NotFound(ApiResponse.Fail("Checklist not found."));

        _context.Checklists.Remove(checklist);
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Checklist deleted."));
    }

    // ─── CHECKLIST ITEMS ──────────────────────────────────

    /// <summary>POST /api/checklists/items — Add an item to a checklist.</summary>
    [HttpPost("items")]
    public async Task<IActionResult> CreateItem([FromBody] CreateChecklistItemDto dto, CancellationToken ct)
    {
        var item = _mapper.Map<ChecklistItem>(dto);
        await _context.ChecklistItems.AddAsync(item, ct);
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<ChecklistItemDto>.Ok(_mapper.Map<ChecklistItemDto>(item), "Item added."));
    }

    /// <summary>PUT /api/checklists/items/{id} — Update a checklist item (toggle, rename, reorder).</summary>
    [HttpPut("items/{id:long}")]
    public async Task<IActionResult> UpdateItem(long id, [FromBody] UpdateChecklistItemDto dto, CancellationToken ct)
    {
        var item = await _context.ChecklistItems.FindAsync(new object[] { id }, ct);
        if (item is null) return NotFound(ApiResponse.Fail("Checklist item not found."));

        if (dto.Content is not null)      item.Content     = dto.Content;
        if (dto.IsCompleted is not null)  item.IsCompleted = dto.IsCompleted.Value;
        if (dto.Position is not null)     item.Position    = dto.Position.Value;

        _context.ChecklistItems.Update(item);
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<ChecklistItemDto>.Ok(_mapper.Map<ChecklistItemDto>(item), "Item updated."));
    }

    /// <summary>DELETE /api/checklists/items/{id} — Delete a checklist item.</summary>
    [HttpDelete("items/{id:long}")]
    public async Task<IActionResult> DeleteItem(long id, CancellationToken ct)
    {
        var item = await _context.ChecklistItems.FindAsync(new object[] { id }, ct);
        if (item is null) return NotFound(ApiResponse.Fail("Checklist item not found."));

        _context.ChecklistItems.Remove(item);
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Item deleted."));
    }
}
