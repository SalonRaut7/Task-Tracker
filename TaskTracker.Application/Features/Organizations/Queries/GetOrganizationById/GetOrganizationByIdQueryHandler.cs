using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Mapping;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Organizations.Queries.GetOrganizationById;

public sealed class GetOrganizationByIdQueryHandler : IRequestHandler<GetOrganizationByIdQuery, OrganizationDto?>
{
    private readonly IOrganizationRepository _organizationRepository;

    public GetOrganizationByIdQueryHandler(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository;
    }

    public async Task<OrganizationDto?> Handle(GetOrganizationByIdQuery request, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByIdAsync(request.Id, cancellationToken);
        if (organization is null)
        {
            return null;
        }

        return OrganizationDtoMapper.ToDto(organization);
    }
}
