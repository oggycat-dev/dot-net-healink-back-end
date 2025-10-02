# 🎯 Generic Saga Configuration Guide

## 📋 Table of Contents
- [Overview](#overview)
- [Current Problem](#current-problem)
- [Proposed Solution](#proposed-solution)
- [Implementation Plan](#implementation-plan)
- [Example Usage](#example-usage)

---

## 🎯 Overview

Mỗi service có thể có nhiều Saga khác nhau để orchestrate các business workflows phức tạp. Hiện tại, MassTransit Saga configuration đang hard-coded `RegistrationSaga` vào tất cả services sử dụng `AddMassTransitWithSaga`, gây ra issues:

❌ **Current Issues:**
1. SubscriptionService không cần `RegistrationSaga` nhưng vẫn bị register  
2. Không thể add custom Saga cho từng service (e.g., SubscriptionPaymentSaga)
3. Database migrations tự động tạo RegistrationSagaState table ở tất cả services
4. Saga state conflicts và unnecessary coupling

---

## 🚨 Current Problem

### Problem 1: SubscriptionService có RegistrationSaga logs
```log
[subscriptionservice-api] NEW RegistrationSaga instance created - Email: test@gmail.com
```

**Root Cause:**
```csharp
// MassTransitSagaConfiguration.cs line 44-62
x.AddSagaStateMachine<RegistrationSaga, RegistrationSagaState>() // ❌ HARD-CODED!
    .EntityFrameworkRepository(r =>
    {
        r.ExistingDbContext<TDbContext>();
        r.UsePostgres();
    });
```

### Problem 2: Cannot add custom Sagas per service
```csharp
// SubscriptionService needs its own Saga
// But AddMassTransitWithSaga<SubscriptionDbContext>() forces RegistrationSaga!
services.AddMassTransitWithSaga<SubscriptionDbContext>(...); // ❌ Wrong Saga type!
```

---

## ✅ Proposed Solution

### Design: Generic Saga Registration Pattern

Similar to `BaseDbContextFactory<TDbContext>`, create a **fluent API** for Saga registration:

```csharp
// 🎯 GOAL: Each service explicitly declares which Sagas it hosts

// AuthService - hosts RegistrationSaga
builder.Services.AddMassTransitWithSagas<AuthDbContext>(
    configuration,
    sagas => sagas.AddRegistrationSaga(), // ✅ Explicit!
    consumers => consumers.AddConsumer<CreateAuthUserConsumer>());

// SubscriptionService - hosts SubscriptionPaymentSaga (future)
builder.Services.AddMassTransitWithSagas<SubscriptionDbContext>(
    configuration,
    sagas => sagas.AddSubscriptionPaymentSaga(), // ✅ Different Saga!
    consumers => { /* no consumers yet */ });

// UserService - NO Saga, only consumers
builder.Services.AddMassTransitWithConsumers(
    configuration,
    consumers => consumers.AddConsumer<CreateUserProfileConsumer>()); // ✅ No Saga!
```

---

## 🛠️ Implementation Plan

### Step 1: Create Generic Saga Configurator Interface

**File:** `SharedLibrary/Commons/Configurations/MassTransitSagaConfiguration.cs`

```csharp
/// <summary>
/// Generic Saga Registration Configurator
/// Allows each service to register multiple Sagas with their own configuration
/// </summary>
public interface ISagaRegistrationConfigurator<TDbContext> where TDbContext : DbContext
{
    /// <summary>
    /// Add a Saga to this service with custom endpoint configuration
    /// </summary>
    ISagaRegistrationConfigurator<TDbContext> AddSaga<TSaga, TSagaState>(
        string endpointName,
        Action<IEntityFrameworkSagaRepositoryConfigurator<TSagaState>>? configureRepository = null,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
        where TSaga : class, SagaStateMachine<TSagaState>
        where TSagaState : class, SagaStateMachineInstance;
}
```

### Step 2: Implement Saga Configurator

```csharp
internal class SagaRegistrationConfigurator<TDbContext> : ISagaRegistrationConfigurator<TDbContext> 
    where TDbContext : DbContext
{
    private readonly IRegistrationConfigurator _busConfigurator;
    private readonly List<(string, Type, Action<IReceiveEndpointConfigurator>?)> _sagaEndpoints = new();

    public ISagaRegistrationConfigurator<TDbContext> AddSaga<TSaga, TSagaState>(
        string endpointName,
        Action<IEntityFrameworkSagaRepositoryConfigurator<TSagaState>>? configureRepository = null,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
        where TSaga : class, SagaStateMachine<TSagaState>
        where TSagaState : class, SagaStateMachineInstance
    {
        // Register Saga with EF Core repository
        _busConfigurator.AddSagaStateMachine<TSaga, TSagaState>()
            .EntityFrameworkRepository(r =>
            {
                r.ExistingDbContext<TDbContext>();
                r.UsePostgres();
                
                // Default configuration
                r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
                r.IsolationLevel = System.Data.IsolationLevel.ReadCommitted;
                
                // Allow custom configuration
                configureRepository?.Invoke(r);
            });

        // Store endpoint configuration
        _sagaEndpoints.Add((endpointName, typeof(TSagaState), configureEndpoint));
        
        return this;
    }
    
    public IEnumerable<(string, Type, Action<IReceiveEndpointConfigurator>?)> GetSagaEndpoints() 
        => _sagaEndpoints;
}
```

### Step 3: Create Extension Methods for Common Sagas

```csharp
/// <summary>
/// Extension methods for common Saga configurations
/// Each Saga type has its own extension method
/// </summary>
public static class SagaRegistrationConfiguratorExtensions
{
    /// <summary>
    /// Add RegistrationSaga (for AuthService)
    /// </summary>
    public static ISagaRegistrationConfigurator<TDbContext> AddRegistrationSaga<TDbContext>(
        this ISagaRegistrationConfigurator<TDbContext> configurator)
        where TDbContext : DbContext
    {
        return configurator.AddSaga<RegistrationSaga, RegistrationSagaState>(
            endpointName: "registration-saga",
            configureRepository: null, // Use defaults
            configureEndpoint: null);
    }

    /// <summary>
    /// Add SubscriptionPaymentSaga (for SubscriptionService - future)
    /// </summary>
    public static ISagaRegistrationConfigurator<TDbContext> AddSubscriptionPaymentSaga<TDbContext>(
        this ISagaRegistrationConfigurator<TDbContext> configurator)
        where TDbContext : DbContext
    {
        return configurator.AddSaga<SubscriptionPaymentSaga, SubscriptionPaymentSagaState>(
            endpointName: "subscription-payment-saga",
            configureRepository: r =>
            {
                // Custom configuration for subscription saga
                r.ConcurrencyMode = ConcurrencyMode.Optimistic; // Different strategy
            },
            configureEndpoint: e =>
            {
                e.ConcurrentMessageLimit = 5; // Allow more concurrent processing
            });
    }
}
```

### Step 4: Update AddMassTransitWithSagas Method

```csharp
public static IServiceCollection AddMassTransitWithSagas<TDbContext>(
    this IServiceCollection services, 
    IConfiguration configuration,
    Action<ISagaRegistrationConfigurator<TDbContext>> configureSagas, // ✅ Generic!
    Action<IRegistrationConfigurator>? configureConsumers = null,
    string connectionStringKey = "DefaultConnection")
    where TDbContext : DbContext
{
    // ... existing configuration ...

    services.AddMassTransit(x =>
    {
        // ✅ Let each service configure its own Sagas!
        var sagaConfigurator = new SagaRegistrationConfigurator<TDbContext>(x);
        configureSagas(sagaConfigurator);

        configureConsumers?.Invoke(x);

        x.UsingRabbitMq((context, cfg) =>
        {
            // ... existing RabbitMQ configuration ...
            
            cfg.ConfigureEndpoints(context);
            
            // ✅ Configure saga endpoints dynamically
            foreach (var (endpointName, sagaStateType, endpointConfig) in sagaConfigurator.GetSagaEndpoints())
            {
                cfg.ReceiveEndpoint(endpointName, e =>
                {
                    // Use reflection to configure saga
                    var configureSagaMethod = typeof(SagaConfiguratorExtensions)
                        .GetMethod(nameof(SagaConfiguratorExtensions.ConfigureSaga))
                        ?.MakeGenericMethod(sagaStateType);

                    configureSagaMethod?.Invoke(null, new object[] { e, context });

                    // Default config
                    e.UseMessageRetry(r => r.None());
                    e.DiscardFaultedMessages();
                    e.ConcurrentMessageLimit = 1;
                    e.PrefetchCount = 1;

                    // Apply custom config
                    endpointConfig?.Invoke(e);
                });
            }
        });
    });

    return services;
}
```

### Step 5: Add Legacy Support

```csharp
/// <summary>
/// LEGACY METHOD - For backward compatibility
/// Will be deprecated - use AddMassTransitWithSagas instead
/// </summary>
[Obsolete("Use AddMassTransitWithSagas with configureSagas parameter for better flexibility")]
public static IServiceCollection AddMassTransitWithSaga<TDbContext>(
    this IServiceCollection services, 
    IConfiguration configuration,
    Action<IRegistrationConfigurator>? configureConsumers = null,
    string connectionStringKey = "DefaultConnection")
    where TDbContext : DbContext
{
    // Redirect to new method with RegistrationSaga as default
    return services.AddMassTransitWithSagas<TDbContext>(
        configuration,
        sagas => sagas.AddRegistrationSaga(), // ✅ Default behavior
        configureConsumers,
        connectionStringKey);
}
```

---

## 📚 Example Usage

### AuthService (hosts RegistrationSaga)

```csharp
// AuthService/AuthService.API/Configurations/ServiceConfiguration.cs
public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
{
    builder.ConfigureMicroserviceServices("AuthService");
    builder.Services.AddMicroserviceDistributedAuth(builder.Configuration);

    // ✅ NEW WAY: Explicit Saga registration
    builder.Services.AddMassTransitWithSagas<AuthDbContext>(
        builder.Configuration,
        sagas => sagas.AddRegistrationSaga(), // Only RegistrationSaga
        consumers =>
        {
            consumers.AddConsumer<CreateAuthUserConsumer>();
            consumers.AddConsumer<DeleteAuthUserConsumer>();
        });

    // OR use legacy method (deprecated but still works)
    builder.Services.AddMassTransitWithSaga<AuthDbContext>(
        builder.Configuration,
        consumers =>
        {
            consumers.AddConsumer<CreateAuthUserConsumer>();
            consumers.AddConsumer<DeleteAuthUserConsumer>();
        });

    builder.Services.AddAuthApplication();
    builder.Services.AddAuthInfrastructure(builder.Configuration);

    return builder;
}
```

### SubscriptionService (hosts SubscriptionPaymentSaga - future)

```csharp
// SubscriptionService/SubscriptionService.API/Configurations/ServiceConfiguration.cs
public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
{
    builder.ConfigureMicroserviceServices("SubscriptionService");
    builder.Services.AddMicroserviceDistributedAuth(builder.Configuration);

    // ✅ NEW WAY: Host SubscriptionPaymentSaga (when implemented)
    builder.Services.AddMassTransitWithSagas<SubscriptionDbContext>(
        builder.Configuration,
        sagas => sagas.AddSubscriptionPaymentSaga(), // Different Saga!
        consumers =>
        {
            // Add subscription-related consumers here
        });

    // ❌ TEMPORARILY: No Saga needed yet, use consumers only
    builder.Services.AddMassTransitWithConsumers(
        builder.Configuration,
        consumers =>
        {
            // TODO: Add consumers when needed
        });

    builder.Services.AddSubscriptionApplication();
    builder.Services.AddSubscriptionInfrastructure(builder.Configuration);

    return builder;
}
```

### UserService (NO Saga, only consumers)

```csharp
// UserService/UserService.Infrastructure/UserInfrastructureDependencyInjection.cs
public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
{
    builder.ConfigureMicroserviceServices("UserService");

    // ✅ No Saga - only consumers
    builder.Services.AddMassTransitWithConsumers(
        builder.Configuration,
        consumers =>
        {
            consumers.AddConsumer<CreateUserProfileConsumer>();
            consumers.AddConsumer<DeleteUserProfileConsumer>();
        });

    builder.Services.AddUserApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    return builder;
}
```

### NotificationService (NO Saga, only consumers)

```csharp
// NotificationService/NotificationService.Infrastructure/NotificationInfrastructureDependencyInjection.cs
public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
{
    builder.ConfigureMicroserviceServices("NotificationService");

    // ✅ No Saga - only consumers
    builder.Services.AddMassTransitWithConsumers(
        builder.Configuration,
        consumers =>
        {
            consumers.AddConsumer<SendOtpNotificationConsumer>();
            consumers.AddConsumer<SendWelcomeNotificationConsumer>();
        });

    builder.Services.AddNotificationApplication();
    builder.Services.AddNotificationInfrastructure(builder.Configuration);

    return builder;
}
```

---

## 🎯 Benefits

### 1. **Separation of Concerns**
Each service explicitly declares which Sagas it hosts:
- AuthService → RegistrationSaga
- SubscriptionService → SubscriptionPaymentSaga
- UserService → NO Saga
- NotificationService → NO Saga

### 2. **Type Safety**
Generic constraints ensure type safety at compile time:
```csharp
where TDbContext : DbContext
where TSaga : class, SagaStateMachine<TSagaState>
where TSagaState : class, SagaStateMachineInstance
```

### 3. **Flexibility**
Each Saga can have custom configuration:
```csharp
sagas => sagas
    .AddRegistrationSaga()
    .AddSubscriptionPaymentSaga() // Can add multiple!
```

### 4. **Clean Database Migrations**
Each service only has tables for its own Sagas:
- `authservicedb` → `RegistrationSagaStates` table
- `subscriptiondb` → `SubscriptionPaymentSagaStates` table
- `userservicedb` → NO Saga tables
- `notificationdb` → NO tables (no DB)

### 5. **Easy Testing**
Can mock `ISagaRegistrationConfigurator` for unit tests

---

##  Migration Plan

### Phase 1: Implement Generic Configuration ✅ (This document)
- [ ] Create `ISagaRegistrationConfigurator<TDbContext>` interface
- [ ] Implement `SagaRegistrationConfigurator<TDbContext>` class  
- [ ] Create extension methods (`AddRegistrationSaga`, etc.)
- [ ] Update `AddMassTransitWithSagas` method
- [ ] Add `[Obsolete]` to old `AddMassTransitWithSaga`

### Phase 2: Update AuthService
- [ ] Change to new API in `ServiceConfiguration.cs`
- [ ] Test RegistrationSaga still works
- [ ] Verify database migrations unchanged

### Phase 3: Fix SubscriptionService
- [ ] Remove Saga registration (use `AddMassTransitWithConsumers`)
- [ ] Drop `RegistrationSagaStates` table migration
- [ ] Verify no Saga logs appear

### Phase 4: Verify Other Services
- [ ] UserService → Confirm using `AddMassTransitWithConsumers`
- [ ] NotificationService → Confirm using `AddMassTransitWithConsumers`
- [ ] ContentService → Confirm using `AddMassTransitWithConsumers`
- [ ] PaymentService → Confirm using `AddMassTransitWithConsumers`

### Phase 5: Add SubscriptionPaymentSaga (Future)
- [ ] Create `SubscriptionPaymentSaga.cs`
- [ ] Create `SubscriptionPaymentSagaState.cs`  
- [ ] Add `AddSubscriptionPaymentSaga()` extension method
- [ ] Generate EF migrations for `SubscriptionPaymentSagaStates` table
- [ ] Update SubscriptionService configuration

---

## 📊 Comparison: Before vs After

### Before (Current - Hard-coded)

```csharp
// ❌ All services forced to use RegistrationSaga
builder.Services.AddMassTransitWithSaga<SubscriptionDbContext>(...);
// Result: SubscriptionService has RegistrationSaga logs + table 😞
```

### After (Generic - Flexible)

```csharp
// ✅ AuthService explicitly hosts RegistrationSaga
builder.Services.AddMassTransitWithSagas<AuthDbContext>(
    config, 
    sagas => sagas.AddRegistrationSaga());

// ✅ SubscriptionService hosts NO Saga (for now)
builder.Services.AddMassTransitWithConsumers(config);

// ✅ Future: SubscriptionService hosts SubscriptionPaymentSaga
builder.Services.AddMassTransitWithSagas<SubscriptionDbContext>(
    config,
    sagas => sagas.AddSubscriptionPaymentSaga());
```

---

## 🔗 Related Files

**Implementation:**
- `SharedLibrary/Commons/Configurations/MassTransitSagaConfiguration.cs`

**Usage:**
- `AuthService/AuthService.API/Configurations/ServiceConfiguration.cs`
- `SubscriptionService/SubscriptionService.API/Configurations/ServiceConfiguration.cs`  
- `UserService/UserService.Infrastructure/UserInfrastructureDependencyInjection.cs`
- `NotificationService/NotificationService.Infrastructure/NotificationInfrastructureDependencyInjection.cs`

**Sagas:**
- `SharedLibrary/Contracts/User/Saga/RegistrationSaga.cs`
- `SharedLibrary/Contracts/User/Saga/RegistrationSagaState.cs`
- (Future) `SharedLibrary/Contracts/Subscription/Saga/SubscriptionPaymentSaga.cs`

---

**Generated:** 2025-10-02  
**Purpose:** Generic Saga Configuration Architecture  
**Status:** 📋 Design Document (Implementation Pending)
