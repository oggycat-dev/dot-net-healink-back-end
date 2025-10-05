# Saga Architecture - Best Practices Guide

## 🎯 Core Principle
**Each microservice should own and manage its own saga state machines and saga tables.**

## ❌ Anti-Pattern (Before Refactor)
```csharp
// ❌ BAD: Saga in SharedLibrary - ALL services get saga tables!
SharedLibrary.Contracts.User.Saga.RegistrationSaga
SharedLibrary.Contracts.User.Saga.RegistrationSagaState

// ❌ Result: Every service DB has unnecessary saga tables
- AuthService DB ✅ (needs it)
- UserService DB ❌ (doesn't need it)
- SubscriptionService DB ❌ (doesn't need it)
- NotificationService DB ❌ (doesn't need it)
```

## ✅ Best Practice (After Refactor)
```csharp
// ✅ GOOD: Saga owned by specific service
AuthService.Infrastructure.Saga.RegistrationSaga
AuthService.Infrastructure.Saga.RegistrationSagaState

// ✅ Result: Only AuthService DB has saga table
- AuthService DB ✅ (owns the saga)
- UserService DB ✅ (no saga table)
- SubscriptionService DB ✅ (no saga table)
- NotificationService DB ✅ (no saga table)
```

## 📐 Architecture Overview

### 1. Saga Ownership Rules
- **Orchestrator Pattern**: Saga lives in the service that orchestrates the workflow
- **Registration Saga**: Owned by `AuthService` (initiates registration)
- **Payment Saga**: Would be owned by `PaymentService` (initiates payment)
- **Subscription Saga**: Would be owned by `SubscriptionService` (initiates subscription)

### 2. Shared Library Contents
**✅ SHOULD be in SharedLibrary:**
- Event contracts (messages)
- Configuration helpers (generic)
- Base classes and interfaces

**❌ SHOULD NOT be in SharedLibrary:**
- Concrete saga implementations
- Saga state classes
- Service-specific business logic

## 🔧 Implementation Guide

### Step 1: Create Saga in Service
```csharp
// src/YourService/YourService.Infrastructure/Saga/YourSagaState.cs
using MassTransit;

namespace YourService.Infrastructure.Saga;

public class YourSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    // ... your saga properties
}
```

```csharp
// src/YourService/YourService.Infrastructure/Saga/YourSaga.cs
using MassTransit;

namespace YourService.Infrastructure.Saga;

public class YourSaga : MassTransitStateMachine<YourSagaState>
{
    public YourSaga()
    {
        InstanceState(x => x.CurrentState);
        // ... configure your saga workflow
    }
}
```

### Step 2: Create Saga Configuration
```csharp
// src/YourService/YourService.Infrastructure/Configurations/YourSagaConfiguration.cs
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;

namespace YourService.Infrastructure.Configurations;

public static class YourSagaConfiguration
{
    public static void ConfigureYourSaga<TDbContext>(IRegistrationConfigurator configurator)
        where TDbContext : DbContext
    {
        configurator.AddSagaStateMachine<YourSaga, YourSagaState>()
            .EntityFrameworkRepository(r =>
            {
                r.ExistingDbContext<TDbContext>();
                r.UsePostgres(); // or r.UseSqlServer(), etc.
                r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
                r.IsolationLevel = System.Data.IsolationLevel.ReadCommitted;
            });
    }
    
    public static void ConfigureSagaEndpoints(
        IRabbitMqBusFactoryConfigurator cfg, 
        IBusRegistrationContext context)
    {
        cfg.ReceiveEndpoint("your-saga-queue", e =>
        {
            e.ConfigureSaga<YourSagaState>(context);
            e.UseMessageRetry(r => r.None()); // Configure as needed
        });
    }
}
```

### Step 3: Create DbContext Extension
```csharp
// src/YourService/YourService.Infrastructure/Extensions/YourSagaDbContextExtensions.cs
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace YourService.Infrastructure.Extensions;

public static class YourSagaDbContextExtensions
{
    public static void AddYourSagaEntities(this ModelBuilder modelBuilder)
    {
        // Add MassTransit infrastructure
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        
        // Configure your saga state
        modelBuilder.Entity<YourSagaState>(entity =>
        {
            entity.HasKey(e => e.CorrelationId);
            entity.Property(e => e.CurrentState).HasMaxLength(64).IsRequired();
            // ... configure your properties and indexes
            entity.ToTable("YourSagaStates");
        });
    }
}
```

### Step 4: Update DbContext
```csharp
// src/YourService/YourService.Infrastructure/Context/YourDbContext.cs
using YourService.Infrastructure.Extensions;

protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);
    
    // Your entity configurations...
    
    // Add saga entities
    builder.AddYourSagaEntities();
}
```

### Step 5: Configure Service Startup
```csharp
// src/YourService/YourService.API/Configurations/ServiceConfiguration.cs
using YourService.Infrastructure.Configurations;
using SharedLibrary.Commons.Configurations;

builder.Services.AddMassTransitWithSaga<YourService.Infrastructure.Context.YourDbContext>(
    builder.Configuration,
    configureSagas: x =>
    {
        // Configure your saga
        YourSagaConfiguration.ConfigureYourSaga<YourService.Infrastructure.Context.YourDbContext>(x);
    },
    configureConsumers: x =>
    {
        // Register your consumers
        x.AddConsumer<YourConsumer>();
    },
    configureEndpoints: (cfg, context) =>
    {
        // Configure saga endpoints
        YourSagaConfiguration.ConfigureSagaEndpoints(cfg, context);
    });
```

## 📊 Example: Registration Saga (Real Implementation)

### Ownership
- **Owner**: `AuthService` (orchestrates user registration)
- **Participants**: `NotificationService`, `UserService`
- **Workflow**: Auth → Notification → Auth → User

### File Structure
```
src/AuthService/
└── AuthService.Infrastructure/
    ├── Saga/
    │   ├── RegistrationSaga.cs          # State machine definition
    │   └── RegistrationSagaState.cs     # State class
    ├── Configurations/
    │   └── AuthSagaConfiguration.cs     # Saga setup
    ├── Extensions/
    │   └── AuthSagaDbContextExtensions.cs # DbContext config
    └── Context/
        └── AuthDbContext.cs             # Uses AddAuthSagaEntities()
```

### Event Flow
```
1. AuthService publishes RegistrationStarted → Saga created
2. Saga publishes SendOtpNotification → NotificationService
3. NotificationService publishes OtpSent → Saga transitions
4. User verifies OTP → AuthService publishes OtpVerified
5. Saga publishes CreateAuthUser → AuthService
6. AuthService publishes AuthUserCreated → Saga
7. Saga publishes CreateUserProfile → UserService
8. UserService publishes UserProfileCreated → Saga completes
```

## 🚀 Services Without Sagas

If your service only consumes events (no saga orchestration):

```csharp
// Use AddMassTransitWithConsumers instead
builder.Services.AddMassTransitWithConsumers(
    builder.Configuration,
    configureConsumers: x =>
    {
        x.AddConsumer<YourEventConsumer>();
    });
```

**Examples:**
- `NotificationService`: Only sends emails/SMS (no saga)
- `UserService`: Only creates profiles (no saga)
- `SubscriptionService`: Currently only consumers (unless adding subscription saga)

## 🔍 When to Create a New Saga

Create a saga when you need to:
1. **Coordinate multiple services** in a workflow
2. **Handle distributed transactions** with compensating actions
3. **Track long-running processes** (minutes/hours)
4. **Ensure eventual consistency** across services

**Examples:**
- ✅ User Registration (Auth → Notification → User)
- ✅ Order Processing (Order → Payment → Inventory → Shipping)
- ✅ Subscription Renewal (Subscription → Payment → Notification)
- ❌ Simple event notification (just use consumer)
- ❌ Single service operation (use local transaction)

## 🛠️ Migration Guide

### For Existing Projects:
1. Identify services with saga tables they don't own
2. Move saga to correct owning service
3. Create new migration in owning service
4. Drop saga tables from other services
5. Update references to use new saga location

### Creating Migration:
```powershell
# In the service that owns the saga
cd src/YourService/YourService.Infrastructure
dotnet ef migrations add AddYourSaga --context YourDbContext --output-dir Migrations
dotnet ef database update --context YourDbContext
```

## 📚 Related Files
- `SharedLibrary.Commons.Configurations.MassTransitSagaConfiguration` - Generic config
- `SAGA_REFACTOR_SUMMARY.md` - Refactor details
- Event contracts in `SharedLibrary.Contracts.*` - Shared between services

## ✅ Checklist for New Saga

- [ ] Saga state class created in service's Infrastructure/Saga
- [ ] Saga state machine created in service's Infrastructure/Saga  
- [ ] Configuration helper created in Infrastructure/Configurations
- [ ] DbContext extension created in Infrastructure/Extensions
- [ ] DbContext updated to use saga entities
- [ ] Service startup configured with AddMassTransitWithSaga
- [ ] Migration created and applied
- [ ] Events defined in SharedLibrary.Contracts (if new)
- [ ] Consumers registered in participating services
- [ ] Tests created for saga workflow
- [ ] Documentation updated
