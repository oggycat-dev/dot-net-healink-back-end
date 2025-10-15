using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using SubscriptionService.Infrastructure.Saga;
using SharedLibrary.Contracts.Subscription.Events;
using SharedLibrary.Contracts.Payment.Events;
using System.Data;

namespace SubscriptionService.Infrastructure.Configurations;

/// <summary>
/// SubscriptionService-specific Saga configuration
/// Configures RegisterSubscription Saga with MassTransit Entity Framework Outbox
/// Reference: https://masstransit.io/documentation/configuration/middleware/outbox
/// </summary>
public static class SubscriptionSagaConfiguration
{
    /// <summary>
    /// Configure RegisterSubscription Saga for SubscriptionService
    /// </summary>
    public static void ConfigureRegisterSubscriptionSaga<TDbContext>(IRegistrationConfigurator configurator)
        where TDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        // Add RegisterSubscription Saga with optimized configuration
        configurator.AddSagaStateMachine<RegisterSubscriptionSaga, RegisterSubscriptionSagaState>()
            .EntityFrameworkRepository(r =>
            {
                r.ExistingDbContext<TDbContext>();
                r.UsePostgres();
                
                // CRITICAL: Use pessimistic concurrency for data integrity
                // Prevents duplicate key violations during concurrent saga creation
                r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
                
                // ReadCommitted isolation - balance between performance and consistency
                r.IsolationLevel = IsolationLevel.ReadCommitted;
            });
    }

    /// <summary>
    /// Configure saga endpoints with Entity Framework Outbox and proper fault handling
    /// </summary>
    public static void ConfigureSagaEndpoints(IRabbitMqBusFactoryConfigurator cfg, IBusRegistrationContext context)
    {
        // Configure subscription saga endpoint
        cfg.ReceiveEndpoint("subscription-saga", e =>
        {
            // CRITICAL: Enable Entity Framework Outbox for transactional messaging
            // This ensures saga state changes and published messages are atomic
            // Reference: https://masstransit.io/documentation/configuration/middleware/outbox
            e.UseEntityFrameworkOutbox<Context.SubscriptionDbContext>(context);

            e.ConfigureSaga<RegisterSubscriptionSagaState>(context, s =>
            {
                // Ensure saga correlation works properly with partitioning
                s.Message<SubscriptionRegistrationStarted>(m => 
                    m.UsePartitioner(8, p => p.Message.SubscriptionId));
                s.Message<PaymentSucceeded>(m => 
                    m.UsePartitioner(8, p => p.Message.SubscriptionId));
                s.Message<PaymentFailed>(m => 
                    m.UsePartitioner(8, p => p.Message.SubscriptionId));
            });
            
            // CRITICAL: Configure retry policy
            // Use moderate retry for payment-related operations
            e.UseMessageRetry(r => r.Intervals(100, 500, 1000, 2000, 5000));
            
            // Handle faults gracefully
            e.DiscardSkippedMessages();
            
            // Concurrency settings
            e.ConcurrentMessageLimit = 8; // Allow some parallelism for payment processing
            e.PrefetchCount = 16;
        });
    }
}

