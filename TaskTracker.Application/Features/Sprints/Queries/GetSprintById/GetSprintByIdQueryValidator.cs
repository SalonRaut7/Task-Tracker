using FluentValidation;

namespace TaskTracker.Application.Features.Sprints.Queries.GetSprintById;

public sealed class GetSprintByIdQueryValidator : AbstractValidator<GetSprintByIdQuery>
{
    public GetSprintByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Sprint ID is required.");
    }
}
