using FluentValidation;

namespace TaskTracker.Application.Features.Tasks.Commands.UpdateTask
{
    public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
    {
        private static readonly int[] AllowedEndDateExtensions = [1, 5, 10];

        public UpdateTaskCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id must be greater than 0");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("ProjectId is required");

            RuleFor(x => x.EpicId)
                .NotEmpty().WithMessage("EpicId cannot be empty GUID when provided")
                .When(x => x.EpicId.HasValue);

            RuleFor(x => x.SprintId)
                .NotEmpty().WithMessage("SprintId cannot be empty GUID when provided")
                .When(x => x.SprintId.HasValue);

            RuleFor(x => x.AssigneeId)
                .Must(value => value == null || value.Trim().Length > 0)
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

            RuleFor(x => x.EndDateExtensionDays)
                .Must(x => !x.HasValue || AllowedEndDateExtensions.Contains(x.Value))
                .WithMessage("EndDateExtensionDays must be one of: 1, 5, or 10.");

            RuleFor(x => x)
                .Must(x => !(x.EndDate.HasValue && x.EndDateExtensionDays.HasValue))
                .WithMessage("Provide either EndDate or EndDateExtensionDays, not both.");

            RuleFor(x => x)
                .Must(x => !x.EndDateExtensionDays.HasValue || x.EndDateExtensionDays.Value > 0)
                .WithMessage("EndDateExtensionDays must be a positive number of days.");
        }
    }
}