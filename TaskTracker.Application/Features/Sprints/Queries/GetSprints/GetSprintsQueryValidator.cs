using FluentValidation;

namespace TaskTracker.Application.Features.Sprints.Queries.GetSprints;

public sealed class GetSprintsQueryValidator : AbstractValidator<GetSprintsQuery>
{
    public GetSprintsQueryValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty().When(x => x.ProjectId.HasValue);
    }
}
