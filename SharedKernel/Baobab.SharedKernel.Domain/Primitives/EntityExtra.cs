using Baobab.SharedKernel.Domain.ValueObjects;

namespace Baobab.SharedKernel.Domain.Primitives;

public class EntityExtra : Entity
{
    public UserId CreatedUserId { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public UserId? LastModifiedUserId { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public bool IsActive { get; set; }
}