using FluentValidation;

namespace TaskTracker.Application.Features.Epics.Queries.GetEpics;

public sealed class GetEpicsQueryValidator : AbstractValidator<GetEpicsQuery>
{
    public GetEpicsQueryValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty().When(x => x.ProjectId.HasValue);
    }
}
