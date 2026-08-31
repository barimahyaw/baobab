namespace Baobab.SharedKernel.Persistence.OutBox.Idempotence;

public interface IOutboxMessageContext
{
    Guid MessageId { get; }
    void SetMessageId(Guid messageId);
}

public class OutboxMessageContext : IOutboxMessageContext
{
    public Guid MessageId { get; private set; }
    public void SetMessageId(Guid messageId) => MessageId = messageId;
}
