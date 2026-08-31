using Baobab.SharedKernel.Domain.Primitives;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Baobab.SharedKernel.Persistence.OutBox.Idempotence;

internal sealed class IdempotentDomainEventHandler<TDomainEvent, TDbContext>(
    INotificationHandler<TDomainEvent> decorated,
    TDbContext dbContext,
    IOutboxMessageContext outboxMessageContext)
    : INotificationHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
    where TDbContext : DbContext
{
    private readonly INotificationHandler<TDomainEvent> _decorated = decorated;
    private readonly TDbContext _dbContext = dbContext;
    private readonly IOutboxMessageContext _outboxMessageContext = outboxMessageContext;

    public async Task Handle(TDomainEvent notification, CancellationToken cancellationToken)
    {
        string consumer = _decorated.GetType().Name;
        Guid messageId = _outboxMessageContext.MessageId;

        if (await _dbContext.Set<OutboxMessageConsumer>()
                .AnyAsync(
                    outboxMessageConsumer =>
                        outboxMessageConsumer.Id == messageId &&
                        outboxMessageConsumer.Name == consumer,
                    cancellationToken))
        {
            return;
        }

        await _decorated.Handle(notification, cancellationToken);

        _dbContext.Set<OutboxMessageConsumer>()
            .Add(new OutboxMessageConsumer
            {
                Id = messageId,
                Name = consumer
            });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
