# Service Configuration Summary

## 📋 Tổng quan ServiceConfiguration

Tất cả services đã được refactor để sử dụng centralized configuration pattern:

### **1. AuthService.API/Configurations/ServiceConfiguration.cs**
```csharp
public static class ServiceConfiguration
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Configure microservice with shared services (includes env + logging)
        builder.ConfigureMicroserviceServices("AuthService");
        
        // Add distributed authentication
        builder.Services.AddMicroserviceDistributedAuth(builder.Configuration);
        
        // Add OTP cache service for registration/password reset features
        builder.Services.AddRedisOtpCacheService(builder.Configuration);
        
        // Add MassTransit with Saga for registration workflow
        builder.Services.AddMassTransitWithSaga<AuthService.Infrastructure.Context.AuthDbContext>(
            builder.Configuration, x => {
                x.AddConsumer<AuthService.Infrastructure.Consumers.CreateAuthUserConsumer>();
                x.AddConsumer<AuthService.Infrastructure.Consumers.DeleteAuthUserConsumer>();
            });
        
        // Application & Infrastructure layers
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        
        return builder;
    }
    
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Use shared pipeline configuration
        app.ConfigureSharedPipeline("AuthService");
        return app;
    }
}
```

### **2. Gateway.API/Configuration/ServiceConfiguration.cs**
```csharp
public static class ServiceConfiguration
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Load environment configuration first
        builder.AddEnvironmentConfiguration("Gateway");
        
        // Add logging configuration
        builder.AddLoggingConfiguration("Gateway");
        
        // Core ASP.NET services
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        // Gateway-specific configurations
        builder.Services.AddCorsConfiguration(builder.Configuration);
        builder.Services.AddGatewayDistributedAuth(builder.Configuration);
        
        // Register JWT configuration
        builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection(JwtConfig.SectionName));
        
        // Add current user service
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHttpClient("AuthService", client => {
            client.BaseAddress = new Uri("http://authservice-api");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        
        // Configure Ocelot
        builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
        
        // Configure Authentication for Ocelot
        var jwtConfig = builder.Configuration.GetSection(JwtConfig.SectionName).Get<JwtConfig>();
        if (jwtConfig != null && !string.IsNullOrEmpty(jwtConfig.Key))
        {
            var key = System.Text.Encoding.UTF8.GetBytes(jwtConfig.Key);
            builder.Services.AddAuthentication()
                .AddJwtBearer("Bearer", options => {
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = jwtConfig.ValidateIssuer,
                        ValidateAudience = jwtConfig.ValidateAudience,
                        ValidIssuer = jwtConfig.Issuer,
                        ValidAudience = jwtConfig.Audience,
                        ValidateIssuerSigningKey = jwtConfig.ValidateIssuerSigningKey,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateLifetime = jwtConfig.ValidateLifetime,
                        ClockSkew = TimeSpan.FromMinutes(jwtConfig.ClockSkewMinutes)
                    };
                });
        }
        
        builder.Services.AddOcelot(builder.Configuration);
        builder.Services.AddAuthorization();
        
        return builder;
    }
    
    public static async Task<WebApplication> ConfigurePipelineAsync(this WebApplication app)
    {
        var environment = app.Environment;
        var logger = app.Services.GetRequiredService<ILogger<WebApplication>>();
        
        logger.LogInformation("Configuring Gateway pipeline for Ocelot, Environment: {Environment}", environment.EnvironmentName);
        
        // Enable Swagger for development
        if (environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        
        app.UseHttpsRedirection();
        
        // Use CORS - must be before authentication and Ocelot
        app.UseCorsConfiguration();
        
        // Use correlation ID middleware for distributed tracing - early in pipeline
        app.UseCorrelationId();
        
        // Use distributed auth middleware (Gateway-specific) - before Ocelot
        app.UseMiddleware<Gateway.API.Middlewares.DistributedAuthMiddleware>();
        
        // Use Ocelot - handles its own auth pipeline
        await app.UseOcelot();
        
        return app;
    }
}
```

### **3. UserService.API/Configurations/ServiceConfiguration.cs**
```csharp
public static class ServiceConfiguration
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Configure microservice with shared services (includes env + logging)
        builder.ConfigureMicroserviceServices("UserService");
        
        // Add distributed authentication
        builder.Services.AddMicroserviceDistributedAuth(builder.Configuration);
        
        // Add MassTransit with Consumers
        builder.Services.AddMassTransitWithConsumers(builder.Configuration, x => {
            x.AddConsumer<UserService.Infrastructure.Consumers.CreateUserConsumer>();
            x.AddConsumer<UserService.Infrastructure.Consumers.UpdateUserConsumer>();
            x.AddConsumer<UserService.Infrastructure.Consumers.DeleteUserConsumer>();
        });
        
        // Application & Infrastructure layers
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        
        return builder;
    }
    
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Use shared pipeline configuration
        app.ConfigureSharedPipeline("UserService");
        return app;
    }
}
```

### **4. NotificationService.API/Configurations/ServiceConfiguration.cs**
```csharp
public static class ServiceConfiguration
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Configure microservice with shared services (includes env + logging)
        builder.ConfigureMicroserviceServices("NotificationService");
        
        // Add MassTransit with Consumers
        builder.Services.AddMassTransitWithConsumers(builder.Configuration, x => {
            x.AddConsumer<NotificationService.Infrastructure.Consumers.SendOtpNotificationConsumer>();
            x.AddConsumer<NotificationService.Infrastructure.Consumers.SendWelcomeNotificationConsumer>();
        });
        
        // Application & Infrastructure layers
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        
        return builder;
    }
    
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Use shared pipeline configuration
        app.ConfigureSharedPipeline("NotificationService");
        return app;
    }
    
    public static WebApplication ConfigureEventSubscriptions(this WebApplication app)
    {
        app.AddRabbitMQEventBus();
        var eventBus = app.Services.GetRequiredService<IEventBus>();
        eventBus.Subscribe<ResetPasswordEvent, SendOtpResetPasswordEventHandler>();
        app.Services.SubscribeToAuthEvents();
        return app;
    }
}
```

## 🔧 Shared Configuration Methods

### **ConfigureMicroserviceServices**
```csharp
public static WebApplicationBuilder ConfigureMicroserviceServices(this WebApplicationBuilder builder, string serviceName)
{
    builder.AddEnvironmentConfiguration(serviceName);
    builder.AddLoggingConfiguration(serviceName);
    builder.Services.AddControllers().AddJsonOptions(options => {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSharedServices(builder.Configuration, serviceName);
    return builder;
}
```

### **ConfigureSharedPipeline**
```csharp
public static WebApplication ConfigureSharedPipeline(this WebApplication app, string serviceName)
{
    var environment = app.Environment;
    var logger = app.Services.GetRequiredService<ILogger<WebApplication>>();
    
    logger.LogInformation("Configuring {ServiceName} pipeline, Environment: {Environment}", serviceName, environment.EnvironmentName);
    
    // Enable Swagger for development
    if (environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    
    app.UseHttpsRedirection();
    
    // Enable CORS
    app.UseCorsConfiguration();
    
    // Shared middlewares
    app.UseSharedMiddleware();
    
    app.UseAuthentication();
    app.UseAuthorization();
    
    app.MapControllers();
    
    return app;
}
```

### **UseSharedMiddleware**
```csharp
public static IApplicationBuilder UseSharedMiddleware(this IApplicationBuilder app)
{
    // Use correlation ID middleware for distributed tracing - must be early
    app.UseCorrelationId();
    
    // Use global exception handling
    app.UseGlobalExceptionHandling();
    
    // Use JWT middleware
    app.UseJwtMiddleware();
    
    return app;
}
```

## 📊 Configuration Benefits

### **1. Centralized Configuration**
- **Single source of truth** từ `.env` file
- **Environment-specific** settings
- **Service-specific** configurations
- **Easy maintenance** và updates

### **2. Consistent Pattern**
- **All services** follow same pattern
- **Shared methods** for common configurations
- **Reduced duplication** across services
- **Easier onboarding** for new services

### **3. Logging Integration**
- **Automatic logging** configuration
- **Distributed tracing** with Correlation ID
- **EF Core filtering** to reduce noise
- **File logging** per service

### **4. Service-Specific Features**
- **AuthService**: MassTransit Saga, OTP Cache, Redis
- **UserService**: MassTransit Consumers, User Management
- **NotificationService**: Event Handlers, Email/SMS
- **Gateway**: Ocelot, JWT Auth, CORS, Distributed Auth

## 🚀 Usage Examples

### **Adding New Service**
```csharp
public static class NewServiceConfiguration
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Use shared microservice configuration
        builder.ConfigureMicroserviceServices("NewService");
        
        // Add service-specific configurations
        builder.Services.AddCustomService(builder.Configuration);
        
        // Add Application & Infrastructure
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        
        return builder;
    }
    
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Use shared pipeline
        app.ConfigureSharedPipeline("NewService");
        return app;
    }
}
```

### **Program.cs Usage**
```csharp
var logger = LoggingConfiguration.CreateStartupLogger("ServiceName");
try
{
    logger.LogInformation("Starting ServiceName API...");
    var builder = WebApplication.CreateBuilder(args);
    builder.ConfigureServices(); // Calls service-specific configuration
    var app = builder.Build();
    app.ConfigurePipeline(); // Calls service-specific pipeline
    logger.LogInformation("ServiceName API configured successfully");
    app.Run();
}
catch (Exception ex)
{
    logger.LogError(ex, "ServiceName API failed to start");
    throw;
}
finally
{
    logger.Dispose();
}
```

## 📁 File Structure

```
src/
├── SharedLibrary/
│   └── Commons/
│       ├── DependencyInjection/
│       │   └── SharedServiceExtensions.cs
│       ├── Configurations/
│       │   ├── LoggingConfiguration.cs
│       │   └── EnvironmentConfiguration.cs
│       └── Middlewares/
│           └── CorrelationIdMiddleware.cs
├── Gateway/
│   └── Gateway.API/
│       └── Configuration/
│           └── ServiceConfiguration.cs
├── AuthService/
│   └── AuthService.API/
│       └── Configurations/
│           └── ServiceConfiguration.cs
├── UserService/
│   └── UserService.API/
│       └── Configurations/
│           └── ServiceConfiguration.cs
└── NotificationService/
    └── NotificationService.API/
        └── Configurations/
            └── ServiceConfiguration.cs
```

---

**Tạo bởi:** Healink Development Team  
**Ngày cập nhật:** 2025-01-28  
**Phiên bản:** 1.0.0
