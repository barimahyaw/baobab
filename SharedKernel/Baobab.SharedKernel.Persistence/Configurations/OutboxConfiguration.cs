using Baobab.SharedKernel.Persistence.OutBox;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Baobab.SharedKernel.Persistence.Configurations;

public class OutboxConfiguration<T>(T schema) : IEntityTypeConfiguration<OutboxMessage>
    where T : ISchemaStringValue
{
    public T Schema { get; } = schema;

    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages", Schema.Name);
    }
}