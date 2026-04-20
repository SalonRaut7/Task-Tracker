using FluentValidation;

namespace TaskTracker.Application.Features.Tasks.Commands.CreateTask
{
    public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
    {
        public CreateTaskCommandValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("ProjectId is required");

            RuleFor(x => x.EpicId)
                .NotEmpty().WithMessage("EpicId cannot be empty GUID when provided")
                .When(x => x.EpicId.HasValue);

            RuleFor(x => x.SprintId)
                .NotEmpty().WithMessage("SprintId cannot be empty GUID when provided")
                .When(x => x.SprintId.HasValue);

            RuleFor(x => x.AssigneeId)
                .Must(value => string.IsNullOrWhiteSpace(value) || !string.IsNullOrWhiteSpace(value.Trim()))
                .WithMessage("AssigneeId cannot be whitespace");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(100).WithMessage("Title cannot exceed 100 characters");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Invalid status value");

            RuleFor(x => x.Priority)
                .IsInEnum().WithMessage("Invalid priority value");

            RuleFor(x => x.StartDate)
                .LessThanOrEqualTo(x => x.EndDate)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("Start date must be before or equal to end date.");
        }
    }
}