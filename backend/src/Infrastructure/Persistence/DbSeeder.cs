using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Oypa.Crm.Infrastructure.Identity;

namespace Oypa.Crm.Infrastructure.Persistence;

/// <summary>Migration uygular ve başlangıç verilerini (rol, admin, örnek kayıtlar) oluşturur.</summary>
public static class DbSeeder
{
    public static readonly string[] Roles = ["Admin", "Sales"];

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var config = sp.GetRequiredService<IConfiguration>();
        var environment = sp.GetRequiredService<IHostEnvironment>();
        var logger = sp.GetRequiredService<ILogger<AppDbContext>>();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        var adminEmail = config["Seed:AdminEmail"] ?? "admin@oypa.com.tr";
        // Sabit parola fallback'i YALNIZCA Development'ta. Diğer ortamlarda
        // parola Seed:AdminPassword ile sağlanmazsa admin oluşturulmaz.
        var adminPassword = config["Seed:AdminPassword"]
            ?? (environment.IsDevelopment() ? "Admin!23456" : null);

        if (adminPassword is null)
        {
            logger.LogWarning(
                "Seed:AdminPassword sağlanmadığı için admin kullanıcı oluşturulmadı (ortam: {Env}).",
                environment.EnvironmentName);
        }
        else if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Sistem Yöneticisi",
                Position = "Administrator",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }

        // --- Varsayılan Kategoriler ---
        await SeedCategoriesAsync(db, logger);

        if (!await db.SalesReps.AnyAsync())
        {
            db.SalesReps.AddRange(
                new SalesRep("Helin Zin Ası", "hasi@oypa.com.tr"),
                new SalesRep("Hasan Aksoy", "hasan.aksoy@oypa.com"),
                new SalesRep("Ayşe Yılmaz", "ayse.yilmaz@oypa.com"));
        }

        if (!await db.Companies.AnyAsync())
        {
            db.Companies.Add(new Company(
                "Global Enerji A.Ş.", Sector.Energy, "0212 111 22 33", "info@global.com", "Ankara, Çankaya No:1"));
        }

        await db.SaveChangesAsync();

        // --- Organizasyon hiyerarşisi seed'i ---
        await SeedOrgAsync(db, userManager, logger);

        // --- SalesRep → Employee bağlantısı (e-posta eşleşmesiyle) ---
        await LinkSalesRepsToEmployeesAsync(db, logger);

        // --- Örnek Hedef seed'i ---
        await SeedGoalsAsync(db, logger);

        // --- Örnek bildirim seed'i ---
        await SeedNotificationsAsync(db, userManager, logger);

        // --- Örnek İhale seed'i ---
        await SeedTendersAsync(db, logger);

        // --- SalesRep'lere giriş hesabı (idempotent) ---
        await EnsureSalesRepLoginAccountsAsync(db, userManager, logger);
    }

    /// <summary>
    /// Belirli SalesRep'lere kimlik hesabı (giriş) ve Sales rolü verir; varsa atlar.
    /// Hesap oluşturulduktan sonra ilgili SalesRep, e-posta eşleşmesiyle Employee'ye
    /// bağlanır (notification/atama zinciri için) — eşleşen Employee yoksa yalnız giriş eklenir.
    /// </summary>
    private static async Task EnsureSalesRepLoginAccountsAsync(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger<AppDbContext> logger)
    {
        // Org seed parolasıyla aynı (demo). Production'da ilk girişten sonra değiştirilmeli.
        const string salesRepPassword = "Oypa!2026";

        (string Email, string FullName)[] accounts =
        [
            ("hasi@oypa.com.tr", "Helin Zin Ası"),
            ("hasan.aksoy@oypa.com", "Hasan Aksoy"),
            ("ayse.yilmaz@oypa.com", "Ayşe Yılmaz")
        ];

        foreach (var (email, fullName) in accounts)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing is not null)
            {
                // Hesap zaten varsa: demo parolasını bilinen değere sabitle ve
                // Sales rolünü garanti et (eski/farklı parolalı kayıtlarda da giriş çalışsın).
                // Not: AddIdentityCore default token provider eklemediği için
                // GeneratePasswordResetToken kullanılamaz; parola hash'i doğrudan set edilir.
                existing.PasswordHash = userManager.PasswordHasher.HashPassword(existing, salesRepPassword);
                var upd = await userManager.UpdateAsync(existing);
                if (!upd.Succeeded)
                {
                    logger.LogWarning(
                        "SalesRep login seed: {Email} parola güncellenemedi. Hatalar: {Errors}",
                        email,
                        string.Join("; ", upd.Errors.Select(e => e.Description)));
                }

                if (!await userManager.IsInRoleAsync(existing, "Sales"))
                    await userManager.AddToRoleAsync(existing, "Sales");

                continue;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                Position = "Satış Temsilcisi",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, salesRepPassword);
            if (!result.Succeeded)
            {
                logger.LogWarning(
                    "SalesRep login seed: {Email} için hesap oluşturulamadı. Hatalar: {Errors}",
                    email,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                continue;
            }

            await userManager.AddToRoleAsync(user, "Sales");
            logger.LogInformation("SalesRep login seed: {Email} (Sales) hesabı oluşturuldu.", email);
        }
    }

    /// <summary>
    /// Organizasyon hiyerarşisini ve bağlı kimlik hesaplarını idempotent olarak oluşturur.
    /// Employees tablosu boşsa çalışır.
    /// </summary>
    private static async Task SeedOrgAsync(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger<AppDbContext> logger)
    {
        if (await db.Set<Employee>().AnyAsync())
            return;

        const string orgPassword = "Oypa!2026";

        // --- Kök: Umur KUTLU (Direktör / Admin) ---
        var umur = new Employee("Pazarlama ve Satış Direktörü", "Umur KUTLU", "umur.kutlu@oypa.com.tr");
        await LinkAccountAsync(umur, "umur.kutlu@oypa.com.tr", "Umur KUTLU", orgPassword, "Admin", userManager, logger);
        db.Set<Employee>().Add(umur);
        await db.SaveChangesAsync(); // Id'ye ihtiyaç var

        // --- Müdürler (Umur'a bağlı / Admin) ---
        var avniye = new Employee("Satış Müdürü", "Avniye Belgin ÖNER", "avniye.oner@oypa.com.tr", umur.Id);
        await LinkAccountAsync(avniye, "avniye.oner@oypa.com.tr", "Avniye Belgin ÖNER", orgPassword, "Admin", userManager, logger);

        var halil = new Employee("Ticari Pazarlama Müdürü", "Halil Serdar KÜTÜKCÜ", "halil.kutukcu@oypa.com.tr", umur.Id);
        await LinkAccountAsync(halil, "halil.kutukcu@oypa.com.tr", "Halil Serdar KÜTÜKCÜ", orgPassword, "Admin", userManager, logger);

        db.Set<Employee>().AddRange(avniye, halil);
        await db.SaveChangesAsync(); // Id'ye ihtiyaç var

        // --- Avniye'ye bağlı personel ---
        var muhammed = new Employee("Satış Uzmanı", "Muhammed Safa MARANGOZ", "muhammed.marangoz@oypa.com.tr", avniye.Id);
        await LinkAccountAsync(muhammed, "muhammed.marangoz@oypa.com.tr", "Muhammed Safa MARANGOZ", orgPassword, "Sales", userManager, logger);

        var yigit = new Employee("Satış Uzmanı", "Yiğit ERSOY", "yigit.ersoy@oypa.com.tr", avniye.Id);
        await LinkAccountAsync(yigit, "yigit.ersoy@oypa.com.tr", "Yiğit ERSOY", orgPassword, "Sales", userManager, logger);

        var ismail = new Employee("Depo Şefi", "İsmail TAZECAN", "ismail.tazecan@oypa.com.tr", avniye.Id);
        await LinkAccountAsync(ismail, "ismail.tazecan@oypa.com.tr", "İsmail TAZECAN", orgPassword, "Sales", userManager, logger);

        // Hesapsız düğümler — Avniye'ye bağlı
        var turizmYon = new Employee("Turizm Satış ve İş Geliştirme Yöneticisi", managerId: avniye.Id);
        var satiOpsYon = new Employee("Satış Operasyonları ve Raporlama Yöneticisi", managerId: avniye.Id);
        var tesisYon = new Employee("Tesis Satış ve İş Geliştirme Yöneticisi", managerId: avniye.Id);
        var perakendeYon = new Employee("Perakende Satış Yöneticisi", managerId: avniye.Id);

        // --- Halil'e bağlı hesapsız düğümler ---
        var pazarArastirma = new Employee("Pazar Araştırma ve Analiz Yöneticisi", managerId: halil.Id);
        var pazarGelistirme = new Employee("Pazar Geliştirme ve Fiyatlandırma Yöneticisi", managerId: halil.Id);

        // --- Umur'a bağlı kalan düğümler ---
        var stajyer = new Employee("Stajyer", managerId: umur.Id);

        var saban = new Employee("Destek Personeli", "Şaban Ulukaya", "saban.ulukaya@oypa.com.tr", umur.Id);
        await LinkAccountAsync(saban, "saban.ulukaya@oypa.com.tr", "Şaban Ulukaya", orgPassword, "Sales", userManager, logger);

        db.Set<Employee>().AddRange(
            muhammed, yigit, ismail,
            turizmYon, satiOpsYon, tesisYon, perakendeYon,
            pazarArastirma, pazarGelistirme,
            stajyer, saban);

        await db.SaveChangesAsync();

        logger.LogInformation("Organizasyon hiyerarşisi seed'i tamamlandı. 14 personel oluşturuldu.");
    }

    /// <summary>
    /// SalesRep'leri e-posta adresi eşleşmesiyle Employee'ye bağlar.
    /// Eşleşme bulunamazsa EmployeeId null bırakılır.
    /// </summary>
    private static async Task LinkSalesRepsToEmployeesAsync(AppDbContext db, ILogger<AppDbContext> logger)
    {
        var reps = await db.SalesReps.Where(r => r.EmployeeId == null).ToListAsync();
        if (reps.Count == 0)
            return;

        var employees = await db.Set<Employee>()
            .Where(e => e.Email != null)
            .Select(e => new { e.Id, e.Email })
            .ToListAsync();

        var emailToEmployeeId = employees
            .Where(e => e.Email != null)
            .ToDictionary(e => e.Email!.ToLowerInvariant(), e => e.Id);

        int linked = 0;
        foreach (var rep in reps)
        {
            var emailKey = rep.Email.ToLowerInvariant();
            if (emailToEmployeeId.TryGetValue(emailKey, out var employeeId))
            {
                rep.LinkEmployee(employeeId);
                linked++;
            }
        }

        if (linked > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("SalesRep→Employee bağlantısı: {Count} temsilci bağlandı.", linked);
        }
    }

    /// <summary>
    /// Örnek Hedef seed'i: Umur'a atanan, Segment=All, WeeklyTarget=5.
    /// Idempotent (Goals tablosu boşsa çalışır).
    /// </summary>
    private static async Task SeedGoalsAsync(AppDbContext db, ILogger<AppDbContext> logger)
    {
        if (await db.Goals.AnyAsync())
            return;

        // Umur'u bul (kök direktör)
        var umur = await db.Set<Employee>()
            .FirstOrDefaultAsync(e => e.Email == "umur.kutlu@oypa.com.tr");

        if (umur is null)
        {
            logger.LogWarning("Goal seed: Umur KUTLU bulunamadı, örnek hedef oluşturulamadı.");
            return;
        }

        var goal = new Goal(umur.Id, GoalSegment.All, 5, "Haftalık Tüm Görüşmeler");
        db.Goals.Add(goal);
        await db.SaveChangesAsync();

        logger.LogInformation("Goal seed: Umur için örnek hedef oluşturuldu (Segment=All, WeeklyTarget=5).");
    }

    /// <summary>
    /// Hesaplı kullanıcılara "Hoş geldiniz" bildirimi oluşturur (idempotent).
    /// </summary>
    private static async Task SeedNotificationsAsync(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger<AppDbContext> logger)
    {
        if (await db.Notifications.AnyAsync())
            return;

        var employees = await db.Set<Employee>()
            .Where(e => e.ApplicationUserId != null)
            .Select(e => new { e.ApplicationUserId, e.FullName, e.Title })
            .ToListAsync();

        if (employees.Count == 0)
            return;

        foreach (var emp in employees)
        {
            db.Notifications.Add(new Notification(
                recipientUserId: emp.ApplicationUserId!.Value,
                message: "OYPA CRM sistemine hoş geldiniz! Bildirimler burada görünecek.",
                type: NotificationType.Manual,
                title: "Hoş Geldiniz"));
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Notification seed: {Count} kullanıcıya hoş geldiniz bildirimi oluşturuldu.", employees.Count);
    }

    /// <summary>
    /// Örnek ihaleler: farklı sektör/durum, birinin tarihi bugünden ~5 gün sonrası (bildirim demosu).
    /// Idempotent (Tenders tablosu boşsa çalışır).
    /// </summary>
    private static async Task SeedTendersAsync(AppDbContext db, ILogger<AppDbContext> logger)
    {
        if (await db.Tenders.AnyAsync())
            return;

        // Mevcut firma ve satış temsilcilerini al
        var company = await db.Companies.FirstOrDefaultAsync();
        if (company is null)
        {
            logger.LogWarning("Tender seed: Firma bulunamadı, örnek ihaleler oluşturulamadı.");
            return;
        }

        // Bildirim demo ihalesi için e-posta ile eşleşen bir rep bul
        var demoRep = await db.SalesReps
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.EmployeeId != null && r.Employee != null && r.Employee.ApplicationUserId != null);

        // Bugünden ~5 gün sonrası (demo: yaklaşan bildirim tetiklenir)
        var approachingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));

        var t1 = Tender.Create(
            company.Id,
            "Global Enerji Temizlik Hizmetleri İhalesi 2026",
            "TNR-2026-001",
            Sector.Energy,
            approachingDate,
            personnelCount: 45,
            estimatedValue: 1_250_000m,
            volume: 12_000m,
            quantity: 12,
            "Yıllık temizlik hizmet ihalesi. Bildirim demo için ~5 gün sonrası tarih.",
            assignedSalesRepId: demoRep?.Id);

        var t2 = Tender.Create(
            company.Id,
            "Otel Zinciri Yataklı Araç İhalesi",
            "TNR-2026-002",
            Sector.Tourism,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(45)),
            personnelCount: 20,
            estimatedValue: 850_000m,
            volume: null,
            quantity: 15,
            "Turizm sezonu öncesi araç kiralama ihalesi.",
            assignedSalesRepId: null);

        var t3 = Tender.Create(
            company.Id,
            "AVM Tesis Yönetimi Güvenlik Hizmetleri",
            "TNR-2025-090",
            Sector.FacilityManagement,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            personnelCount: 60,
            estimatedValue: 2_100_000m,
            volume: 24_000m,
            quantity: 24,
            "Tamamlanan ihale — kazanıldı.",
            assignedSalesRepId: demoRep?.Id);
        t3.ChangeStatus(TenderStatus.Kazanildi);

        db.Tenders.AddRange(t1, t2, t3);
        await db.SaveChangesAsync();

        logger.LogInformation("Tender seed: 3 örnek ihale oluşturuldu (biri ~5 gün sonrası, biri kazanıldı).");
    }

    /// <summary>
    /// Personel için kimlik hesabı oluşturur; hesap zaten varsa sadece linker.
    /// </summary>
    private static async Task LinkAccountAsync(
        Employee employee,
        string email,
        string fullName,
        string password,
        string role,
        UserManager<ApplicationUser> userManager,
        ILogger<AppDbContext> logger)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            employee.LinkAccount(existing.Id);
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            Position = employee.Title,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogWarning(
                "Org seed: {Email} için hesap oluşturulamadı. Hatalar: {Errors}",
                email,
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, role);
        employee.LinkAccount(user.Id);

        logger.LogInformation("Org seed: {Email} ({Role}) hesabı oluşturuldu.", email, role);
    }

    /// <summary>
    /// Varsayılan kategorileri oluşturur. Her kategori ad bazında kontrol edilir (idempotent).
    /// </summary>
    private static async Task SeedCategoriesAsync(AppDbContext db, ILogger<AppDbContext> logger)
    {
        (string Name, string Color)[] defaults =
        [
            ("Kurumsal", "#3b82f6"),
            ("KOBİ",     "#22c55e"),
            ("Kamu",     "#f59e0b"),
            ("Öncelikli","#ef4444")
        ];

        int added = 0;
        foreach (var (name, color) in defaults)
        {
            var exists = await db.Categories
                .IgnoreQueryFilters() // silinmiş olanlar da dahil — isim benzersizliği için
                .AnyAsync(c => c.Name == name);

            if (exists)
                continue;

            db.Categories.Add(new Category(name, color));
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Category seed: {Count} varsayılan kategori oluşturuldu.", added);
        }
    }
}
