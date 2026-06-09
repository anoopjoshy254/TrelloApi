using System.ComponentModel.DataAnnotations;

namespace TrelloApi.Models;

public class ChecklistItem
{
    public long Id { get; set; }

    [Required, MaxLength(500)]
    public string Content { get; set; } = string.Empty;

    public bool IsCompleted { get; set; } = false;

    public int Position { get; set; } = 0;

    public long ChecklistId { get; set; }
    public virtual Checklist Checklist { get; set; } = null!;
}
