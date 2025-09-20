using AuthService.API.Configurations;
using AuthService.Infrastructure.Extensions;
using ProductAuthMicroservice.Commons.Configurations;
using ProductAuthMicroservice.Commons.DependencyInjection;
using ProductAuthMicroservice.Shared.Contracts.Events;
using ProductAuthMicroservice.Commons.Extensions;
using ProductAuthMicroservice.Commons.Services;

// Create startup logger
var logger = LoggingConfiguration.CreateStartupLogger("AuthService");
DotNetEnv.Env.Load();
try
{
    logger.LogInformation("Starting AuthService API in minimal mode...");
    
    var builder = WebApplication.CreateBuilder(args);

    // --- TẠM THỜI VÔ HIỆU HÓA TẤT CẢ CẤU HÌNH DỊCH VỤ ---
    builder.ConfigureServices();
    builder.Services.AddMicroserviceDistributedAuth(builder.Configuration);
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    var app = builder.Build();

    // --- CHỈ GIỮ LẠI CẤU HÌNH PIPELINE CƠ BẢN NHẤT ---
    // Thêm một endpoint đơn giản để kiểm tra
    app.MapGet("/", () => "Hello from minimal AuthService!");
    
    // Debug Swagger configuration
    logger.LogInformation("About to configure pipeline with Swagger...");
    
    // --- TẠM THỜI VÔ HIỆU HÓA TẤT CẢ CẤU HÌNH PIPELINE TÙY CHỈNH ---
     app.ConfigurePipeline();
     
    logger.LogInformation("Pipeline configured. Checking if Swagger is available...");
    await app.ApplyAuthMigrationsAsync(logger); // Enable auto-migration for production
    await app.SeedAuthDataAsync(logger);
    // app.AddRabbitMQEventBus();
    // var eventBus = app.Services.GetRequiredService<...>();
    // eventBus.Subscribe<...>();
    // app.Services.SubscribeToAuthEvents();
    // eventBus.StartConsuming();

    logger.LogInformation("AuthService API minimal mode configured successfully");
    
    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "AuthService API failed to start");
    throw;
}
finally
{
    logger.LogInformation("AuthService API shutting down");
}
// try
// {
//     logger.LogInformation("Starting AuthService API...");
//
//     var builder = WebApplication.CreateBuilder(args);
//
//     // Configure all services
//     builder.ConfigureServices();
//
//     // Add distributed authentication
//     builder.Services.AddMicroserviceDistributedAuth(builder.Configuration);
//
//     // Add current user service for auth
//     builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
//
//     var app = builder.Build();
//
//     // Configure middleware pipeline
//     app.ConfigurePipeline();
//
//     // Apply database migrations for AuthService (MUST be before seeding)
//     await app.ApplyAuthMigrationsAsync(logger);
//
//     // Seed authentication data (Identity-specific for AuthService)
//     await app.SeedAuthDataAsync(logger);
//
//     // Add RabbitMQ Event Bus and subscribe to events
//     app.AddRabbitMQEventBus();
//
//     // Subscribe to Product Events
//     var eventBus = app.Services.GetRequiredService<ProductAuthMicroservice.Commons.EventBus.IEventBus>();
//     eventBus.Subscribe<ProductCreatedEvent, ProductAuthMicroservice.AuthService.Application.Features.EventHandlers.ProductEventHandlers.ProductCreatedEventHandler>();
//     eventBus.Subscribe<ProductUpdatedEvent, ProductAuthMicroservice.AuthService.Application.Features.EventHandlers.ProductEventHandlers.ProductUpdatedEventHandler>();
//     eventBus.Subscribe<ProductInventoryCreatedEvent, ProductAuthMicroservice.AuthService.Application.Features.EventHandlers.ProductEventHandlers.ProductInventoryCreatedEventHandler>();
//
//     // Subscribe to auth events for distributed authentication
//     app.Services.SubscribeToAuthEvents();
//
//     // Start consuming events
//     eventBus.StartConsuming();
//
//     logger.LogInformation("AuthService API configured successfully");
//
//     app.Run();
// }
// catch (Exception ex)
// {
//     logger.LogCritical(ex, "AuthService API failed to start");
//     throw;
// }
// finally
// {
//     logger.LogInformation("AuthService API shutting down");
// }

