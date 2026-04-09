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

        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0).WithMessage("Skip must be zero or greater")
            .When(x => x.Skip.HasValue);

        RuleFor(x => x.Take)
            .GreaterThan(0).WithMessage("Take must be greater than zero")
            .LessThanOrEqualTo(500).WithMessage("Take cannot exceed 500")
            .When(x => x.Take.HasValue);
    }
}