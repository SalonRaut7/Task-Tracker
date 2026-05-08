using FluentValidation;

namespace TaskTracker.Application.Features.TaskAttachments.Commands.DeleteAttachment;

public class DeleteAttachmentCommandValidator : AbstractValidator<DeleteAttachmentCommand>
{
    public DeleteAttachmentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Attachment Id is required.");

        RuleFor(x => x.TaskId)
            .GreaterThan(0).WithMessage("TaskId must be a positive integer.");

        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("ProjectId is required.");
    }
}
