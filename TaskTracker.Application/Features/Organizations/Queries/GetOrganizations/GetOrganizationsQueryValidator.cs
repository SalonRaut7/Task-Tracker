using FluentValidation;

namespace TaskTracker.Application.Features.Organizations.Queries.GetOrganizations;

public sealed class GetOrganizationsQueryValidator : AbstractValidator<GetOrganizationsQuery>
{
    public GetOrganizationsQueryValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(100).WithMessage("Search cannot exceed 100 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Search));

        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0).WithMessage("Skip must be zero or greater")
            .When(x => x.Skip.HasValue);

        RuleFor(x => x.Take)
            .GreaterThan(0).WithMessage("Take must be greater than zero")
            .LessThanOrEqualTo(500).WithMessage("Take cannot exceed 500")
            .When(x => x.Take.HasValue);
    }
}
