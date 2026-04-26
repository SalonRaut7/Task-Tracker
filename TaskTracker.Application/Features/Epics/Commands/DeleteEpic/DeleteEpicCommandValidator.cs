using FluentValidation;

namespace TaskTracker.Application.Features.Epics.Commands.DeleteEpic;

public sealed class DeleteEpicCommandValidator : AbstractValidator<DeleteEpicCommand>
{
    public DeleteEpicCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Epic ID is required.");
    }
}
