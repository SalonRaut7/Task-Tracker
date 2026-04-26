using FluentValidation;

namespace TaskTracker.Application.Features.Epics.Commands.CreateEpic;

public sealed class CreateEpicCommandValidator : AbstractValidator<CreateEpicCommand>
{
    public CreateEpicCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Epic title is required.")
            .MaximumLength(500).WithMessage("Epic title cannot exceed 500 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid epic status.");
    }
}
