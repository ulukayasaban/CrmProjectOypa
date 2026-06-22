using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Oypa.Crm.Infrastructure.Identity;
using Oypa.Crm.Infrastructure.Persistence;

namespace Oypa.Crm.IntegrationTests;

/// <summary>
/// Integration test host: "Testing" ortamında çalışır (Program seed/migration atlar),
/// gerçek SQL Server DbContext kaydını kaldırıp InMemory sağlayıcı ekler ve
/// test verisini (rol + admin kullanıcı) elle oluşturur.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@oypa.com.tr";
    public const string AdminPassword = "Admin!23456";
    private const string DbName = "oypa-tests";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting değerleri en yüksek öncelikli config kaynağına yazılır; böylece
        // Program.cs içinde builder.Configuration okunduğunda kesinlikle görünür.
        builder.UseSetting("Jwt:Secret", "integration-test-secret-key-which-is-long-enough-32+");
        builder.UseSetting("Jwt:Issuer", "OypaCrm");
        builder.UseSetting("Jwt:Audience", "OypaCrmClient");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=test;Database=test;");

        builder.ConfigureTestServices(services =>
        {
            // Gerçek SQL Server DbContext seçenek kaydını kaldır.
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(AppDbContext))
                .ToList();
            foreach (var d in toRemove)
                services.Remove(d);

            // InMemory provider'ı izole bir iç servis sağlayıcıyla kaydet; böylece
            // uygulamanın container'ında kalan SqlServer provider servisleriyle çakışmaz.
            var efServiceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<AppDbContext>(o => o
                .UseInMemoryDatabase(DbName)
                .UseInternalServiceProvider(efServiceProvider));
        });
    }

    /// <summary>DB oluşturur, rolleri ve admin kullanıcıyı seed eder (Testing'de otomatik seed yoktur).</summary>
    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in new[] { "Admin", "Sales" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(AdminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                FullName = "Test Yöneticisi",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, AdminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}
