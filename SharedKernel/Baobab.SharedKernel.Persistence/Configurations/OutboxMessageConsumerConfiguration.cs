using Baobab.SharedKernel.Persistence.OutBox;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Baobab.SharedKernel.Persistence.Configurations;

public class OutboxMessageConsumerConfiguration<S>(S schema) : IEntityTypeConfiguration<OutboxMessageConsumer>
    where S : IProjectStringValue
{
    public S Schema { get; } = schema;
    public void Configure(EntityTypeBuilder<OutboxMessageConsumer> builder)
    {
        builder.ToTable("outbox_messages_consumer", Schema.Name);

        builder.HasKey(x => new { x.Id, x.Name });
    }
}