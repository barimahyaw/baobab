using Baobab.SharedKernel.Domain.Primitives;
using System.ComponentModel.DataAnnotations;

namespace Baobab.SharedKernel.Domain.Lookups;

public class LookupType : EntityExtra
{
    public Guid Id { get; private set; }
    [Required, MaxLength(150)]
    public string TypeName { get; private set; } = null!;
    [Required, MaxLength(255)]
    public string Description { get; private set; } = null!;

    private readonly HashSet<LookupValue> _lookupValues = [];
    public IReadOnlyList<LookupValue> LookupValues => _lookupValues.ToList();
}
