using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Commons.Interfaces;
using NotificationService.Application.Commons.Models;
using NotificationService.Infrastructure.EventHandlers;
using NotificationService.Infrastructure.Factories;
using NotificationService.Infrastructure.Services;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Commons.Models.Otp;
using SharedLibrary.Contracts.Auth;

namespace NotificationService.Infrastructure;

public static class NotiInfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register infrastructure services here
        services.AddHttpClient();
        
        // Add Memory Cache for idempotency handling
        services.AddMemoryCache();

        // Configure settings using Options pattern
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
        services.Configure<OtpSettings>(configuration.GetSection("OtpSettings"));

        services.AddScoped<INotificationFactory, NotificationFactory>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IFirebaseService, FirebaseService>();

        // Register event handlers
        services.AddScoped<IIntegrationEventHandler<ResetPasswordEvent>, SendOtpResetPasswordEventHandler>();

        return services;
    }
}