using FluentValidation;

namespace TaskTracker.Application.Features.Sprints.Commands.CancelSprint;

public sealed class CancelSprintCommandValidator : AbstractValidator<CancelSprintCommand>
{
    public CancelSprintCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Sprint ID is required.");
    }
}
