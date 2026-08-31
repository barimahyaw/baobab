using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Baobab.SharedKernel.Application.Abstractions.Services;
using Baobab.SharedKernel.Domain.ValueObjects;
using Baobab.SharedKernel.Domain.Primitives;

namespace Baobab.SharedKernel.Persistence.Audits.Contexts;

public class AuditableContext<T>(DbContextOptions options, ICurrentUserService currentUserService, ILogger<T> logger)
    : DbContext(options) where T : DbContext
{
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<T> _logger = logger;

    public DbSet<Audit> AuditTrail { get; set; } = null!;

    public virtual async Task<int> SaveChangesAsync(Guid userId)
    {
        var auditEntries = OnBeforeSaveChanges(userId);
        var result = await base.SaveChangesAsync();
        await OnAfterSaveChanges(auditEntries);
        return result;
    }

    private List<AuditEntry> OnBeforeSaveChanges(Guid userId)
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is Audit || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var auditEntry = new AuditEntry(entry)
            {
                TableName = entry.Entity.GetType().Name,
                UserId = userId
            };
            auditEntries.Add(auditEntry);
            foreach (var property in entry.Properties)
            {
                if (property.IsTemporary)
                {
                    auditEntry.TemporaryProperties.Add(property);
                    continue;
                }

                string propertyName = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[propertyName] = property.CurrentValue!;
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.AuditType = AuditType.Create;
                        auditEntry.NewValues[propertyName] = property.CurrentValue!;
                        break;

                    case EntityState.Deleted:
                        auditEntry.AuditType = AuditType.Delete;
                        auditEntry.OldValues[propertyName] = property.OriginalValue!;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            auditEntry.ChangedColumns.Add(propertyName);
                            auditEntry.AuditType = AuditType.Update;
                            auditEntry.OldValues[propertyName] = property.OriginalValue!;
                            auditEntry.NewValues[propertyName] = property.CurrentValue!;
                        }
                        break;
                }
            }
        }
        foreach (var auditEntry in auditEntries.Where(_ => !_.HasTemporaryProperties))
        {
            AuditTrail.Add(auditEntry.ToAudit());
        }
        return [.. auditEntries.Where(_ => _.HasTemporaryProperties)];
    }

    private Task OnAfterSaveChanges(List<AuditEntry> auditEntries)
    {
        if (auditEntries == null || auditEntries.Count == 0)
            return Task.CompletedTask;

        foreach (var auditEntry in auditEntries)
        {
            foreach (var prop in auditEntry.TemporaryProperties)
            {
                if (prop.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue!;
                }
                else
                {
                    auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue!;
                }
            }
            AuditTrail.Add(auditEntry.ToAudit());
        }
        return SaveChangesAsync();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        var userId = _currentUserService.UserId;
        var isBackgroundJob = userId == Guid.Empty || userId == default;

        // Try to get system admin ID from environment variable or configuration
        if (isBackgroundJob)
        {
            var systemAdminIdStr = Environment.GetEnvironmentVariable("SYSTEM_ADMIN_ID");
            if (string.IsNullOrWhiteSpace(systemAdminIdStr))
            {
                _logger.LogError("Save Changes operation failed: both user context and SYSTEM_ADMIN_ID environment variable are unavailable. " +
                    "Background jobs require SYSTEM_ADMIN_ID to be set for audit purposes.");
                throw new InvalidOperationException("Cannot save changes without user context or SYSTEM_ADMIN_ID");
            }

            if (!Guid.TryParse(systemAdminIdStr, out userId))
            {
                _logger.LogError("Save Changes operation failed: SYSTEM_ADMIN_ID '{SystemAdminId}' is not a valid GUID format", systemAdminIdStr);
                throw new InvalidOperationException($"SYSTEM_ADMIN_ID '{systemAdminIdStr}' is not a valid GUID");
            }

            _logger.LogInformation("Using SYSTEM_ADMIN_ID '{SystemAdminId}' for background job audit context", userId);
        }

        var userIdValidationResult = UserId.Validate(userId);
        if (!userIdValidationResult.Succeeded)
        {
            var errorMessage = $"Save Changes operation failed: Invalid user Id '{userId}'. Validation errors: {string.Join(", ", userIdValidationResult.Messages.Select(m => m.Message))}";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        var userIdResult = UserId.Create(userId);

        // Set audit fields for EntityExtra entities
        foreach (var entry in ChangeTracker.Entries<EntityExtra>().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = entry.Entity.CreatedAtUtc != DateTime.MinValue
                        ? entry.Entity.CreatedAtUtc
                        : DateTime.UtcNow;
                    entry.Entity.CreatedUserId = userIdResult;
                    entry.Entity.IsActive = true;

                    _logger.LogInformation("Setting audit fields for new {EntityType}: CreatedUserId={UserId}, CreatedAtUtc={CreatedAt}",
                        entry.Entity.GetType().Name, userIdResult, entry.Entity.CreatedAtUtc);
                    break;

                case EntityState.Modified:
                    entry.Entity.LastModifiedAtUtc = DateTime.UtcNow;
                    entry.Entity.LastModifiedUserId = userIdResult;

                    _logger.LogInformation("Setting audit fields for modified {EntityType}: LastModifiedUserId={UserId}, LastModifiedAtUtc={ModifiedAt}",
                        entry.Entity.GetType().Name, userIdResult, entry.Entity.LastModifiedAtUtc);
                    break;
            }
        }

        // For background jobs without HTTP context, use base SaveChangesAsync
        if (isBackgroundJob)
            return await base.SaveChangesAsync(cancellationToken);

        // For user-initiated requests, use audit-enabled SaveChangesAsync
        return await SaveChangesAsync(_currentUserService.UserId);
    }
}
