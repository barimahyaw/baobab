namespace Baobab.SharedKernel.Domain.Primitives;

public abstract class AggregateRoot : EntityExtra
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    public IReadOnlyCollection<IDomainEvent> GetDomainEvents()
        => _domainEvents.AsReadOnly();

    public void ClearDomainEvents()
        => _domainEvents.Clear();
}
