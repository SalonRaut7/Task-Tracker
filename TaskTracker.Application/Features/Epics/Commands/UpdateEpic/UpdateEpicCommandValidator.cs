using FluentValidation;

namespace TaskTracker.Application.Features.Epics.Commands.UpdateEpic;

public sealed class UpdateEpicCommandValidator : AbstractValidator<UpdateEpicCommand>
{
    public UpdateEpicCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Epic ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Epic title is required.")
            .Matches(@"[a-zA-Z]").WithMessage("Epic title must contain at least one letter.")
            .MaximumLength(500).WithMessage("Epic title cannot exceed 500 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid epic status.");
    }
}
