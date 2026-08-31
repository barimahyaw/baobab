using Baobab.SharedKernel.Domain.Primitives;

namespace Baobab.SharedKernel.Persistence.OutBox;

public class OutboxMessage : Entity
{
    public Ulid Id { get; set; }
    public string Type { get; set; } = default!;
    public string Assembly { get; set; } = default!;    
    public string Content { get; set; } = default!;
    public DateTime OccurredOnUtc { get; set; }
    public DateTime? ProcessedDateUtc { get; set; }
    public DateTime? ProcessLastAttemptOnUtc { get; set; }
    public int ProcessingAttempts { get; set; }
    public string? Error { get; set; }
}
