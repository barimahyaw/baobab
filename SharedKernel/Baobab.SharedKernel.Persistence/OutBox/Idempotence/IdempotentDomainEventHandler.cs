using Baobab.SharedKernel.Domain.Primitives;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Baobab.SharedKernel.Persistence.OutBox.Idempotence;

internal class IdempotentDomainEventHandler<TDomainEvent, TDbContext>(
    INotificationHandler<TDomainEvent> decorated,
    TDbContext dbContext)
    where TDomainEvent : DomainEvent
    where TDbContext : DbContext
{
    private readonly INotificationHandler<TDomainEvent> _decorated = decorated;
    private readonly TDbContext _dbContext = dbContext;

    public async Task Handle(TDomainEvent notification, CancellationToken cancellationToken)
    {
        //string consumerName = _decorated.GetType().FullName!;
        string consumer = _decorated.GetType().Name;

        if (await _dbContext.Set<OutboxMessageConsumer>()
                .AnyAsync(
                    outboxMessageConsumer =>
                        outboxMessageConsumer.Name == consumer &&
                        outboxMessageConsumer.Id == notification.Id,
                    cancellationToken))
        {
            return;
        }

        await _decorated.Handle(notification, cancellationToken);

        _dbContext.Set<OutboxMessageConsumer>()
            .Add(new OutboxMessageConsumer
            {
                Id = notification.Id,
                Name = consumer
            });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
