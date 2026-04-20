using FluentValidation;

namespace TaskTracker.Application.Features.Comments.Commands.DeleteComment;

public sealed class DeleteCommentCommandValidator : AbstractValidator<DeleteCommentCommand>
{
    public DeleteCommentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
