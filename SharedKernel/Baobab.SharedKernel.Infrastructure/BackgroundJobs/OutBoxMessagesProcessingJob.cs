using Baobab.SharedKernel.Domain.Primitives;
using Baobab.SharedKernel.Domain.Primitives.Factory;
using Baobab.SharedKernel.Infrastructure.Resilience;
using Baobab.SharedKernel.Persistence.OutBox;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;

namespace Baobab.SharedKernel.Infrastructure.BackgroundJobs;

public class OutBoxMessagesProcessingJob<TDbContext>(
    ILogger<OutBoxMessagesProcessingJob<TDbContext>> logger,
    TDbContext dbContext,
    IPublisher publisher) : IOutBoxMessagesProcessingJob
    where TDbContext : DbContext
{
    public async Task Execute(CancellationToken cancellationToken = default)
    {
        var messages = await dbContext
               .Set<OutboxMessage>()
               .Where(m => m.ProcessedDateUtc == null)
               .OrderByDescending(m => m.ProcessedDateUtc)
               .Take(20)
               .ToListAsync(cancellationToken);

        if (messages.Count > 0) logger.LogInformation("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            logger.LogInformation("Processing outbox message with ID: {Id} of TYPE: {Type}", message.Id, message.Type);

            var errorMessage = $"Failed to process outbox message with ID: {message.Id} of TYPE: {message.Type}";

            var retryPolicy = PollyPolicy<OutBoxMessagesProcessingJob<TDbContext>>.Retry(logger, errorMessage);

            message.ProcessingAttempts++;
            message.ProcessLastAttemptOnUtc = DateTime.UtcNow;

            PolicyResult policyResult = await retryPolicy.ExecuteAndCaptureAsync(async () =>
            {
                IDomainEvent domainEvent = EventFactory.CreateEventTypeUsingReflection(message.Assembly, message.Type, message.Content);

                if (domainEvent == null)
                {
                    message.ProcessedDateUtc = DateTime.UtcNow;
                    message.Error = "Failed to deserialize domain event";
                    logger.LogError("Failed to deserialize domain event for outbox message {Id} of TYPE: {Type}", message.Id, message.Type);
                    return;
                }

                await publisher.Publish(domainEvent, cancellationToken);

                message.ProcessedDateUtc = DateTime.UtcNow;
                logger.LogInformation("Successfully processed outbox message with ID: {Id} of TYPE: {Type}", message.Id, message.Type);
            });
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}