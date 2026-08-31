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
                userId => (Guid)userId,
                value => UserId.Create(value));

        builder.Property(c => c.LastModifiedUserId)
            .HasConversion(
                userId => userId != null ? (Guid)userId : (Guid?)null,
                value => value != null ? UserId.Create(value.Value) : null);

        builder.Property(c => c.CreatedAtUtc)
            .IsRequired();

        return builder;
    }
}