using System.ComponentModel.DataAnnotations;

namespace TrelloApi.Models;

public class Label
{
    public long Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Color { get; set; } = "#808080"; // Default gray

    public long ProjectId { get; set; }
    public virtual Project Project { get; set; } = null!;

    public virtual ICollection<TaskLabel> TaskLabels { get; set; } = new List<TaskLabel>();
}
