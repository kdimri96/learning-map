using LearningMap.Application.Interfaces.Services;
using LearningMap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LearningMap.Infrastructure.Data;

/// <summary>
/// EF Core interceptor: runs just before every SaveChanges and stamps Created*/Modified*.
/// SafeSpace does the same thing by overriding SaveChangesAsync in its DbContext;
/// an interceptor keeps the DbContext clean and can receive services (current user, clock) via DI.
/// </summary>
public class AuditableEntityInterceptor(ICurrentUserService currentUser, TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        StampAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        StampAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void StampAuditFields(DbContext? context)
    {
        if (context is null) return;

        var now = timeProvider.GetUtcNow();
        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedBy = currentUser.UserId;
                entry.Entity.CreatedDate = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedBy = currentUser.UserId;
                entry.Entity.ModifiedDate = now;
                // Never let an update overwrite who/when the row was created.
                entry.Property(e => e.CreatedBy).IsModified = false;
                entry.Property(e => e.CreatedDate).IsModified = false;
            }
        }
    }
}
