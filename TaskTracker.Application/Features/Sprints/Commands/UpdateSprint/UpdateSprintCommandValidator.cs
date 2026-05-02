using FluentValidation;

namespace TaskTracker.Application.Features.Sprints.Commands.UpdateSprint;

public sealed class UpdateSprintCommandValidator : AbstractValidator<UpdateSprintCommand>
{
    public UpdateSprintCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Sprint ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Sprint name is required.")
            .Matches(@"[a-zA-Z]").WithMessage("Sprint name must contain at least one letter.")
            .MaximumLength(200).WithMessage("Sprint name cannot exceed 200 characters.");

        RuleFor(x => x.Goal)
            .MaximumLength(1000).WithMessage("Goal cannot exceed 1000 characters.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid sprint status.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.");

        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(x => x.EndDate)
            .WithMessage("Start date must be earlier than or equal to end date.");
    }
}
