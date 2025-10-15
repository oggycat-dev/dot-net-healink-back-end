using MassTransit;

namespace AuthService.Infrastructure.Configurations;

/// <summary>
/// Configuration for RPC (Request-Response) endpoints in AuthService
/// Handles synchronous-style communication with timeout settings
/// </summary>
public static class AuthRpcConfiguration
{
    /// <summary>
    /// Configure RPC endpoints with timeout for AuthService
    /// </summary>
    public static void ConfigureRpcEndpoints(IRabbitMqBusFactoryConfigurator cfg, IBusRegistrationContext context)
    {
        // ✅ Configure GetUserRoles RPC endpoint with timeout
        cfg.ReceiveEndpoint("get-user-roles-rpc", e =>
        {
            // ⚠️ NO RETRY - RPC should respond immediately or timeout
            // If timeout occurs, caller handles gracefully by returning empty roles
            e.UseMessageRetry(r => r.None());
            
            // Request timeout at endpoint level (10 seconds)
            // If consumer doesn't respond in 10s, caller gets RequestTimeoutException
            // Caller will catch this and return users without roles
            e.UseTimeout(x => x.Timeout = TimeSpan.FromSeconds(10));
            
            // Concurrent processing for better performance
            // Multiple RPC requests can be processed in parallel
            e.ConcurrentMessageLimit = 10;
            e.PrefetchCount = 10;
            
            // Configure the consumer
            e.ConfigureConsumer<Consumers.GetUserRolesConsumer>(context);
        });

        // ✅ Configure UpdateUserInfo RPC endpoint with timeout
        cfg.ReceiveEndpoint("update-user-info-rpc", e =>
        {
            // ⚠️ NO RETRY - If update fails, UserService will rollback
            e.UseMessageRetry(r => r.None());
            
            // Request timeout at endpoint level (10 seconds)
            // If consumer doesn't respond in 10s, UserService will rollback changes
            e.UseTimeout(x => x.Timeout = TimeSpan.FromSeconds(10));
            
            // Sequential processing for data consistency
            // Email/phone updates should not conflict
            e.ConcurrentMessageLimit = 1;
            e.PrefetchCount = 1;
            
            // Configure the consumer
            e.ConfigureConsumer<Consumers.UpdateUserInfoConsumer>(context);
        });

        // ✅ Configure UpdateUserStatus RPC endpoint with timeout
        cfg.ReceiveEndpoint("update-user-status-rpc", e =>
        {
            // ⚠️ NO RETRY - If update fails, UserService will rollback
            e.UseMessageRetry(r => r.None());
            
            // Request timeout at endpoint level (5 seconds)
            // If consumer doesn't respond in 5s, UserService will rollback changes
            e.UseTimeout(x => x.Timeout = TimeSpan.FromSeconds(10));
            
            // Sequential processing for data consistency
            // Status updates should not conflict
            e.ConcurrentMessageLimit = 1;
            e.PrefetchCount = 1;
            
            // Configure the consumer
            e.ConfigureConsumer<Consumers.UpdateUserStatusConsumer>(context);
        });

        // ✅ Configure UpdateUserRoles RPC endpoint with timeout
        cfg.ReceiveEndpoint("update-user-roles-rpc", e =>
        {
            // ⚠️ NO RETRY - If update fails, UserService will rollback
            e.UseMessageRetry(r => r.None());
            
            // Request timeout at endpoint level (5 seconds)
            // If consumer doesn't respond in 5s, UserService will rollback changes
            e.UseTimeout(x => x.Timeout = TimeSpan.FromSeconds(10));
            
            // Sequential processing for data consistency
            // Role updates should not conflict
            e.ConcurrentMessageLimit = 1;
            e.PrefetchCount = 1;
            
            // Configure the consumer
            e.ConfigureConsumer<Consumers.UpdateUserRolesConsumer>(context);
        });

    }
}
