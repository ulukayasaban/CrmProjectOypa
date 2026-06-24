using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Oypa.Crm.Infrastructure.Persistence;
using Oypa.Crm.Infrastructure.Persistence.Interceptors;
using Shouldly;

namespace Oypa.Crm.IntegrationTests;

/// <summary>
/// Soft-delete ve AuditLog interceptor doğrulama testleri.
/// InMemory provider kullanılır; global query filter InMemory'de de çalışır.
/// </summary>
public sealed class SoftDeleteAuditTests
{
    // ────────────────────────────────────────────────────────────────────────
    // Yardımcı: izole InMemory AppDbContext oluşturur (interceptor olmadan)
    // ────────────────────────────────────────────────────────────────────────

    private static AppDbContext BuildContext(string dbName)
    {
        var dispatcherMock = Substitute.For<Oypa.Crm.Application.Common.Events.IDomainEventDispatcher>();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options, dispatcherMock);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Yardımcı: AuditSaveChangesInterceptor'lı InMemory context
    // ────────────────────────────────────────────────────────────────────────

    private static (AppDbContext Context, ICurrentUser CurrentUser) BuildContextWithAudit(string dbName)
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns((Guid?)Guid.Parse("11111111-1111-1111-1111-111111111111"));
        currentUser.Email.Returns("test@oypa.com.tr");
        currentUser.IsAuthenticated.Returns(true);

        var interceptor = new AuditSaveChangesInterceptor(currentUser);

        var dispatcherMock = Substitute.For<Oypa.Crm.Application.Common.Events.IDomainEventDispatcher>();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(interceptor)
            .Options;

        return (new AppDbContext(options, dispatcherMock), currentUser);
    }

    // ========================================================================
    // SOFT-DELETE TESTLERİ
    // ========================================================================

    /// <summary>
    /// Bir Tender soft-delete sonrası normal sorgudan gizlenmeli,
    /// ama DB'de IsDeleted=true olarak kalmalıdır.
    /// </summary>
    [Fact]
    public async Task SoftDelete_Tender_HiddenFromQueryButExistsInDb()
    {
        var dbName = $"soft-delete-tender-{Guid.NewGuid():N}";
        var now = new DateTime(2026, 6, 24, 10, 0, 0, DateTimeKind.Utc);

        // Firma ve ihale oluştur
        await using (var ctx = BuildContext(dbName))
        {
            var company = new Company("Test Firma", Sector.Retail, "111", "t@t.com", "Adres");
            ctx.Companies.Add(company);
            await ctx.SaveChangesAsync();

            var tender = Tender.Create(company.Id, "Test İhalesi", null, Sector.Retail,
                new DateOnly(2026, 12, 1), null, null, null, null, null, null);
            ctx.Tenders.Add(tender);
            await ctx.SaveChangesAsync();

            // Soft-delete uygula
            tender.MarkDeleted(now);
            ctx.Tenders.Update(tender);
            await ctx.SaveChangesAsync();
        }

        // Yeni context ile oku — global query filter aktif
        await using (var ctx = BuildContext(dbName))
        {
            // Filtrelenmiş sorgu: silinmiş ihale görünmemeli
            var visibleTenders = await ctx.Tenders.ToListAsync();
            visibleTenders.ShouldBeEmpty("Soft-delete sonrası ihale listelenmiş sorgudan gizlenmeli");

            // Filtre devre dışı bırakılarak (IgnoreQueryFilters) doğrudan DB'yi kontrol et
            var rawTenders = await ctx.Tenders.IgnoreQueryFilters().ToListAsync();
            rawTenders.Count.ShouldBe(1, "İhale DB'de fiziksel olarak kalmalı");
            rawTenders[0].IsDeleted.ShouldBeTrue("IsDeleted true olmalı");
            rawTenders[0].DeletedAtUtc.ShouldBe(now);
        }
    }

    /// <summary>
    /// Bir Company soft-delete sonrası Company listesinden gizlenmelidir.
    /// </summary>
    [Fact]
    public async Task SoftDelete_Company_HiddenFromListQuery()
    {
        var dbName = $"soft-delete-company-{Guid.NewGuid():N}";
        var now = DateTime.UtcNow;

        await using (var ctx = BuildContext(dbName))
        {
            var company = new Company("Silinecek Firma", Sector.Energy, "222", "e@e.com", "Adres");
            ctx.Companies.Add(company);
            await ctx.SaveChangesAsync();

            company.MarkDeleted(now);
            ctx.Companies.Update(company);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = BuildContext(dbName))
        {
            // Filtrelenmiş liste boş olmalı
            var visible = await ctx.Companies.ToListAsync();
            visible.ShouldBeEmpty("Soft-delete sonrası firma listeden gizlenmeli");

            // Ham kayıt DB'de mevcut
            var raw = await ctx.Companies.IgnoreQueryFilters().ToListAsync();
            raw.Count.ShouldBe(1);
            raw[0].IsDeleted.ShouldBeTrue();
        }
    }

    /// <summary>
    /// Bir Contact soft-delete sonrası hem tekil get hem listeden gizlenmelidir.
    /// </summary>
    [Fact]
    public async Task SoftDelete_Contact_HiddenFromQueryAndById()
    {
        var dbName = $"soft-delete-contact-{Guid.NewGuid():N}";
        var now = DateTime.UtcNow;
        Guid contactId;

        await using (var ctx = BuildContext(dbName))
        {
            var company = new Company("Firma", Sector.Retail, "111", "f@f.com", "Adres");
            ctx.Companies.Add(company);
            await ctx.SaveChangesAsync();

            var contact = new Contact(company.Id, "Ali Veli", "ali@v.com", "555");
            ctx.Contacts.Add(contact);
            await ctx.SaveChangesAsync();
            contactId = contact.Id;

            contact.MarkDeleted(now);
            ctx.Contacts.Update(contact);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = BuildContext(dbName))
        {
            // FirstOrDefaultAsync global query filter'a uyar → null dönmeli
            var found = await ctx.Contacts.FirstOrDefaultAsync(c => c.Id == contactId);
            found.ShouldBeNull("Soft-delete sonrası FirstOrDefaultAsync null dönmeli");

            // Liste de boş olmalı
            var list = await ctx.Contacts.ToListAsync();
            list.ShouldBeEmpty();

            // IgnoreQueryFilters ile DB'de mevcut
            var raw = await ctx.Contacts.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == contactId);
            raw.ShouldNotBeNull();
            raw!.IsDeleted.ShouldBeTrue();
        }
    }

    /// <summary>
    /// Soft-delete uygulanan Employee Restore() ile geri yüklenebilmeli.
    /// </summary>
    [Fact]
    public async Task SoftDelete_Employee_RestoreWorksCorrectly()
    {
        var dbName = $"soft-delete-employee-restore-{Guid.NewGuid():N}";
        var now = DateTime.UtcNow;
        Guid employeeId;

        await using (var ctx = BuildContext(dbName))
        {
            var emp = new Employee("Yazılım Geliştirici", "Saban U.", "s@oypa.com");
            ctx.Employees.Add(emp);
            await ctx.SaveChangesAsync();
            employeeId = emp.Id;

            emp.MarkDeleted(now);
            ctx.Employees.Update(emp);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = BuildContext(dbName))
        {
            // Gizli olmalı
            var hidden = await ctx.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);
            hidden.ShouldBeNull("Soft-delete sonrası personel gizlenmeli");

            // Geri yükle
            var raw = await ctx.Employees.IgnoreQueryFilters().FirstAsync(e => e.Id == employeeId);
            raw.Restore();
            ctx.Employees.Update(raw);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = BuildContext(dbName))
        {
            // Artık görünür olmalı
            var restored = await ctx.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);
            restored.ShouldNotBeNull("Restore sonrası personel tekrar görünür olmalı");
            restored!.IsDeleted.ShouldBeFalse();
            restored.DeletedAtUtc.ShouldBeNull();
        }
    }

    // ========================================================================
    // AUDİT LOG TESTLERİ
    // ========================================================================

    /// <summary>
    /// Yeni entity oluşturma sonrası AuditLog.Created kaydı oluşmalıdır.
    /// </summary>
    [Fact]
    public async Task AuditLog_WhenEntityCreated_LogsCreatedAction()
    {
        var dbName = $"audit-created-{Guid.NewGuid():N}";
        var (ctx, _) = BuildContextWithAudit(dbName);
        await using (ctx)
        {
            var company = new Company("Yeni Firma", Sector.Retail, "111", "n@n.com", "Adres");
            ctx.Companies.Add(company);
            await ctx.SaveChangesAsync();

            var logs = await ctx.AuditLogs.ToListAsync();
            logs.ShouldNotBeEmpty("Create işleminde AuditLog oluşmalı");

            var createLog = logs.FirstOrDefault(l =>
                l.EntityName == nameof(Company) &&
                l.Action == Domain.Enums.AuditAction.Created);
            createLog.ShouldNotBeNull("Company Created audit kaydı olmalı");
            createLog!.EntityId.ShouldBe(company.Id.ToString());
            createLog.UserId.ShouldBe(Guid.Parse("11111111-1111-1111-1111-111111111111"));
            createLog.UserName.ShouldBe("test@oypa.com.tr");
        }
    }

    /// <summary>
    /// Entity güncellemesi sonrası AuditLog.Updated kaydı oluşmalıdır.
    /// </summary>
    [Fact]
    public async Task AuditLog_WhenEntityUpdated_LogsUpdatedAction()
    {
        var dbName = $"audit-updated-{Guid.NewGuid():N}";
        var (ctx, _) = BuildContextWithAudit(dbName);
        await using (ctx)
        {
            // Önce oluştur
            var tender = Tender.Create(Guid.NewGuid(), "Başlık", null, Sector.Retail,
                new DateOnly(2026, 12, 1), null, null, null, null, null, null);
            ctx.Tenders.Add(tender);
            await ctx.SaveChangesAsync();

            // Şimdi güncelle
            tender.ChangeStatus(Domain.Enums.TenderStatus.TeklifVerildi);
            ctx.Tenders.Update(tender);
            await ctx.SaveChangesAsync();

            var logs = await ctx.AuditLogs
                .Where(l => l.EntityName == nameof(Tender) && l.Action == Domain.Enums.AuditAction.Updated)
                .ToListAsync();

            logs.ShouldNotBeEmpty("Güncelleme sonrası Updated audit kaydı olmalı");
            logs.Any(l => l.EntityId == tender.Id.ToString()).ShouldBeTrue();
        }
    }

    /// <summary>
    /// Soft-delete işlemi sonrası AuditLog.Deleted kaydı oluşmalıdır.
    /// </summary>
    [Fact]
    public async Task AuditLog_WhenEntitySoftDeleted_LogsDeletedAction()
    {
        var dbName = $"audit-softdelete-{Guid.NewGuid():N}";
        var (ctx, _) = BuildContextWithAudit(dbName);
        await using (ctx)
        {
            // Tender oluştur
            var tender = Tender.Create(Guid.NewGuid(), "Silinecek İhale", null, Sector.Energy,
                new DateOnly(2026, 12, 1), null, null, null, null, null, null);
            ctx.Tenders.Add(tender);
            await ctx.SaveChangesAsync();

            // Soft-delete
            tender.MarkDeleted(DateTime.UtcNow);
            ctx.Tenders.Update(tender);
            await ctx.SaveChangesAsync();

            // AuditLog'dan Deleted aksiyonu ara
            var deleteLogs = await ctx.AuditLogs
                .Where(l =>
                    l.EntityName == nameof(Tender) &&
                    l.EntityId == tender.Id.ToString() &&
                    l.Action == Domain.Enums.AuditAction.Deleted)
                .ToListAsync();

            deleteLogs.ShouldNotBeEmpty("Soft-delete sonrası Deleted audit kaydı oluşmalı");
        }
    }

    /// <summary>
    /// AuditLog kendi kendini (sonsuz döngü) audit etmemeli.
    /// </summary>
    [Fact]
    public async Task AuditLog_DoesNotAuditItself_NoInfiniteLoop()
    {
        var dbName = $"audit-noloop-{Guid.NewGuid():N}";
        var (ctx, _) = BuildContextWithAudit(dbName);
        await using (ctx)
        {
            var company = new Company("Loop Test", Sector.Retail, "111", "l@l.com", "Adres");
            ctx.Companies.Add(company);
            await ctx.SaveChangesAsync();

            // AuditLog kayıtlarının kendilerini audit etmemeli;
            // eğer audit et→ audit et zinciri oluşsaydı çok sayıda kayıt olurdu.
            var auditLogs = await ctx.AuditLogs.ToListAsync();
            auditLogs.Any(l => l.EntityName == nameof(AuditLog))
                .ShouldBeFalse("AuditLog entity'si kendi kaydını denetlememeli (döngü önleme)");
        }
    }

    /// <summary>
    /// RefreshToken audit'lenmemeli (teknik/düşük değerli entity).
    /// </summary>
    [Fact]
    public async Task AuditLog_DoesNotAuditRefreshToken()
    {
        var dbName = $"audit-norefreshtoken-{Guid.NewGuid():N}";
        var (ctx, _) = BuildContextWithAudit(dbName);
        await using (ctx)
        {
            // RefreshToken oluştur — şema olmadan InMemory'de bağımsız eklenebilir
            var token = new RefreshToken(
                Guid.NewGuid(),
                "hash-" + Guid.NewGuid().ToString("N"),
                DateTime.UtcNow.AddDays(7),
                createdByIp: null);
            ctx.RefreshTokens.Add(token);
            await ctx.SaveChangesAsync();

            var logs = await ctx.AuditLogs.ToListAsync();
            logs.Any(l => l.EntityName == nameof(RefreshToken))
                .ShouldBeFalse("RefreshToken audit dışı tutulmalı");
        }
    }
}
