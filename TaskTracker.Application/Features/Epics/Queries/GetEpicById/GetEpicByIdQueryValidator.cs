using FluentValidation;

namespace TaskTracker.Application.Features.Epics.Queries.GetEpicById;

public sealed class GetEpicByIdQueryValidator : AbstractValidator<GetEpicByIdQuery>
{
    public GetEpicByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
