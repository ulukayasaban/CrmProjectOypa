using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Oypa.Crm.Application.Common.Events;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Common.Services;
using Oypa.Crm.Application.Features.Auth;
using Oypa.Crm.Application.Features.Companies;
using Oypa.Crm.Application.Features.Contacts;
using Oypa.Crm.Application.Features.Dashboard;
using Oypa.Crm.Application.Features.Employees;
using Oypa.Crm.Application.Features.Events;
using Oypa.Crm.Application.Features.Goals;
using Oypa.Crm.Application.Features.MailDrafts;
using Oypa.Crm.Application.Features.Meetings;
using Oypa.Crm.Application.Features.Notifications;
using Oypa.Crm.Application.Features.SalesReps;
using Oypa.Crm.Application.Features.Tenders;
using Oypa.Crm.Domain.Events;

namespace Oypa.Crm.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<IMeetingService, MeetingService>();
        services.AddScoped<ISalesRepService, SalesRepService>();
        services.AddScoped<IMailDraftService, MailDraftService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IGoalService, GoalService>();
        services.AddScoped<ITenderService, TenderService>();
        services.AddScoped<ITenderReminderService, TenderReminderService>();

        // Domain event handler'ları — her olay için bağımsız kayıt (çoklu handler desteği)
        services.AddScoped<IDomainEventHandler<MeetingScheduledEvent>, MeetingScheduledAuditHandler>();
        services.AddScoped<IDomainEventHandler<MeetingScheduledEvent>, MeetingScheduledNotificationHandler>();

        services.AddScoped<IDomainEventHandler<LeadConvertedToCustomerEvent>, LeadConvertedAuditHandler>();
        services.AddScoped<IDomainEventHandler<LeadConvertedToCustomerEvent>, LeadConvertedNotificationHandler>();

        return services;
    }
}
