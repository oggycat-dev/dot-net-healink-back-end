namespace ProductAuthMicroservice.Commons.Configs;

public class RedisConfig
{
    public const string SectionName = "Redis";
    
    public string ConnectionString { get; set; } = string.Empty;
    public int Database { get; set; } = 0;
    public int ConnectTimeout { get; set; } = 5000;
    public int SyncTimeout { get; set; } = 5000;
    public bool AbortOnConnectFail { get; set; } = false;
    public int ConnectRetry { get; set; } = 3;
    public string InstanceName { get; set; } = "ProductAuthMicroservice";
    
    // Cache expiration settings
    public int DefaultExpirationMinutes { get; set; } = 60;
    public int UserStateCacheMinutes { get; set; } = 120;
    public int ActiveUsersListHours { get; set; } = 24;
    public int UserStateSlidingMinutes { get; set; } = 30;
}