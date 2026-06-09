namespace TrelloApi.DTOs.Task;

public class LabelDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public long ProjectId { get; set; }
}

public class CreateLabelDto
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#808080";
    public long ProjectId { get; set; }
}

public class ChecklistDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public long TaskId { get; set; }
    public List<ChecklistItemDto> Items { get; set; } = new();
}

public class CreateChecklistDto
{
    public string Title { get; set; } = string.Empty;
    public long TaskId { get; set; }
}

public class ChecklistItemDto
{
    public long Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int Position { get; set; }
    public long ChecklistId { get; set; }
}

public class CreateChecklistItemDto
{
    public string Content { get; set; } = string.Empty;
    public long ChecklistId { get; set; }
    public int Position { get; set; }
}

public class UpdateChecklistItemDto
{
    public string? Content { get; set; }
    public bool? IsCompleted { get; set; }
    public int? Position { get; set; }
}
