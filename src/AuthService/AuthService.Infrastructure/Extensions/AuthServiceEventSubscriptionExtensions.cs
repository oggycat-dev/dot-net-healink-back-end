using AuthService.Infrastructure.EventHandlers;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Commons.EventHandlers;
using SharedLibrary.SharedLibrary.Contracts.Events;

namespace AuthService.Infrastructure.Extensions;

public static class AuthServiceEventSubscriptionExtensions
{
    /// <summary>
    /// Subscribe to auth events with custom handlers for AuthService
    /// Uses AuthUserRolesChangedEventHandler instead of SharedLibrary's UserRolesChangedEventHandler
    /// </summary>
    public static void SubscribeToAuthServiceEvents(this IServiceProvider serviceProvider)
    {
        var eventBus = serviceProvider.GetRequiredService<IEventBus>();
        
        // Subscribe to shared auth events
        eventBus.Subscribe<UserLoggedInEvent, UserLoggedInEventHandler>();
        eventBus.Subscribe<UserLoggedOutEvent, UserLoggedOutEventHandler>();
        eventBus.Subscribe<UserStatusChangedEvent, UserStatusChangedEventHandler>();
        eventBus.Subscribe<RefreshTokenRevokedEvent, RefreshTokenRevokedEventHandler>();
        eventBus.Subscribe<UserSessionsInvalidatedEvent, UserSessionsInvalidatedEventHandler>();
        
        // Subscribe Auth-specific handler for UserRolesChangedEvent
        // This handler updates BOTH database AND cache (not just cache like SharedLibrary handler)
        eventBus.Subscribe<UserRolesChangedEvent, AuthUserRolesChangedEventHandler>();
    }
}
