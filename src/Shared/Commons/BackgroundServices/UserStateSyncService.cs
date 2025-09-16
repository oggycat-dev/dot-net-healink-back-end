using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductAuthMicroservice.Commons.Cache;
using ProductAuthMicroservice.Commons.Configs;

namespace ProductAuthMicroservice.Commons.BackgroundServices;

/// <summary>
/// Background service để sync user state và cleanup expired tokens
/// </summary>
public class UserStateSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<UserStateSyncService> _logger;
    private readonly CacheConfig _config;

    public UserStateSyncService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<UserStateSyncService> logger,
        IOptions<CacheConfig> config)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("User State Sync Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessUserStateMaintenanceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in user state sync service");
            }

            // Wait for next cleanup cycle
            await Task.Delay(TimeSpan.FromMinutes(_config.CleanupIntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("User State Sync Service stopped");
    }

    private async Task ProcessUserStateMaintenanceAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var userStateCache = scope.ServiceProvider.GetRequiredService<IUserStateCache>();

        try
        {
            _logger.LogDebug("Starting user state maintenance");

            // 1. Cleanup expired refresh tokens
            await userStateCache.CleanupExpiredTokensAsync();

            // 2. Get active users for monitoring
            var activeUsers = await userStateCache.GetActiveUsersAsync();
            _logger.LogInformation("User state maintenance completed. Active users: {Count}", activeUsers.Count);

            // 3. Log cache statistics
            await LogCacheStatisticsAsync(activeUsers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user state maintenance");
        }
    }

    private async Task LogCacheStatisticsAsync(List<UserStateInfo> activeUsers)
    {
        try
        {
            var statistics = new
            {
                TotalActiveUsers = activeUsers.Count,
                UsersWithValidTokens = activeUsers.Count(u => u.IsRefreshTokenValid),
                UsersWithExpiredTokens = activeUsers.Count(u => !u.IsRefreshTokenValid),
                RoleDistribution = activeUsers
                    .SelectMany(u => u.Roles)
                    .GroupBy(r => r)
                    .ToDictionary(g => g.Key, g => g.Count()),
                StatusDistribution = activeUsers
                    .GroupBy(u => u.Status)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count())
            };

            _logger.LogInformation("Cache Statistics: {@Statistics}", statistics);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error logging cache statistics");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("User State Sync Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}
