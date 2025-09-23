# Registration Saga Setup Guide

## Prerequisites

1. .NET 8.0 SDK
2. PostgreSQL Database
3. Redis Server
4. RabbitMQ Message Broker

## Installation Steps

### 1. Install Required NuGet Packages

Add to each service's `.csproj`:

```xml
<!-- AuthService.API -->
<PackageReference Include="MassTransit" Version="8.1.3" />
<PackageReference Include="MassTransit.RabbitMQ" Version="8.1.3" />
<PackageReference Include="MassTransit.EntityFrameworkCore" Version="8.1.3" />

<!-- NotificationService.Infrastructure -->
<PackageReference Include="MassTransit" Version="8.1.3" />
<PackageReference Include="MassTransit.RabbitMQ" Version="8.1.3" />

<!-- UserService.Infrastructure -->
<PackageReference Include="MassTransit" Version="8.1.3" />
<PackageReference Include="MassTransit.RabbitMQ" Version="8.1.3" />
```

### 2. Database Migrations

Add to your DbContext in AuthService:

```csharp
// AuthService/Infrastructure/Context/AuthDbContext.cs
using SharedLibrary.Commons.Configurations;

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Add Saga tables
    modelBuilder.AddSagaTables();
}
```

Run migration:
```bash
dotnet ef migrations add AddRegistrationSaga -p AuthService.Infrastructure -s AuthService.API
dotnet ef database update -p AuthService.Infrastructure -s AuthService.API
```

### 3. Service Configuration

#### AuthService Program.cs

```csharp
using SharedLibrary.Commons.Configurations;
using AuthService.Infrastructure.Context;

var builder = WebApplication.CreateBuilder(args);

// Add MassTransit with Saga
builder.Services.AddMassTransitWithSaga<AuthDbContext>(builder.Configuration);

// Add other services...
builder.Services.AddRedisConfiguration(builder.Configuration);

var app = builder.Build();

// Configure the pipeline...
app.Run();
```

#### NotificationService Program.cs

```csharp
using SharedLibrary.Commons.Configurations;
using NotificationService.Infrastructure.Consumers;

var builder = WebApplication.CreateBuilder(args);

// Add MassTransit with consumers
builder.Services.AddMassTransitWithConsumers(builder.Configuration, x =>
{
    x.AddConsumer<SendOtpNotificationConsumer>();
    x.AddConsumer<SendWelcomeNotificationConsumer>();
});

var app = builder.Build();
app.Run();
```

#### UserService Program.cs

```csharp
using SharedLibrary.Commons.Configurations;
using UserService.Infrastructure.Consumers;

var builder = WebApplication.CreateBuilder(args);

// Add MassTransit with consumers
builder.Services.AddMassTransitWithConsumers(builder.Configuration, x =>
{
    x.AddConsumer<CreateUserConsumer>();
});

var app = builder.Build();
app.Run();
```

### 4. Configuration Files

#### appsettings.json (All Services)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=authservicedb;Username=admin;Password=admin@123"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "admin",
    "Password": "admin@123",
    "VirtualHost": "/",
    "ExchangeName": "healink_exchange",
    "RetryCount": 3,
    "RetryDelaySeconds": 5,
    "UseSsl": false,
    "SslServerName": ""
  },
  "Redis": {
    "ConnectionString": "localhost:6379,password=admin@123",
    "Database": 0,
    "ConnectTimeout": 5000,
    "SyncTimeout": 5000,
    "AbortOnConnectFail": false,
    "ConnectRetry": 3,
    "InstanceName": "HealinkMicroservices",
    "DefaultExpirationMinutes": 60,
    "UserStateCacheMinutes": 120,
    "ActiveUsersListHours": 24,
    "UserStateSlidingMinutes": 30
  },
  "OTPSettings": {
    "Length": 6,
    "ExpirationMinutes": 10,
    "MaxAttempts": 3
  }
}
```

### 5. Docker Compose Configuration

Ensure your `docker-compose.yml` includes:

```yaml
services:
  postgres:
    # existing config...
    
  redis:
    # existing config...
    
  rabbitmq:
    # existing config...
    
  authservice-api:
    environment:
      - Redis__ConnectionString=redis:6379,password=${REDIS_PASSWORD}
      - RabbitMQ__HostName=rabbitmq
    depends_on:
      - postgres
      - redis
      - rabbitmq
      
  notificationservice-api:
    environment:
      - RabbitMQ__HostName=rabbitmq
    depends_on:
      - rabbitmq
      
  userservice-api:
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=userservicedb;Username=${DB_USER};Password=${DB_PASSWORD}
      - RabbitMQ__HostName=rabbitmq
    depends_on:
      - postgres
      - rabbitmq
```

## Testing the Implementation

### 1. Registration Flow Test

```bash
# Start registration
curl -X POST http://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test@123",
    "fullName": "Test User",
    "phoneNumber": "+1234567890",
    "otpSentChannel": "Email"
  }'

# Verify OTP (check email for code)
curl -X POST http://localhost:5001/api/auth/verify-otp \
  -H "Content-Type: application/json" \
  -d '{
    "contact": "test@example.com",
    "otpCode": "123456",
    "channel": "Email"
  }'
```

### 2. Monitor Saga State

Query the saga state table:

```sql
SELECT 
    CorrelationId,
    CurrentState,
    Email,
    CreatedAt,
    OtpSentAt,
    OtpVerifiedAt,
    UserCreatedAt,
    CompletedAt,
    ErrorMessage,
    IsCompleted,
    IsFailed
FROM RegistrationSagaState
ORDER BY CreatedAt DESC;
```

### 3. RabbitMQ Management

Access RabbitMQ Management UI at `http://localhost:15672`
- Check message flow through queues
- Monitor consumer activity
- View exchange bindings

## Troubleshooting

### Common Issues

1. **Saga State Not Persisting**
   - Verify EF Core context includes saga tables
   - Check database migrations
   - Ensure connection string is correct

2. **Messages Not Being Consumed**
   - Verify RabbitMQ connection
   - Check consumer registration
   - Review exchange and queue configuration

3. **OTP Cache Issues**
   - Verify Redis connection
   - Check Redis configuration
   - Monitor Redis logs

4. **Correlation ID Not Found**
   - Ensure correlation ID is stored in OTP cache
   - Verify userData serialization/deserialization
   - Check OTP cache service implementation

### Monitoring and Logging

Add structured logging to track saga progress:

```csharp
// In Saga
_logger.LogInformation("Saga {SagaType} {CorrelationId} transitioned to {State}",
    nameof(RegistrationSaga), context.Saga.CorrelationId, context.Saga.CurrentState);

// In Consumers
_logger.LogInformation("Processing {EventType} for CorrelationId {CorrelationId}",
    nameof(SendOtpNotification), message.CorrelationId);
```

## Performance Considerations

1. **Saga State Cleanup**
   - Configure automatic cleanup of completed sagas
   - Monitor saga state table size
   - Implement archival strategy

2. **Message Retries**
   - Configure appropriate retry policies
   - Implement circuit breaker pattern
   - Monitor failed message queues

3. **Cache Optimization**
   - Set appropriate OTP expiration times
   - Monitor Redis memory usage
   - Implement cache eviction policies