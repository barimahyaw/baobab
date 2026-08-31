using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Baobab.SharedKernel.Application.Abstractions.Services;
using Baobab.SharedKernel.Domain.ValueObjects;
using Baobab.SharedKernel.Domain.Primitives;
using Baobab.SharedKernel.Persistence.Audits;

namespace Baobab.SharedKernel.Persistence.Contexts;

public class AuditableContext<TDbContext>
    (DbContextOptions options, ICurrentUserService currentUserService, ILogger<TDbContext> logger)
    : DbContext(options)
    where TDbContext : DbContext
{
    public DbSet<Audit> AuditTrail { get; set; } = null!;

    public virtual async Task<int> SaveChangesAsync(Ulid userId)
    {
        var auditEntries = AuditHelper.OnBeforeSaveChanges(ChangeTracker, userId, AuditTrail);
        var result = await base.SaveChangesAsync();
        await AuditHelper.OnAfterSaveChanges(ChangeTracker, auditEntries, AuditTrail);
        return result;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        var userId = currentUserService.UserId;

        if ((userId == Ulid.Empty || userId == default)
            && !Ulid.TryParse(Environment.GetEnvironmentVariable("SYS_ADMIN_ID"), out userId))
        {
            logger.LogError("Save Changes operation failed due to missing SYS_ADMIN_ID environment variable.");
            throw new InvalidOperationException("SYS_ADMIN_ID environment variable is missing.");
        }

        if (UserId.Validate(userId) is var result && !result.Succeeded)
        {
            logger.LogError($"Save Changes operation failed due to invalid User ID: {userId}. Error: {result.Messages}");
            throw new InvalidOperationException($"Invalid User ID: {userId}. Error: {result.Messages}");
        }

        foreach (var entry in ChangeTracker.Entries<EntityExtra>().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = entry.Entity.CreatedAtUtc == DateTime.MinValue
                        ? DateTime.UtcNow
                        : entry.Entity.CreatedAtUtc;
                    entry.Entity.CreatedUserId = UserId.Create(userId);
                    entry.Entity.IsActive = true;
                    break;

                case EntityState.Modified:
                    entry.Entity.LastModifiedAtUtc = DateTime.UtcNow;
                    entry.Entity.LastModifiedUserId = UserId.Create(userId);
                    break;
            }
        }

        if (currentUserService.UserId == Ulid.Empty || currentUserService.UserId == default)
            return await base.SaveChangesAsync(cancellationToken);

        return await SaveChangesAsync(currentUserService.UserId);
    }
}
