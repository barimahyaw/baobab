using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Baobab.SharedKernel.Persistence.Audits;

namespace Baobab.SharedKernel.Persistence.Configurations;

public class AuditTrailConfiguration<S>(S schema) : IEntityTypeConfiguration<Audit>
    where S : ISchemaStringValue
{
    public S Schema { get; } = schema;

    public void Configure(EntityTypeBuilder<Audit> builder)
    {
        builder.ToTable("audits", Schema.Name);
    }
}