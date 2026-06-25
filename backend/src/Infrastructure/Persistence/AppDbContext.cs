using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Oypa.Crm.Application.Common.Events;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Domain.Common;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Infrastructure.Identity;

namespace Oypa.Crm.Infrastructure.Persistence;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IDomainEventDispatcher dispatcher)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IUnitOfWork
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<MeetingNote> MeetingNotes => Set<MeetingNote>();
    public DbSet<SalesRep> SalesReps => Set<SalesRep>();
    public DbSet<MailDraft> MailDrafts => Set<MailDraft>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<GoalWeek> GoalWeeks => Set<GoalWeek>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Tender> Tenders => Set<Tender>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    /// <summary>Tüm denetim kayıtları. Sadece okunabilir; güncellenemez.</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Soft-delete global query filter'ı:
        // ISoftDelete uygulayan tüm entity type'larına otomatik olarak
        // "e.IsDeleted == false" koşulu eklenir. Include'larda da otomatik uygulanır.
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                continue;

            // Expression<Func<TEntity, bool>> e => !e.IsDeleted
            var param = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
            var prop = System.Linq.Expressions.Expression.Property(param, nameof(ISoftDelete.IsDeleted));
            var notDeleted = System.Linq.Expressions.Expression.Not(prop);
            var lambda = System.Linq.Expressions.Expression.Lambda(notDeleted, param);

            builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();

        var domainEvents = ChangeTracker.Entries<BaseEntity>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            entry.Entity.ClearDomainEvents();

        var result = await base.SaveChangesAsync(cancellationToken);

        // Olaylar yalnızca başarılı kalıcılaştırmadan sonra dağıtılır (audit/yan etki).
        await dispatcher.DispatchAsync(domainEvents, cancellationToken);

        return result;
    }

    private void ApplyAuditTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}
