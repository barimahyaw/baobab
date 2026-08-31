using Baobab.SharedKernel.Domain.Primitives;
using Baobab.SharedKernel.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Baobab.SharedKernel.Persistence.Configurations;

public static class EntityExtraConfiguration<T> where T : EntityExtra
{
    public static EntityTypeBuilder Configure(EntityTypeBuilder<T> builder)
    {
        builder.Property(c => c.CreatedUserId)
            .HasConversion(
                userId => userId.Id.ToString(),
                value => UserId.Create(Ulid.Parse(value))
            )
            .HasColumnType("varchar(26)");

        builder.Property(c => c.LastModifiedUserId)
            .HasConversion(
                userId => userId != null ? userId.Id.ToString() : null,
                value => value != null ? UserId.Create(Ulid.Parse(value)) : null
            )
            .HasColumnType("varchar(26)");

        builder.Property(c => c.CreatedAtUtc)
            .IsRequired();

        return builder;
    }
}