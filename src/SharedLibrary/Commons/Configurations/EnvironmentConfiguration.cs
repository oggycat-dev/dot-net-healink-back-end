using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace SharedLibrary.Commons.Configurations;

/// <summary>
/// Centralized environment configuration for all microservices
/// Loads environment variables from .env files and system environment
/// </summary>
public static class EnvironmentConfiguration
{
    /// <summary>
    /// Configure environment variables for microservices
    /// </summary>
    /// <param name="builder">WebApplicationBuilder</param>
    /// <param name="serviceName">Name of the microservice (e.g., "AuthService", "UserService")</param>
    /// <returns>WebApplicationBuilder with environment configuration</returns>
    public static WebApplicationBuilder AddEnvironmentConfiguration(this WebApplicationBuilder builder, string serviceName)
    {
        var environment = builder.Environment.EnvironmentName;
        
        // Load appropriate .env file based on environment
        LoadEnvironmentFiles(environment);
        
        // Set service identification
        builder.Configuration["Service:Name"] = serviceName;
        builder.Configuration["Service:Environment"] = environment;
        
        // Configure shared settings
        ConfigureSharedSettings(builder.Configuration);
        
        // Configure service-specific settings
        ConfigureServiceSpecificSettings(builder.Configuration, serviceName);
        
        return builder;
    }
    
    /// <summary>
    /// Load environment files based on environment
    /// </summary>
    private static void LoadEnvironmentFiles(string environment)
    {
        try
        {
            // Try multiple locations for .env files (like Booklify pattern)
            var envFiles = environment.Equals("Production", StringComparison.OrdinalIgnoreCase) 
                ? new[] { ".env.production" }
                : environment.Equals("Development", StringComparison.OrdinalIgnoreCase)
                    ? new[] { ".env.development", ".env" }
                    : new[] { ".env" };

            var envLocations = new[]
            {
                ".",                              // Current directory
                "..",                            // Parent directory
                "../..",                         // 2 levels up 
                "../../..",                      // 3 levels up (project root from src/service/api)
                "../../../.."                    // 4 levels up
            };

            bool envLoaded = false;
            foreach (var envFile in envFiles)
            {
                foreach (var location in envLocations)
                {
                    var envPath = Path.Combine(location, envFile);
                    if (File.Exists(envPath))
                    {
                        DotNetEnv.Env.Load(envPath);
                        envLoaded = true;
                        break;
                    }
                }
                if (envLoaded) break;
            }
        }
        catch (Exception ex)
        {
            // Log warning but don't fail startup
            Console.WriteLine($"Warning: Could not load .env file: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Configure shared settings across all microservices
    /// </summary>
    private static void ConfigureSharedSettings(IConfiguration configuration)
    {
        // === DATABASE SETTINGS ===
        configuration["ConnectionConfig:DefaultConnection"] = 
            Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? 
            Environment.GetEnvironmentVariable("ConnectionConfig__DefaultConnection");
        configuration["ConnectionConfig:RetryOnFailure"] = 
            Environment.GetEnvironmentVariable("DB_RETRY_ON_FAILURE") ?? "true";
        configuration["ConnectionConfig:MaxRetryCount"] = 
            Environment.GetEnvironmentVariable("DB_MAX_RETRY_COUNT") ?? "3";
        configuration["ConnectionConfig:MaxRetryDelay"] = 
            Environment.GetEnvironmentVariable("DB_MAX_RETRY_DELAY") ?? "30";

        // === JWT SETTINGS ===
        configuration["JwtConfig:Key"] = 
            Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? 
            Environment.GetEnvironmentVariable("JwtConfig__Key");
        configuration["JwtConfig:Issuer"] = 
            Environment.GetEnvironmentVariable("JWT_ISSUER") ?? 
            Environment.GetEnvironmentVariable("JwtConfig__Issuer");
        configuration["JwtConfig:Audience"] = 
            Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? 
            Environment.GetEnvironmentVariable("JwtConfig__Audience");
        configuration["JwtConfig:ExpiresInMinutes"] = 
            Environment.GetEnvironmentVariable("JWT_EXPIRES_IN_MINUTES") ?? "60";
        configuration["JwtConfig:RefreshTokenExpiresInDays"] = 
            Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRES_IN_DAYS") ?? "7";
        configuration["JwtConfig:ValidateIssuer"] = 
            Environment.GetEnvironmentVariable("JWT_VALIDATE_ISSUER") ?? "true";
        configuration["JwtConfig:ValidateAudience"] = 
            Environment.GetEnvironmentVariable("JWT_VALIDATE_AUDIENCE") ?? "true";
        configuration["JwtConfig:ValidateLifetime"] = 
            Environment.GetEnvironmentVariable("JWT_VALIDATE_LIFETIME") ?? "true";
        configuration["JwtConfig:ValidateIssuerSigningKey"] = 
            Environment.GetEnvironmentVariable("JWT_VALIDATE_ISSUER_SIGNING_KEY") ?? "true";
        configuration["JwtConfig:ClockSkewMinutes"] = 
            Environment.GetEnvironmentVariable("JWT_CLOCK_SKEW_MINUTES") ?? "5";

        // === RABBITMQ SETTINGS ===
        configuration["RabbitMQ:HostName"] = 
            Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? 
            Environment.GetEnvironmentVariable("RabbitMQ__HostName") ?? "rabbitmq";
        configuration["RabbitMQ:Port"] = 
            Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672";
        configuration["RabbitMQ:UserName"] = 
            Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? 
            Environment.GetEnvironmentVariable("RabbitMQ__UserName") ?? "admin";
        configuration["RabbitMQ:Password"] = 
            Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "admin@123";
        configuration["RabbitMQ:VirtualHost"] = 
            Environment.GetEnvironmentVariable("RABBITMQ_VHOST") ?? "/";
        configuration["RabbitMQ:ExchangeName"] = 
            Environment.GetEnvironmentVariable("RABBITMQ_EXCHANGE") ?? 
            Environment.GetEnvironmentVariable("RabbitMQ__ExchangeName") ?? "healink_exchange";
        configuration["RabbitMQ:Durable"] = 
            Environment.GetEnvironmentVariable("RABBITMQ_DURABLE") ?? "true";
        configuration["RabbitMQ:AutoDelete"] = 
            Environment.GetEnvironmentVariable("RABBITMQ_AUTO_DELETE") ?? "false";
        configuration["RabbitMQ:RetryCount"] = 
            Environment.GetEnvironmentVariable("RABBITMQ_RETRY_COUNT") ?? "3";
        configuration["RabbitMQ:RetryDelaySeconds"] = 
            Environment.GetEnvironmentVariable("RABBITMQ_RETRY_DELAY_SECONDS") ?? "5";

        // === REDIS SETTINGS ===
        configuration["ConnectionStrings:Redis"] = 
            Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? 
            Environment.GetEnvironmentVariable("Redis__ConnectionString");
        configuration["Redis:ConnectionString"] = 
            Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? 
            Environment.GetEnvironmentVariable("Redis__ConnectionString");
        configuration["Redis:Database"] = 
            Environment.GetEnvironmentVariable("REDIS_DATABASE") ?? "0";
        configuration["Redis:ConnectTimeout"] = 
            Environment.GetEnvironmentVariable("REDIS_CONNECT_TIMEOUT") ?? "5000";
        configuration["Redis:SyncTimeout"] = 
            Environment.GetEnvironmentVariable("REDIS_SYNC_TIMEOUT") ?? "5000";
        configuration["Redis:AbortOnConnectFail"] = 
            Environment.GetEnvironmentVariable("REDIS_ABORT_ON_CONNECT_FAIL") ?? "false";
        configuration["Redis:ConnectRetry"] = 
            Environment.GetEnvironmentVariable("REDIS_CONNECT_RETRY") ?? "3";
        configuration["Redis:InstanceName"] = 
            Environment.GetEnvironmentVariable("REDIS_INSTANCE_NAME") ?? "HealinkMicroservices";
        configuration["Redis:Enabled"] = 
            Environment.GetEnvironmentVariable("REDIS_ENABLED") ?? "true";
        configuration["Redis:DefaultExpirationMinutes"] = 
            Environment.GetEnvironmentVariable("REDIS_DEFAULT_EXPIRATION_MINUTES") ?? "60";
        configuration["Redis:UserStateCacheMinutes"] = 
            Environment.GetEnvironmentVariable("REDIS_USER_STATE_CACHE_MINUTES") ?? "120";
        configuration["Redis:ActiveUsersListHours"] = 
            Environment.GetEnvironmentVariable("REDIS_ACTIVE_USERS_LIST_HOURS") ?? "24";
        configuration["Redis:UserStateSlidingMinutes"] = 
            Environment.GetEnvironmentVariable("REDIS_USER_STATE_SLIDING_MINUTES") ?? "30";

        // === CORS SETTINGS ===
        configuration["CorsConfig:PolicyName"] = 
            Environment.GetEnvironmentVariable("CORS_POLICY_NAME") ?? "DefaultCors";
        configuration["CorsConfig:AllowAnyOrigin"] = 
            Environment.GetEnvironmentVariable("CORS_ALLOW_ANY_ORIGIN") ?? "false";
        configuration["CorsConfig:AllowAnyMethod"] = 
            Environment.GetEnvironmentVariable("CORS_ALLOW_ANY_METHOD") ?? "true";
        configuration["CorsConfig:AllowAnyHeader"] = 
            Environment.GetEnvironmentVariable("CORS_ALLOW_ANY_HEADER") ?? "true";
        configuration["CorsConfig:AllowCredentials"] = 
            Environment.GetEnvironmentVariable("CORS_ALLOW_CREDENTIALS") ?? "true";
        
        // Load CORS allowed origins - support both comma-separated string and array format
        var corsOriginsString = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS") ?? 
                               "http://localhost:3000,http://localhost:3001,http://localhost:5173,http://localhost:4200";
        
        // Convert comma-separated string to array format for configuration
        var corsOriginsArray = corsOriginsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                .Select(o => o.Trim())
                                                .ToArray();
        
        // Set both formats for compatibility
        configuration["CorsConfig:AllowedOrigins"] = corsOriginsString;
        for (int i = 0; i < corsOriginsArray.Length; i++)
        {
            configuration[$"CorsConfig:AllowedOrigins:{i}"] = corsOriginsArray[i];
        }
        
        // Additional CORS settings
        configuration["CorsConfig:AllowedMethods"] = 
            Environment.GetEnvironmentVariable("CORS_ALLOWED_METHODS") ?? "GET,POST,PUT,DELETE,OPTIONS";
        configuration["CorsConfig:AllowedHeaders"] = 
            Environment.GetEnvironmentVariable("CORS_ALLOWED_HEADERS") ?? "*";
        configuration["CorsConfig:ExposedHeaders"] = 
            Environment.GetEnvironmentVariable("CORS_EXPOSED_HEADERS") ?? "Content-Disposition,Token-Expired";
        configuration["CorsConfig:PreflightMaxAgeSeconds"] = 
            Environment.GetEnvironmentVariable("CORS_PREFLIGHT_MAX_AGE_SECONDS") ?? "600";

        // === OUTBOX PROCESSOR SETTINGS ===
        configuration["OutboxProcessor:ProcessingIntervalSeconds"] = 
            Environment.GetEnvironmentVariable("OUTBOX_PROCESSING_INTERVAL_SECONDS") ?? "30";
        configuration["OutboxProcessor:BatchSize"] = 
            Environment.GetEnvironmentVariable("OUTBOX_BATCH_SIZE") ?? "50";
        configuration["OutboxProcessor:MaxRetryAttempts"] = 
            Environment.GetEnvironmentVariable("OUTBOX_MAX_RETRY_ATTEMPTS") ?? "5";
        configuration["OutboxProcessor:Enabled"] = 
            Environment.GetEnvironmentVariable("OUTBOX_ENABLED") ?? "true";
        configuration["OutboxProcessor:ProcessingTimeoutSeconds"] = 
            Environment.GetEnvironmentVariable("OUTBOX_PROCESSING_TIMEOUT_SECONDS") ?? "300";
        configuration["OutboxProcessor:EnableRetryWithBackoff"] = 
            Environment.GetEnvironmentVariable("OUTBOX_ENABLE_RETRY_WITH_BACKOFF") ?? "true";

        // === CACHE SETTINGS ===
        configuration["Cache:UserStateCacheMinutes"] = 
            Environment.GetEnvironmentVariable("CACHE_USER_STATE_CACHE_MINUTES") ?? "30";
        configuration["Cache:UserStateSlidingMinutes"] = 
            Environment.GetEnvironmentVariable("CACHE_USER_STATE_SLIDING_MINUTES") ?? "15";
        configuration["Cache:ActiveUsersListHours"] = 
            Environment.GetEnvironmentVariable("CACHE_ACTIVE_USERS_LIST_HOURS") ?? "24";
        configuration["Cache:CleanupIntervalMinutes"] = 
            Environment.GetEnvironmentVariable("CACHE_CLEANUP_INTERVAL_MINUTES") ?? "60";
        configuration["Cache:MaxCacheSize"] = 
            Environment.GetEnvironmentVariable("CACHE_MAX_CACHE_SIZE") ?? "10000";

        // === DATA CONFIG ===
        configuration["DataConfig:EnableSeeding"] = 
            Environment.GetEnvironmentVariable("DATA_ENABLE_SEEDING") ?? "true";
        configuration["DataConfig:EnableAutoApplyMigrations"] = 
            Environment.GetEnvironmentVariable("DATA_ENABLE_AUTO_APPLY_MIGRATIONS") ?? "true";

        // === OTP SETTINGS ===
        configuration["OtpSettings:Length"] = 
            Environment.GetEnvironmentVariable("OTP_LENGTH") ?? "6";
        configuration["OtpSettings:ExpirationMinutes"] = 
            Environment.GetEnvironmentVariable("OTP_EXPIRATION_MINUTES") ?? "5";
        configuration["OtpSettings:MaxAttempts"] = 
            Environment.GetEnvironmentVariable("OTP_MAX_ATTEMPTS") ?? "3";
        configuration["OtpSettings:RetryLimitPerHour"] = 
            Environment.GetEnvironmentVariable("OTP_RETRY_LIMIT_PER_HOUR") ?? "5";
        configuration["OtpSettings:CooldownMinutes"] = 
            Environment.GetEnvironmentVariable("OTP_COOLDOWN_MINUTES") ?? "1";
    }
    
    /// <summary>
    /// Configure service-specific settings
    /// </summary>
    private static void ConfigureServiceSpecificSettings(IConfiguration configuration, string serviceName)
    {
        switch (serviceName.ToLowerInvariant())
        {
            case "authservice":
                ConfigureAuthServiceSettings(configuration);
                break;
            case "userservice":
                ConfigureUserServiceSettings(configuration);
                break;
            case "notificationservice":
                ConfigureNotificationServiceSettings(configuration);
                break;
            case "gateway":
                ConfigureGatewaySettings(configuration);
                break;
        }
    }
    
    private static void ConfigureAuthServiceSettings(IConfiguration configuration)
    {
        // Auth-specific database
        configuration["ConnectionConfig:DefaultConnection"] = 
            Environment.GetEnvironmentVariable("AUTH_DB_CONNECTION_STRING") ?? 
            Environment.GetEnvironmentVariable("ConnectionConfig__DefaultConnection");
        
        // Queue name
        configuration["RabbitMQ:QueueName"] = 
            Environment.GetEnvironmentVariable("AUTH_QUEUE_NAME") ?? "authservice_queue";
        
        // Admin account
        configuration["DefaultAdminAccount:Email"] = 
            Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@healink.com";
        configuration["DefaultAdminAccount:Password"] = 
            Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "admin@123";
        configuration["DefaultAdminAccount:UserId"] = 
            Environment.GetEnvironmentVariable("ADMIN_USER_ID") ?? "00000000-0000-0000-0000-000000000001";
        
        // Password encryption
        configuration["PasswordEncryptionKey"] = 
            Environment.GetEnvironmentVariable("PASSWORD_ENCRYPTION_KEY") ?? "K9ltF1d2jlYvLsaN6AdmiaPHY8qwqUIW";
    }
    
    private static void ConfigureUserServiceSettings(IConfiguration configuration)
    {
        // User-specific database
        configuration["ConnectionConfig:DefaultConnection"] = 
            Environment.GetEnvironmentVariable("USER_DB_CONNECTION_STRING") ?? 
            Environment.GetEnvironmentVariable("ConnectionConfig__DefaultConnection");
        
        // Queue name
        configuration["RabbitMQ:QueueName"] = 
            Environment.GetEnvironmentVariable("USER_QUEUE_NAME") ?? "userservice_queue";
        
        // Default admin account for UserService
        configuration["DefaultAdminAccount:Email"] = 
            Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@healink.com";
        configuration["DefaultAdminAccount:UserId"] = 
            Environment.GetEnvironmentVariable("ADMIN_USER_ID") ?? "00000000-0000-0000-0000-000000000001";
    }
    
    private static void ConfigureNotificationServiceSettings(IConfiguration configuration)
    {
        // Notification service doesn't need database, so no queue name needed
        configuration["RabbitMQ:QueueName"] = 
            Environment.GetEnvironmentVariable("NOTIFICATION_QUEUE_NAME") ?? "";
        
        // Email settings
        configuration["EmailSettings:SmtpServer"] = 
            Environment.GetEnvironmentVariable("EMAIL_SMTP_SERVER") ?? "smtp.gmail.com";
        configuration["EmailSettings:SmtpPort"] = 
            Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT") ?? "587";
        configuration["EmailSettings:SenderEmail"] = 
            Environment.GetEnvironmentVariable("EMAIL_SENDER_EMAIL") ?? "nguyenhoainamvt99@gmail.com";
        configuration["EmailSettings:SenderName"] = 
            Environment.GetEnvironmentVariable("EMAIL_SENDER_NAME") ?? "No Reply - Healink";
        configuration["EmailSettings:SenderPassword"] = 
            Environment.GetEnvironmentVariable("EMAIL_SENDER_PASSWORD") ?? "";
        configuration["EmailSettings:EnableSsl"] = 
            Environment.GetEnvironmentVariable("EMAIL_ENABLE_SSL") ?? "true";
        
        // App settings
        configuration["AppSettings:AppName"] = 
            Environment.GetEnvironmentVariable("APP_NAME") ?? "Healink Notification Service";
        configuration["AppSettings:SupportEmail"] = 
            Environment.GetEnvironmentVariable("SUPPORT_EMAIL") ?? "healinksupport@gmail.com";
        
        // Firebase settings
        configuration["FirebaseSettings:ServerKey"] = 
            Environment.GetEnvironmentVariable("FIREBASE_SERVER_KEY") ?? "";
        configuration["FirebaseSettings:SenderId"] = 
            Environment.GetEnvironmentVariable("FIREBASE_SENDER_ID") ?? "";
    }
    
    private static void ConfigureGatewaySettings(IConfiguration configuration)
    {
        // Gateway doesn't need database connection
        // Queue name
        configuration["RabbitMQ:QueueName"] = 
            Environment.GetEnvironmentVariable("GATEWAY_QUEUE_NAME") ?? "";
        
        // Service URLs for Gateway
        configuration["AuthServiceUrl"] = 
            Environment.GetEnvironmentVariable("AUTH_SERVICE_URL") ?? "http://authservice-api";
        configuration["UserServiceUrl"] = 
            Environment.GetEnvironmentVariable("USER_SERVICE_URL") ?? "http://userservice-api";
            
        // === LOGGING CONFIGURATION ===
        configuration["LoggingConfig:EnableFileLogging"] = 
            Environment.GetEnvironmentVariable("LOG_ENABLE_FILE_LOGGING") ?? "true";
        configuration["LoggingConfig:EnableConsoleLogging"] = 
            Environment.GetEnvironmentVariable("LOG_ENABLE_CONSOLE_LOGGING") ?? "true";
        configuration["LoggingConfig:EnableDistributedTracing"] = 
            Environment.GetEnvironmentVariable("LOG_ENABLE_DISTRIBUTED_TRACING") ?? "true";
        configuration["LoggingConfig:MinimumLevel"] = 
            Environment.GetEnvironmentVariable("LOG_MINIMUM_LEVEL") ?? "Information";
        configuration["LoggingConfig:FileSizeLimitMB"] = 
            Environment.GetEnvironmentVariable("LOG_FILE_SIZE_LIMIT_MB") ?? "10";
        configuration["LoggingConfig:RetainedFileCount"] = 
            Environment.GetEnvironmentVariable("LOG_RETAINED_FILE_COUNT") ?? "30";
            
        // Docker-specific logging controls
        configuration["LoggingConfig:DockerReduceEfCoreNoise"] = 
            Environment.GetEnvironmentVariable("LOG_DOCKER_REDUCE_EF_CORE_NOISE") ?? "true";
            
        // EF Core log level controls
        configuration["LoggingConfig:EfCoreLevel"] = 
            Environment.GetEnvironmentVariable("LOG_EF_CORE_LEVEL") ?? "None";
        configuration["LoggingConfig:EfDatabaseCommandLevel"] = 
            Environment.GetEnvironmentVariable("LOG_EF_DATABASE_COMMAND_LEVEL") ?? "None";
        configuration["LoggingConfig:NpgsqlLevel"] = 
            Environment.GetEnvironmentVariable("LOG_NPGSQL_LEVEL") ?? "None";
        configuration["LoggingConfig:SqlClientLevel"] = 
            Environment.GetEnvironmentVariable("LOG_SQL_CLIENT_LEVEL") ?? "None";
            
        // Specific category controls
        configuration["LoggingConfig:EnableEfCommands"] = 
            Environment.GetEnvironmentVariable("LOG_ENABLE_EF_COMMANDS") ?? "false";
        configuration["LoggingConfig:EnableEfCore"] = 
            Environment.GetEnvironmentVariable("LOG_ENABLE_EF_CORE") ?? "false";
        configuration["LoggingConfig:EnableNpgsql"] = 
            Environment.GetEnvironmentVariable("LOG_ENABLE_NPGSQL") ?? "false";
        configuration["LoggingConfig:EnableSqlClient"] = 
            Environment.GetEnvironmentVariable("LOG_ENABLE_SQL_CLIENT") ?? "false";
        configuration["LoggingConfig:EnableStaticFiles"] = 
            Environment.GetEnvironmentVariable("LOG_ENABLE_STATIC_FILES") ?? "false";
        configuration["LoggingConfig:EnableHostingDiagnostics"] = 
            Environment.GetEnvironmentVariable("LOG_ENABLE_HOSTING_DIAGNOSTICS") ?? "false";
        configuration["LoggingConfig:EnableMvcInfrastructure"] = 
            Environment.GetEnvironmentVariable("LOG_ENABLE_MVC_INFRASTRUCTURE") ?? "false";
        configuration["LoggingConfig:EnableRouting"] = 
            Environment.GetEnvironmentVariable("LOG_ENABLE_ROUTING") ?? "false";
        configuration["LoggingConfig:EnableAuthentication"] = 
            Environment.GetEnvironmentVariable("LOG_ENABLE_AUTHENTICATION") ?? "false";
        configuration["LoggingConfig:EnableAuthorization"] = 
            Environment.GetEnvironmentVariable("LOG_ENABLE_AUTHORIZATION") ?? "false";
        configuration["LoggingConfig:EnableOcelot"] = 
            Environment.GetEnvironmentVariable("LOG_ENABLE_OCELOT") ?? "false";
        configuration["LoggingConfig:EnableMassTransit"] = 
            Environment.GetEnvironmentVariable("LOG_ENABLE_MASS_TRANSIT") ?? "false";
    }
}
