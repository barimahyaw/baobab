namespace Baobab.SharedKernel.Persistence.OutBox;

public class OutboxMessageConsumer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}