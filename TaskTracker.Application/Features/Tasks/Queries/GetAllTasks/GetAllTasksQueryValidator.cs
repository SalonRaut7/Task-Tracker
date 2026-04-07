using FluentValidation;

namespace TaskTracker.Application.Features.Tasks.Queries.GetAllTasks;

public class GetAllTasksQueryValidator : AbstractValidator<GetAllTasksQuery>
{
    public GetAllTasksQueryValidator()
    {
        RuleFor(x => x.TitleContains)
            .MaximumLength(100).WithMessage("Title filter cannot exceed 100 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.TitleContains));

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid status value")
            .When(x => x.Status.HasValue);

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value")
            .When(x => x.Priority.HasValue);
    }
}