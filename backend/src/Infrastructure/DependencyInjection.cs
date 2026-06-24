using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oypa.Crm.Application.Common.Events;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Common.Options;
using Oypa.Crm.Application.Features.Employees;
using Oypa.Crm.Infrastructure.Email;
using Oypa.Crm.Infrastructure.Events;
using Oypa.Crm.Infrastructure.Features.Employees;
using Oypa.Crm.Infrastructure.Features.Org;
using Oypa.Crm.Infrastructure.Identity;
using Oypa.Crm.Infrastructure.Persistence;
using Oypa.Crm.Infrastructure.Persistence.Repositories;
using Oypa.Crm.Infrastructure.Reports;
using Oypa.Crm.Infrastructure.Security;

namespace Oypa.Crm.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IMeetingRepository, MeetingRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ITenderRepository, TenderRepository>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IOrgScopeService, OrgScopeService>();
        services.AddScoped<IReportService, ExcelReportService>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));

        // E-posta yapılandırması: Email:Host doluysa gerçek SMTP, boşsa NullEmailSender (dev ortamı).
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        var emailHost = configuration[$"{EmailOptions.SectionName}:Host"];
        if (!string.IsNullOrWhiteSpace(emailHost))
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        else
            services.AddScoped<IEmailSender, NullEmailSender>();

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;

                // Brute-force koruması: 5 başarısız girişte hesap 5 dk kilitlenir.
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            // Parola sıfırlama token'ı üretimi için varsayılan token sağlayıcıları eklenir.
            .AddDefaultTokenProviders();

        return services;
    }
}
