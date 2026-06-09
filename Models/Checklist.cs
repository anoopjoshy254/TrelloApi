using System.ComponentModel.DataAnnotations;

namespace TrelloApi.Models;

public class Checklist
{
    public long Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public long TaskId { get; set; }
    public virtual TaskItem Task { get; set; } = null!;

    public virtual ICollection<ChecklistItem> Items { get; set; } = new List<ChecklistItem>();
}
