using FluentValidation;

namespace TaskTracker.Application.Features.Organizations.Queries.GetOrganizationById;

public sealed class GetOrganizationByIdQueryValidator : AbstractValidator<GetOrganizationByIdQuery>
{
    public GetOrganizationByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Organization ID is required.");
    }
}
