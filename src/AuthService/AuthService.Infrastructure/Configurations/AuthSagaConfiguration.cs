using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using AuthService.Infrastructure.Saga;
using SharedLibrary.Contracts.User.Events;
using SharedLibrary.Contracts.User.Saga;

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
                r.IsolationLevel = System.Data.IsolationLevel.ReadCommitted;
            });
    }

    /// <summary>
    /// Configure saga endpoints with proper fault handling
    /// </summary>
    public static void ConfigureSagaEndpoints(IRabbitMqBusFactoryConfigurator cfg, IBusRegistrationContext context)
    {
        // Configure saga endpoint with proper fault handling
        cfg.ReceiveEndpoint("registration-saga", e =>
        {
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
            
            // Single threaded processing to prevent race conditions
            e.ConcurrentMessageLimit = 1;
            
            // Minimal prefetch to reduce duplicate processing  
            e.PrefetchCount = 1;
        });
    }
}
