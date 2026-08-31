using Baobab.SharedKernel.Domain.Primitives;

namespace Baobab.SharedKernel.Persistence.Audits;

public sealed class Audit : Entity
{
    public Ulid Id { get; private set; }
    public Ulid UserId { get; private set; }
    public string? Type { get; private set; }
    public string? TableName { get; private set; }
    public DateTime DateTime { get; private set; }
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public string? AffectedColumns { get; private set; }
    public string? PrimaryKey { get; private set; }

    private Audit() { }

    private Audit(Ulid userId, string type, string tableName, DateTime dateTime, string? oldValues, string? newValues, string? affectedColumns, string primaryKey)
    {
        Id = Ulid.NewUlid();
        UserId = userId;
        Type = type;
        TableName = tableName;
        DateTime = dateTime;
        OldValues = oldValues;
        NewValues = newValues;
        AffectedColumns = affectedColumns;
        PrimaryKey = primaryKey;
    }

    public static Audit Create(Ulid userId, string type, string tableName, DateTime dateTime, string? oldValues, string? newValues, string? affectedColumns, string primaryKey)
        => new(userId, type, tableName, dateTime, oldValues, newValues, affectedColumns, primaryKey);
}
