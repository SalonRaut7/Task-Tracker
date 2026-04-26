using FluentValidation;

namespace TaskTracker.Application.Features.Members.Queries.GetMembersByScope;

public sealed class GetMembersByScopeQueryValidator : AbstractValidator<GetMembersByScopeQuery>
{
    public GetMembersByScopeQueryValidator()
    {
        RuleFor(x => x.ScopeType)
            .IsInEnum().WithMessage("Invalid scope type.");

        RuleFor(x => x.ScopeId)
            .NotEmpty().WithMessage("Scope ID is required.");
    }
}
