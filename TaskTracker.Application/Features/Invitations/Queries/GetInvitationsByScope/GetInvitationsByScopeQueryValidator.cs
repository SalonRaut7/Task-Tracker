using FluentValidation;

namespace TaskTracker.Application.Features.Invitations.Queries.GetInvitationsByScope;

public sealed class GetInvitationsByScopeQueryValidator : AbstractValidator<GetInvitationsByScopeQuery>
{
    public GetInvitationsByScopeQueryValidator()
    {
        RuleFor(x => x.ScopeType)
            .IsInEnum().WithMessage("Invalid scope type.");

        RuleFor(x => x.ScopeId)
            .NotEmpty().WithMessage("Scope ID is required.");
    }
}
