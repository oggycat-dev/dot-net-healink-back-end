using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using AuthService.Infrastructure.Saga;
using SharedLibrary.Contracts.User.Events;
using SharedLibrary.Contracts.User.Saga;
using System.Data;

namespace AuthService.Infrastructure.Configurations;

/// <summary>
/// AuthService-specific Saga configuration
/// Configures Registration Saga and its endpoints
/// </summary>
public static class AuthSagaConfiguration
{
    /// <summary>
    /// Configure Registration Saga for AuthService
    /// </summary>
    public static void ConfigureRegistrationSaga<TDbContext>(IRegistrationConfigurator configurator)
        where TDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        // Add Registration Saga with optimized configuration
        configurator.AddSagaStateMachine<RegistrationSaga, RegistrationSagaState>()
            .EntityFrameworkRepository(r =>
            {
                r.ExistingDbContext<TDbContext>();
                r.UsePostgres();
                
                // CRITICAL: Use pessimistic concurrency for data integrity
                // Prevents duplicate key violations during concurrent saga creation
                r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
                
                // ReadCommitted isolation - Serializable was causing deadlocks/retries
                // This allows proper saga creation without blocking on concurrent access
                r.IsolationLevel = IsolationLevel.ReadCommitted;
            });
    }

    /// <summary>
    /// Configure saga endpoints with Entity Framework Outbox and proper fault handling
    /// Single-threaded saga processing to prevent race conditions
    /// </summary>
    public static void ConfigureSagaEndpoints(IRabbitMqBusFactoryConfigurator cfg, IBusRegistrationContext context)
    {
        // Configure saga endpoint with Entity Framework Outbox
        cfg.ReceiveEndpoint("registration-saga", e =>
        {
            // CRITICAL: Enable Entity Framework Outbox for transactional messaging
            // This ensures saga state changes and published messages are atomic
            // Prevents race conditions and duplicate saga instances
            // Reference: https://masstransit.io/documentation/configuration/middleware/outbox
            e.UseEntityFrameworkOutbox<Context.AuthDbContext>(context);

            e.ConfigureSaga<RegistrationSagaState>(context, s =>
            {
                // Ensure saga correlation works properly with partitioning
                s.Message<RegistrationStarted>(m => m.UsePartitioner(8, p => p.Message.CorrelationId));
                s.Message<OtpSent>(m => m.UsePartitioner(8, p => p.Message.CorrelationId));
                s.Message<OtpVerified>(m => m.UsePartitioner(8, p => p.Message.CorrelationId));
                s.Message<AuthUserCreated>(m => m.UsePartitioner(8, p => p.Message.CorrelationId));
                s.Message<UserProfileCreated>(m => m.UsePartitioner(8, p => p.Message.CorrelationId));
            });
            
            // CRITICAL: Disable ALL retry mechanisms for data integrity
            e.UseMessageRetry(r => r.None());
            
            // CRITICAL: Handle faults without retrying
            e.DiscardFaultedMessages();
            e.DiscardSkippedMessages();
            
            // CRITICAL: Single threaded processing to prevent race conditions
            // With Outbox, saga state is locked per correlation ID
            // But we still need single-thread to prevent duplicate RegistrationStarted processing
            e.ConcurrentMessageLimit = 1;
            
            // Minimal prefetch to reduce duplicate processing  
            e.PrefetchCount = 1;
        });
    }
}
