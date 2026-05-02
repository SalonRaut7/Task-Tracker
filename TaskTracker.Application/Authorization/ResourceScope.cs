using System.Globalization;

namespace TaskTracker.Application.Authorization;

public sealed record ResourceScope
{
    public ResourceScope(ResourceType resourceType, Guid id)
        : this(resourceType, id.ToString())
    {
    }

    public ResourceScope(ResourceType resourceType, int id)
        : this(resourceType, id.ToString(CultureInfo.InvariantCulture))
    {
    }

    public ResourceScope(ResourceType resourceType, string id)
    {
        ResourceType = resourceType;
        Id = id;
    }

    public ResourceType ResourceType { get; }

    public string Id { get; }
}
