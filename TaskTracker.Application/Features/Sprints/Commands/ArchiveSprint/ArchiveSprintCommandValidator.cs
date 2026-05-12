using FluentValidation;

namespace TaskTracker.Application.Features.Sprints.Commands.ArchiveSprint;

public sealed class ArchiveSprintCommandValidator : AbstractValidator<ArchiveSprintCommand>
{
    public ArchiveSprintCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Sprint ID is required.");

        RuleFor(x => x.ArchiveReason)
            .NotEmpty().WithMessage("Archive reason is required when archiving a sprint.")
            .MaximumLength(500).WithMessage("Archive reason cannot exceed 500 characters.");
    }
}
