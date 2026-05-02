using FluentValidation;

namespace TaskTracker.Application.Features.Projects.Commands.CreateProject;

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("Organization ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .Matches(@"[a-zA-Z]").WithMessage("Project name must contain at least one letter.")
            .MaximumLength(200).WithMessage("Project name cannot exceed 200 characters.");

        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("Project key is required.")
            .MaximumLength(10).WithMessage("Project key cannot exceed 10 characters.")
            .Matches(@"^[A-Z0-9_-]+$")
            .WithMessage("Project key must contain only uppercase letters, numbers, hyphens, and underscores.")
            .Matches(@"[A-Z]").WithMessage("Project key must contain at least one letter.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");
    }
}
