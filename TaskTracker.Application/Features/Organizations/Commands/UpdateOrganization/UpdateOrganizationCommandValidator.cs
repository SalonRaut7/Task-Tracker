using FluentValidation;

namespace TaskTracker.Application.Features.Organizations.Commands.UpdateOrganization;

public sealed class UpdateOrganizationCommandValidator : AbstractValidator<UpdateOrganizationCommand>
{
    public UpdateOrganizationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Organization ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .Matches(@"^[a-zA-Z0-9\s]+$").WithMessage("Name must be alphanumeric.")
            .Must(name => name.Any(char.IsLetter))
            .WithMessage("Name must contain at least one letter.")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(200).WithMessage("Slug cannot exceed 200 characters.")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must be URL-safe: lowercase letters, numbers, and hyphens only.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");
    }
}
