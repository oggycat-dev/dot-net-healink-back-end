// Create startup logger
using AuthService.API.Configurations;
using AuthService.Infrastructure.Extensions;
using SharedLibrary.Commons.Configurations;
using SharedLibrary.Commons.DependencyInjection;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Commons.Extensions;

var logger = LoggingConfiguration.CreateStartupLogger("AuthService");
DotNetEnv.Env.Load();
try
{
    logger.LogInformation("Starting AuthService API in minimal mode...");
    
    var builder = WebApplication.CreateBuilder(args);

    // --- TẠM THỜI VÔ HIỆU HÓA TẤT CẢ CẤU HÌNH DỊCH VỤ ---
    builder.ConfigureServices();
    builder.Services.AddMicroserviceDistributedAuth(builder.Configuration);
    

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
    
    // Enable RabbitMQ Event Bus for inter-service communication
    logger.LogInformation("Configuring RabbitMQ Event Bus...");
    app.AddRabbitMQEventBus();

    // Subscribe to Product Events from ProductService
    var eventBus = app.Services.GetRequiredService<IEventBus>();
    // eventBus.Subscribe<ProductCreatedEvent, ProductAuthMicroservice.AuthService.Application.Features.EventHandlers.ProductEventHandlers.ProductCreatedEventHandler>();
    // eventBus.Subscribe<ProductUpdatedEvent, ProductAuthMicroservice.AuthService.Application.Features.EventHandlers.ProductEventHandlers.ProductUpdatedEventHandler>();
    // eventBus.Subscribe<ProductInventoryCreatedEvent, ProductAuthMicroservice.AuthService.Application.Features.EventHandlers.ProductEventHandlers.ProductInventoryCreatedEventHandler>();

    // Subscribe to auth events for distributed authentication
    app.Services.SubscribeToAuthEvents();

    // Start consuming events
    eventBus.StartConsuming();
    logger.LogInformation("RabbitMQ Event Bus configured successfully");

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


