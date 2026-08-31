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

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.ToString(),
                value => Ulid.Parse(value)
            )
            .HasColumnType("varchar(26)");

        builder.Property(x => x.UserId)
            .HasConversion(
                userId => userId.ToString(),
                value => Ulid.Parse(value)
            )
            .HasColumnType("varchar(26)");
    }
}