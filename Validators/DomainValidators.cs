using FluentValidation;
using TrelloApi.DTOs.Task;
using TrelloApi.DTOs.Team;
using TrelloApi.DTOs.Project;
using TrelloApi.DTOs.Comment;

namespace TrelloApi.Validators;

public class CreateTaskValidator : AbstractValidator<CreateTaskDto>
{
    private static readonly string[] ValidPriorities = { "Low", "Medium", "High", "Critical" };

    public CreateTaskValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task title is required.")
            .MaximumLength(300).WithMessage("Task title must not exceed 300 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Description != null);

        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("A valid ProjectId is required.");

        RuleFor(x => x.Priority)
            .Must(p => ValidPriorities.Contains(p))
            .WithMessage($"Priority must be one of: {string.Join(", ", ValidPriorities)}");

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future.")
            .When(x => x.DueDate.HasValue);

        RuleFor(x => x.EstimatedHours)
            .GreaterThan(0).WithMessage("Estimated hours must be greater than 0.")
            .LessThanOrEqualTo(999).WithMessage("Estimated hours must not exceed 999.")
            .When(x => x.EstimatedHours.HasValue);
    }
}

public class UpdateTaskValidator : AbstractValidator<UpdateTaskDto>
{
    public UpdateTaskValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow).When(x => x.DueDate.HasValue);
    }
}

public class CreateTeamValidator : AbstractValidator<CreateTeamDto>
{
    public CreateTeamValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Team name is required.")
            .MaximumLength(150).WithMessage("Team name must not exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description != null);
    }
}

public class CreateProjectValidator : AbstractValidator<CreateProjectDto>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(200).WithMessage("Project name must not exceed 200 characters.");

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future.")
            .When(x => x.DueDate.HasValue);
    }
}

public class CreateCommentValidator : AbstractValidator<CreateCommentDto>
{
    public CreateCommentValidator()
    {
        RuleFor(x => x.TaskId)
            .GreaterThan(0).WithMessage("A valid TaskId is required.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Comment content is required.")
            .MaximumLength(5000).WithMessage("Comment must not exceed 5000 characters.");
    }
}
