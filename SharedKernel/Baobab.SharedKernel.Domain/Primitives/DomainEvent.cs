namespace Baobab.SharedKernel.Domain.Primitives;

public record DomainEvent(Ulid Id) : IDomainEvent;
