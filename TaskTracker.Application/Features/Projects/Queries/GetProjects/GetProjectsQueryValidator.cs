using FluentValidation;

namespace TaskTracker.Application.Features.Projects.Queries.GetProjects;

public sealed class GetProjectsQueryValidator : AbstractValidator<GetProjectsQuery>
{
    public GetProjectsQueryValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("Organization ID cannot be an empty GUID when provided.")
            .When(x => x.OrganizationId.HasValue);
    }
}
