using FluentValidation;

namespace TaskTracker.Application.Features.Sprints.Commands.DeleteSprint;

public sealed class DeleteSprintCommandValidator : AbstractValidator<DeleteSprintCommand>
{
    public DeleteSprintCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
