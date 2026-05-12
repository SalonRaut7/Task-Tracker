using FluentValidation;

namespace TaskTracker.Application.Features.Sprints.Commands.StartSprint;

public sealed class StartSprintCommandValidator : AbstractValidator<StartSprintCommand>
{
    public StartSprintCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Sprint ID is required.");
    }
}
