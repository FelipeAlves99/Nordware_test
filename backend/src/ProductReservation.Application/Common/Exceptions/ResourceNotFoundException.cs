namespace ProductReservation.Application.Common.Exceptions;

public sealed class ResourceNotFoundException(string resourceName, Guid id)
    : Exception($"{resourceName} '{id}' was not found.")
{
    public string ResourceName { get; } = resourceName;

    public Guid Id { get; } = id;
}
