using FluentValidation;

namespace TaskTracker.Application.Features.Sprints.Commands.CompleteSprint;

public sealed class CompleteSprintCommandValidator : AbstractValidator<CompleteSprintCommand>
{
    public CompleteSprintCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Sprint ID is required.");
    }
}
