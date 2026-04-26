using FluentValidation;

namespace TaskTracker.Application.Features.Comments.Commands.CreateComment;

public sealed class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .GreaterThan(0).WithMessage("Task ID must be greater than 0.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Comment content is required.")
            .MaximumLength(5000).WithMessage("Comment content cannot exceed 5000 characters.");
    }
}
