using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;

namespace Baobab.SharedKernel.Persistence.Audits;

public sealed class AuditEntry(EntityEntry entry)
{
    public EntityEntry Entry { get; } = entry;
    public Ulid UserId { get; set; }
    public string TableName { get; set; } = null!;
    public Dictionary<string, object> KeyValues { get; } = [];
    public Dictionary<string, object> OldValues { get; } = [];
    public Dictionary<string, object> NewValues { get; } = [];
    public List<PropertyEntry> TemporaryProperties { get; } = [];
    public AuditType AuditType { get; set; }
    public List<string> ChangedColumns { get; } = [];
    public bool HasTemporaryProperties => TemporaryProperties.Count != 0;

    public Audit ToAudit()
    {
        var audit = Audit.Create(
            UserId,
            AuditType.ToString(),
            TableName,
            DateTime.UtcNow,
            OldValues.Count == 0 ? null : JsonConvert.SerializeObject(OldValues),
            NewValues.Count == 0 ? null : JsonConvert.SerializeObject(NewValues),
            ChangedColumns.Count == 0 ? null : JsonConvert.SerializeObject(ChangedColumns),
            JsonConvert.SerializeObject(KeyValues));

        return audit;
    }
}