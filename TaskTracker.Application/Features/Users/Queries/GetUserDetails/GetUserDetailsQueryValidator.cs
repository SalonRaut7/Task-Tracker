using FluentValidation;

namespace TaskTracker.Application.Features.Users.Queries.GetUserDetails;

public sealed class GetUserDetailsQueryValidator : AbstractValidator<GetUserDetailsQuery>
{
    public GetUserDetailsQueryValidator()
    {
        RuleFor(query => query.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
