# MassTransit Entity Framework Outbox Setup Guide

## 📚 Reference Documentation

- [MassTransit Transactional Outbox Configuration](https://masstransit.io/documentation/configuration/middleware/outbox)
- [MassTransit Transaction Outbox Pattern](https://masstransit.io/documentation/patterns/transactional-outbox)
- [MassTransit Transaction Filter](https://masstransit.io/documentation/configuration/middleware/transactions)

## 🎯 What is Entity Framework Outbox?

MassTransit's Entity Framework Outbox provides **transactional guarantees** for message publishing by using database tables to store messages before they are delivered to the broker.

### The Problem (Dual-Write)

```csharp
// ❌ NOT TRANSACTIONAL!
await _dbContext.Subscriptions.AddAsync(subscription);
await _dbContext.SaveChangesAsync();

await _publishEndpoint.Publish(new SubscriptionCreated { ... }); // Fails → inconsistency!
```

### The Solution (Transactional Outbox)

```csharp
// ✅ TRANSACTIONAL with Bus Outbox!
await _dbContext.Subscriptions.AddAsync(subscription);

// Message saved to OutboxMessage table (not sent yet)
await _publishEndpoint.Publish(new SubscriptionCreated { ... });

// ATOMIC: Both subscription and outbox message committed together
await _dbContext.SaveChangesAsync();

// Background service delivers message from outbox after commit
```

## 📊 Architecture

### MassTransit Outbox Tables

| Table | Purpose |
|-------|---------|
| `InboxState` | Tracks received messages by MessageId for deduplication (prevents duplicate processing) |
| `OutboxMessage` | Stores messages published via `IPublishEndpoint` or `ISendEndpointProvider` |
| `OutboxState` | Tracks delivery of outbox messages by the background delivery service |

### Two Types of Outbox

#### 1. **Bus Outbox** (for HTTP Handlers / Command Handlers)

Protects `IPublishEndpoint.Publish()` calls **outside of consumers**:

```csharp
// In HTTP handler/command handler
await _publishEndpoint.Publish(new OrderCreated { ... });
await _dbContext.SaveChangesAsync(); // ✅ Transactional!
```

#### 2. **Consumer Outbox** (for Consumers)

Protects `context.Publish()` calls **inside consumers**:

```csharp
// In consumer
public async Task Consume(ConsumeContext<OrderCreated> context)
{
    // ... db operations
    await context.Publish(new PaymentRequested { ... }); // ✅ Transactional!
}
```

## 🔧 Implementation Steps

### Step 1: Add MassTransit Outbox Entities to DbContext

```csharp
using MassTransit;
using Microsoft.EntityFrameworkCore;

public class SubscriptionDbContext : DbContext
{
    public SubscriptionDbContext(DbContextOptions<SubscriptionDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // CRITICAL: Add MassTransit Outbox entities
        builder.AddInboxStateEntity();      // For deduplication (consumer-side)
        builder.AddOutboxMessageEntity();   // For storing published messages
        builder.AddOutboxStateEntity();     // For tracking bus outbox delivery

        // ... your domain entities configuration
    }
}
```

### Step 2: Configure MassTransit with Entity Framework Outbox

```csharp
services.AddMassTransit(x =>
{
    // Register consumers, sagas, etc.
    x.AddConsumer<PaymentConsumer>();

    // CRITICAL: Add Entity Framework Outbox
    x.AddEntityFrameworkOutbox<SubscriptionDbContext>(o =>
    {
        // Use PostgreSQL lock provider (or UseSqlServer, UseMySql)
        o.UsePostgres();

        // Enable Bus Outbox for IPublishEndpoint
        o.UseBusOutbox();

        // Duplicate detection window (inbox deduplication)
        o.DuplicateDetectionWindow = TimeSpan.FromSeconds(30);
        
        // Query settings for outbox delivery service
        o.QueryDelay = TimeSpan.FromSeconds(1);
        o.QueryMessageLimit = 100;
    });

    // Configure transport
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});
```

### Step 3: Enable Consumer Outbox (Optional, per-consumer)

#### Option A: Using ConsumerDefinition

```csharp
public class PaymentConsumerDefinition : ConsumerDefinition<PaymentConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<PaymentConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        // Enable outbox for this consumer
        endpointConfigurator.UseEntityFrameworkOutbox<SubscriptionDbContext>(context);
    }
}

// Register with definition
x.AddConsumer<PaymentConsumer, PaymentConsumerDefinition>();
```

#### Option B: Using ConfigureEndpoints Callback (Global)

```csharp
x.AddConfigureEndpointsCallback((context, name, cfg) =>
{
    // Apply outbox to ALL consumers
    cfg.UseEntityFrameworkOutbox<SubscriptionDbContext>(context);
});
```

### Step 4: Create Database Migration

```bash
dotnet ef migrations add AddMassTransitOutbox -p SubscriptionService.Infrastructure -s SubscriptionService.API
dotnet ef database update -p SubscriptionService.Infrastructure -s SubscriptionService.API
```

## 📋 Configuration Options

### Entity Framework Outbox Options

```csharp
x.AddEntityFrameworkOutbox<TDbContext>(o =>
{
    // Lock provider (required for pessimistic locking)
    o.UsePostgres();        // For PostgreSQL
    // o.UseSqlServer();    // For SQL Server
    // o.UseMySql();        // For MySQL

    // Enable Bus Outbox for IPublishEndpoint
    o.UseBusOutbox();

    // Duplicate detection window (inbox deduplication)
    o.DuplicateDetectionWindow = TimeSpan.FromSeconds(30);

    // Transaction isolation level
    o.IsolationLevel = IsolationLevel.ReadCommitted;

    // Query settings for outbox delivery service
    o.QueryDelay = TimeSpan.FromSeconds(1);
    o.QueryMessageLimit = 100;
    o.QueryTimeout = TimeSpan.FromSeconds(30);
});
```

### Bus Outbox Options

```csharp
services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<TDbContext>(o =>
    {
        o.UsePostgres();
        
        o.UseBusOutbox(busOutbox =>
        {
            // Number of messages to deliver at a time
            busOutbox.MessageDeliveryLimit = 100;
            
            // Transport Send timeout when delivering messages
            busOutbox.MessageDeliveryTimeout = TimeSpan.FromSeconds(30);
        });
    });
});
```

## 🎨 Usage Patterns

### Pattern 1: HTTP Handler with Bus Outbox

```csharp
[ApiController]
[Route("api/subscriptions")]
public class SubscriptionController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly SubscriptionDbContext _dbContext;

    [HttpPost]
    public async Task<IActionResult> CreateSubscription(CreateSubscriptionRequest request)
    {
        // 1. Create domain entity
        var subscription = new Subscription
        {
            UserProfileId = request.UserId,
            SubscriptionPlanId = request.PlanId
        };
        
        await _dbContext.Subscriptions.AddAsync(subscription);

        // 2. Queue message to outbox (not sent yet)
        await _publishEndpoint.Publish(new SubscriptionCreated
        {
            SubscriptionId = subscription.Id,
            UserId = subscription.UserProfileId
        });

        // 3. ATOMIC: Commit subscription + outbox message
        await _dbContext.SaveChangesAsync();

        // 4. Background service delivers message from outbox
        
        return Ok();
    }
}
```

### Pattern 2: Command Handler with CQRS

```csharp
public class CreateSubscriptionCommandHandler : IRequestHandler<CreateSubscriptionCommand, Result>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly SubscriptionDbContext _dbContext;

    public async Task<Result> Handle(CreateSubscriptionCommand command, CancellationToken ct)
    {
        var subscription = new Subscription { /* ... */ };
        await _dbContext.Subscriptions.AddAsync(subscription, ct);

        // Queue to outbox
        await _publishEndpoint.Publish(new SubscriptionCreated
        {
            SubscriptionId = subscription.Id
        }, ct);

        // Atomic commit
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}
```

### Pattern 3: Consumer with Outbox

```csharp
public class PaymentSucceededConsumer : IConsumer<PaymentSucceeded>
{
    private readonly SubscriptionDbContext _dbContext;

    public async Task Consume(ConsumeContext<PaymentSucceeded> context)
    {
        // 1. Update subscription
        var subscription = await _dbContext.Subscriptions
            .FindAsync(context.Message.SubscriptionId);
        
        subscription.Activate();

        // 2. Publish event (goes to outbox)
        await context.Publish(new SubscriptionActivated
        {
            SubscriptionId = subscription.Id
        });

        // 3. ATOMIC: Both subscription update and outbox message
        await _dbContext.SaveChangesAsync();
    }
}

// Configure with outbox
public class PaymentSucceededConsumerDefinition : ConsumerDefinition<PaymentSucceededConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<PaymentSucceededConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseEntityFrameworkOutbox<SubscriptionDbContext>(context);
    }
}
```

## ⚙️ How It Works

### Bus Outbox Flow

```
HTTP Request → Handler
    ↓
1. _publishEndpoint.Publish(message)
    ├─ Intercepted by Bus Outbox
    └─ INSERT INTO OutboxMessage (message as JSON)
    
2. _dbContext.SaveChangesAsync()
    ├─ BEGIN TRANSACTION
    ├─ INSERT entity
    ├─ INSERT OutboxMessage
    └─ COMMIT ✓
    
3. Background Delivery Service (Hosted Service)
    ├─ SELECT * FROM OutboxMessage WHERE Delivered = false
    ├─ Publish to RabbitMQ
    └─ UPDATE OutboxMessage SET Delivered = true
```

### Consumer Outbox Flow

```
RabbitMQ Message → Consumer
    ↓
1. Check InboxState (deduplication)
    └─ If MessageId exists → skip (already processed)
    
2. Begin Transaction
    ├─ INSERT INTO InboxState (MessageId, ...)
    ├─ Execute consumer logic
    ├─ INSERT INTO OutboxMessage (for context.Publish)
    └─ COMMIT ✓
    
3. Background Delivery Service
    └─ Deliver OutboxMessage to broker
```

## ✅ Best Practices

### 1. Always Use Bus Outbox for HTTP Handlers

```csharp
// ✅ GOOD: Bus Outbox enabled
x.AddEntityFrameworkOutbox<TDbContext>(o =>
{
    o.UsePostgres();
    o.UseBusOutbox(); // ← CRITICAL for HTTP handlers!
});
```

### 2. Apply Consumer Outbox Selectively

```csharp
// ✅ GOOD: Only consumers that publish events
public class OrderConsumerDefinition : ConsumerDefinition<OrderConsumer>
{
    protected override void ConfigureConsumer(...)
    {
        // This consumer publishes events → needs outbox
        endpointConfigurator.UseEntityFrameworkOutbox<OrderDbContext>(context);
    }
}

// ❌ NOT NEEDED: Consumer that only reads/logs
public class LoggingConsumer : IConsumer<OrderCreated>
{
    // No db writes, no publishing → no outbox needed
}
```

### 3. Set Appropriate Deduplication Window

```csharp
o.DuplicateDetectionWindow = TimeSpan.FromSeconds(30);
// Should be > max message processing time
// Inbox entries older than this are cleaned up
```

### 4. Monitor Outbox Tables

```sql
-- Check pending messages
SELECT * FROM "OutboxMessage" 
WHERE "SentTime" IS NULL
ORDER BY "EnqueueTime" DESC;

-- Check inbox for duplicates
SELECT MessageId, COUNT(*) 
FROM "InboxState"
GROUP BY MessageId
HAVING COUNT(*) > 1;
```

## 🆚 Comparison with Custom OutboxUnitOfWork

| Feature | MassTransit EF Outbox | Custom OutboxUnitOfWork |
|---------|----------------------|------------------------|
| **Official Support** | ✅ Yes | ❌ Custom |
| **Bus Outbox** | ✅ Built-in | ❌ Manual reflection |
| **Consumer Outbox** | ✅ Built-in | ❌ Not supported |
| **Inbox Deduplication** | ✅ Built-in | ❌ Manual |
| **Background Delivery** | ✅ Hosted service | ⚠️ Custom processor |
| **Pessimistic Locking** | ✅ Database locks | ❌ Not implemented |
| **Retries** | ✅ Automatic | ⚠️ Manual exponential backoff |
| **Monitoring** | ✅ Built-in tables | ⚠️ Custom OutboxEvent |
| **Clean Architecture** | ✅ Interface-based | ✅ Yes |

**Verdict:** Use **MassTransit Entity Framework Outbox** for production systems! ✅

## 🔧 Troubleshooting

### Issue: Messages not being delivered

**Check:**
```sql
SELECT * FROM "OutboxMessage" WHERE "SentTime" IS NULL;
```

**Solution:** Ensure `UseBusOutbox()` is enabled and hosted service is running.

### Issue: Duplicate message processing

**Check:**
```sql
SELECT MessageId, COUNT(*) FROM "InboxState" GROUP BY MessageId HAVING COUNT(*) > 1;
```

**Solution:** Ensure `UseEntityFrameworkOutbox<TDbContext>()` is on consumer endpoint.

### Issue: Transaction deadlocks

**Solution:** Adjust `IsolationLevel`:
```csharp
o.IsolationLevel = IsolationLevel.ReadCommitted; // Less strict
```

## 📊 Summary

### ✅ Implemented for

- ✅ SubscriptionService (Bus + Consumer Outbox)
- ✅ PaymentService (Bus + Consumer Outbox)
- ✅ AuthService (Consumer Outbox for Saga)

### 🎯 Benefits

1. **Transactional Guarantees**: Entity changes + messages atomic
2. **Idempotency**: Inbox deduplication prevents duplicate processing
3. **Reliability**: Messages persisted to database before delivery
4. **Official Support**: Well-tested, production-ready
5. **Clean Architecture**: No custom outbox implementation needed

### 📚 Next Steps

1. ✅ Database migrations for outbox tables
2. 🔜 Implement SubscriptionSaga state machine
3. 🔜 Create payment handling consumers
4. 🔜 Test end-to-end subscription flow

---

**Date:** 2025-01-08  
**Pattern:** MassTransit Entity Framework Transactional Outbox  
**Status:** ✅ Configured for SubscriptionService + PaymentService

