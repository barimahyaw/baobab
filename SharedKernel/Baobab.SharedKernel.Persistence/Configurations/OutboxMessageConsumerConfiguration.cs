using Baobab.SharedKernel.Persistence.OutBox;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Baobab.SharedKernel.Persistence.Configurations;

public class OutboxMessageConsumerConfiguration<S>(S schema) : IEntityTypeConfiguration<OutboxMessageConsumer>
    where S : ISchemaStringValue
{
    public S Schema { get; } = schema;
    public void Configure(EntityTypeBuilder<OutboxMessageConsumer> builder)
    {
        builder.ToTable("outbox_messages_consumer", Schema.Name);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.ToString(),
                value => Ulid.Parse(value)
            )
            .HasColumnType("varchar(26)");
    }
}