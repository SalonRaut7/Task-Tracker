using FluentValidation;

namespace TaskTracker.Application.Features.Organizations.Commands.DeleteOrganization;

public sealed class DeleteOrganizationCommandValidator : AbstractValidator<DeleteOrganizationCommand>
{
    public DeleteOrganizationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
