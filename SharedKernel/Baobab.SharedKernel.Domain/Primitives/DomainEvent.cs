namespace Baobab.SharedKernel.Domain.Primitives;

public record DomainEvent(Guid Id) : IDomainEvent;
