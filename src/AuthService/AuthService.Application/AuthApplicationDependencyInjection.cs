using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ProductAuthMicroservice.Commons.DependencyInjection;
using ProductAuthMicroservice.Commons.EventBus;
using ProductAuthMicroservice.AuthService.Application.Features.EventHandlers.ProductEventHandlers;
using ProductAuthMicroservice.Shared.Contracts.Events;

namespace AuthService.Application;

public static class AuthApplicationDependencyInjection
{
    /// <summary>
    /// Add application services for AuthService
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Add MediatR with shared behaviors
        services.AddMediatRWithBehaviors(Assembly.GetExecutingAssembly());
        
        // Add AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        
        // Register Event Handlers
        services.AddScoped<IIntegrationEventHandler<ProductCreatedEvent>, ProductCreatedEventHandler>();
        services.AddScoped<IIntegrationEventHandler<ProductUpdatedEvent>, ProductUpdatedEventHandler>();
        services.AddScoped<IIntegrationEventHandler<ProductInventoryCreatedEvent>, ProductInventoryCreatedEventHandler>();
        
        // Add Category event handlers
        services.AddScoped<IIntegrationEventHandler<CategoryCreatedEvent>, ProductAuthMicroservice.AuthService.Application.Features.EventHandlers.CategoryEventHandlers.CategoryCreatedEventHandler>();
        services.AddScoped<IIntegrationEventHandler<CategoryUpdatedEvent>, ProductAuthMicroservice.AuthService.Application.Features.EventHandlers.CategoryEventHandlers.CategoryUpdatedEventHandler>();
        services.AddScoped<IIntegrationEventHandler<CategoryDeletedEvent>, ProductAuthMicroservice.AuthService.Application.Features.EventHandlers.CategoryEventHandlers.CategoryDeletedEventHandler>();
        
        return services;
    }
}
