using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Contracts.Subscription;
using UserService.Infrastructure.EventHandlers;

namespace UserService.Infrastructure.Extensions;

/// <summary>
/// Extension methods for subscribing to subscription-related events
/// </summary>
public static class SubscriptionEventSubscriptionExtension
{
    /// <summary>
    /// Subscribe to Subscription Plan events from SubscriptionService
    /// </summary>
    public static void SubscribeToSubscriptionEvents(this IServiceProvider serviceProvider)
    {
        var eventBus = serviceProvider.GetRequiredService<IEventBus>();
        
        // Subscribe to SubscriptionPlan events for User Activity Logging
        eventBus.Subscribe<SubscriptionPlanCreatedEvent, SubscriptionPlanCreatedEventHandler>();
        eventBus.Subscribe<SubscriptionPlanUpdatedEvent, SubscriptionPlanUpdatedEventHandler>();
        eventBus.Subscribe<SubscriptionPlanDeletedEvent, SubscriptionPlanDeletedEventHandler>();
        
        // Event bus StartConsuming() is already called in SubscribeToAuthEvents
        // No need to call it again
    }
}
