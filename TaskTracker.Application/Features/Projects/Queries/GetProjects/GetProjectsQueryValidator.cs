using FluentValidation;

namespace TaskTracker.Application.Features.Projects.Queries.GetProjects;

public sealed class GetProjectsQueryValidator : AbstractValidator<GetProjectsQuery>
{
    public GetProjectsQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty().When(x => x.OrganizationId.HasValue);
    }
}
