using FluentValidation;

namespace TaskTracker.Application.Features.Projects.Queries.GetProjectById;

public sealed class GetProjectByIdQueryValidator : AbstractValidator<GetProjectByIdQuery>
{
    public GetProjectByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Project ID is required.");
    }
}
