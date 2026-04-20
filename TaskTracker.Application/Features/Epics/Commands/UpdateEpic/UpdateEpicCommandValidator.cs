using FluentValidation;

namespace TaskTracker.Application.Features.Epics.Commands.UpdateEpic;

public sealed class UpdateEpicCommandValidator : AbstractValidator<UpdateEpicCommand>
{
    public UpdateEpicCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(5000);
        RuleFor(x => x.Status).IsInEnum();
    }
}
