using FluentValidation;

namespace TaskTracker.Application.Features.Comments.Queries.GetComments;

public sealed class GetCommentsQueryValidator : AbstractValidator<GetCommentsQuery>
{
    public GetCommentsQueryValidator()
    {
        RuleFor(x => x.TaskId)
            .GreaterThan(0).WithMessage("Task ID must be greater than 0.")
            .When(x => x.TaskId.HasValue);
    }
}
