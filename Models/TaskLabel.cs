namespace TrelloApi.Models;

public class TaskLabel
{
    public long TaskId { get; set; }
    public virtual TaskItem Task { get; set; } = null!;

    public long LabelId { get; set; }
    public virtual Label Label { get; set; } = null!;
}
