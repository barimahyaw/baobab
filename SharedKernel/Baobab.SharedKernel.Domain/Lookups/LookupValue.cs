using Baobab.SharedKernel.Domain.Primitives;

namespace Baobab.SharedKernel.Domain.Lookups;

public class LookupValue : EntityExtra
{
    public Ulid Id { get; private set; }
    public string ValueName { get; private set; } = null!;
    public string ValueDescription { get; private set; } = null!;
    public long LookupTypeId { get; private set; }
}
