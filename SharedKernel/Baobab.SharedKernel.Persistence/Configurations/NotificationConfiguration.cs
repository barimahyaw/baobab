using Baobab.SharedKernel.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Baobab.SharedKernel.Persistence.Configurations;

internal class NotificationConfiguration<P>(P project) : IEntityTypeConfiguration<Notification>
    where P : IProjectStringValue
{
    public P Project { get; } = project;

    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications", Project.Name);
        builder.Property(x => x.NotificationType)
            .HasConversion<string>()
            .IsRequired();
    }
}