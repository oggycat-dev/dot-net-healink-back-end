# 📨 Outbox Event Integration - Subscription Management

## 🎯 Overview

Tất cả các Command Handler trong Subscription Management đã được cập nhật để publish Integration Events thông qua Outbox Pattern. Các event này sẽ được RabbitMQ consumer xử lý để ghi log User Activity.

---

## 📊 Event Flow Architecture

```
Command Handler
    ↓
Create Entity + Outbox Event
    ↓
SaveChangesWithOutboxAsync()
    ↓
Database Transaction (Entity + OutboxMessage)
    ↓
Outbox Publisher Background Service
    ↓
RabbitMQ
    ↓
Consumer (UserService)
    ↓
Write UserActivityLog
```

---

## 📡 Integration Events Created

### 1. **CreateSubscriptionPlanEvent** ✅
**File:** `SharedLibrary/Contracts/Subscription/CreateSubscriptionPlanEvent.cs`

**Properties:**
```csharp
public record CreateSubscriptionPlanEvent : IntegrationEvent
{
    public string Name { get; init; }
    public string DisplayName { get; init; }
    public string Description { get; init; }
    public string FeatureConfig { get; init; }
    public string Currency { get; init; }
    public int BillingPeriodCount { get; init; }
    public string BillingPeriodUnit { get; init; }
    public decimal Amount { get; init; }
    public int TrialDays { get; init; }
}
```

**Handler:** `CreateSubscriptionPlanCommandHandler`  
**Action:** Plan created by Admin/Staff  
**Log Activity:** "SubscriptionPlanCreated"

---

### 2. **SubscriptionPlanUpdatedEvent** ✅
**File:** `SharedLibrary/Contracts/Subscription/SubscriptionPlanUpdatedEvent.cs`

**Properties:**
```csharp
public record SubscriptionPlanUpdatedEvent : IntegrationEvent
{
    public Guid SubscriptionPlanId { get; init; }
    public string Name { get; init; }
    public string DisplayName { get; init; }
    public string Description { get; init; }
    public string FeatureConfig { get; init; }
    public decimal Amount { get; init; }
    public int TrialDays { get; init; }
    public int BillingPeriodCount { get; init; }
    public Guid? UpdatedBy { get; init; }
}
```

**Handler:** `UpdateSubscriptionPlanCommandHandler`  
**Action:** Plan updated by Admin/Staff  
**Log Activity:** "SubscriptionPlanUpdated"

---

### 3. **SubscriptionPlanDeletedEvent** ✅
**File:** `SharedLibrary/Contracts/Subscription/SubscriptionPlanDeletedEvent.cs`

**Properties:**
```csharp
public record SubscriptionPlanDeletedEvent : IntegrationEvent
{
    public Guid SubscriptionPlanId { get; init; }
    public string Name { get; init; }
    public string DisplayName { get; init; }
    public Guid? DeletedBy { get; init; }
    public DateTime DeletedAt { get; init; }
}
```

**Handler:** `DeleteSubscriptionPlanCommandHandler`  
**Action:** Plan soft deleted by Admin/Staff  
**Log Activity:** "SubscriptionPlanDeleted"

---

### 4. **SubscriptionUpdatedEvent** ✅
**File:** `SharedLibrary/Contracts/Subscription/SubscriptionUpdatedEvent.cs`

**Properties:**
```csharp
public record SubscriptionUpdatedEvent : IntegrationEvent
{
    public Guid SubscriptionId { get; init; }
    public Guid UserProfileId { get; init; }
    public Guid SubscriptionPlanId { get; init; }
    public string PlanName { get; init; }
    public string SubscriptionStatus { get; init; }
    public string RenewalBehavior { get; init; }
    public bool CancelAtPeriodEnd { get; init; }
    public DateTime? CurrentPeriodEnd { get; init; }
    public Guid? UpdatedBy { get; init; }
}
```

**Handler:** `UpdateSubscriptionCommandHandler`  
**Action:** User subscription updated by Admin/Staff  
**Log Activity:** "SubscriptionUpdated"

---

### 5. **SubscriptionCanceledEvent** ✅
**File:** `SharedLibrary/Contracts/Subscription/SubscriptionCanceledEvent.cs`

**Properties:**
```csharp
public record SubscriptionCanceledEvent : IntegrationEvent
{
    public Guid SubscriptionId { get; init; }
    public Guid UserProfileId { get; init; }
    public Guid SubscriptionPlanId { get; init; }
    public string PlanName { get; init; }
    public bool CancelAtPeriodEnd { get; init; }
    public DateTime? CancelAt { get; init; }
    public DateTime? CanceledAt { get; init; }
    public string? Reason { get; init; }
    public Guid? CanceledBy { get; init; }
}
```

**Handler:** `CancelSubscriptionCommandHandler`  
**Action:** Subscription canceled by Admin/Staff  
**Log Activity:** "SubscriptionCanceled"

---

## 🔧 Handler Implementation Pattern

### Example: CreateSubscriptionPlanCommandHandler

```csharp
public async Task<Result<SubscriptionPlanResponse>> Handle(
    CreateSubscriptionPlanCommand request,
    CancellationToken cancellationToken)
{
    try
    {
        var currentUserID = _currentUserService.UserId;
        var repository = _unitOfWork.Repository<SubscriptionPlan>();

        // 1. Business Logic
        var plan = _mapper.Map<SubscriptionPlan>(request.Request);
        plan.InitializeEntity(Guid.Parse(currentUserID));
        await repository.AddAsync(plan);

        // 2. Create Integration Event
        var createEvent = new CreateSubscriptionPlanEvent
        {
            Name = plan.Name,
            DisplayName = plan.DisplayName,
            Description = plan.Description,
            FeatureConfig = plan.FeatureConfig,
            Currency = plan.Currency,
            BillingPeriodCount = plan.BillingPeriodCount,
            BillingPeriodUnit = plan.BillingPeriodUnit.ToString(),
            Amount = plan.Amount,
            TrialDays = plan.TrialDays
        };

        // 3. Add to Outbox
        await _unitOfWork.AddOutboxEventAsync(createEvent);

        // 4. Save with Transaction
        await _unitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

        return Result<SubscriptionPlanResponse>.Success(response);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating subscription plan");
        return Result<SubscriptionPlanResponse>.Failure("Failed", ErrorCodeEnum.InternalError);
    }
}
```

---

## 📝 Updated Command Handlers

### ✅ CreateSubscriptionPlanCommandHandler
**Event:** `CreateSubscriptionPlanEvent`  
**Published:** When new plan created  
**Includes:** Plan details, billing info, trial days

### ✅ UpdateSubscriptionPlanCommandHandler  
**Event:** `SubscriptionPlanUpdatedEvent`  
**Published:** When plan updated  
**Includes:** Updated fields, UpdatedBy user ID

### ✅ DeleteSubscriptionPlanCommandHandler
**Event:** `SubscriptionPlanDeletedEvent`  
**Published:** When plan soft deleted  
**Includes:** DeletedBy user ID, DeletedAt timestamp

### ✅ UpdateSubscriptionCommandHandler
**Event:** `SubscriptionUpdatedEvent`  
**Published:** When subscription settings updated  
**Includes:** Status, renewal behavior, period info

### ✅ CancelSubscriptionCommandHandler
**Event:** `SubscriptionCanceledEvent`  
**Published:** When subscription canceled  
**Includes:** Cancel reason, cancel date, CanceledBy user ID

---

## 🎨 Consumer Implementation (UserService)

### RabbitMQ Consumer Example

```csharp
public class SubscriptionEventConsumer : IHostedService
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _channel.QueueDeclare("subscription.events", durable: true);
        
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var eventType = ea.BasicProperties.Type;

            await HandleEventAsync(eventType, message);
            _channel.BasicAck(ea.DeliveryTag, false);
        };

        _channel.BasicConsume("subscription.events", false, consumer);
        return Task.CompletedTask;
    }

    private async Task HandleEventAsync(string eventType, string message)
    {
        switch (eventType)
        {
            case nameof(CreateSubscriptionPlanEvent):
                await LogActivityAsync("SubscriptionPlanCreated", message);
                break;
            case nameof(SubscriptionPlanUpdatedEvent):
                await LogActivityAsync("SubscriptionPlanUpdated", message);
                break;
            case nameof(SubscriptionPlanDeletedEvent):
                await LogActivityAsync("SubscriptionPlanDeleted", message);
                break;
            case nameof(SubscriptionUpdatedEvent):
                await LogActivityAsync("SubscriptionUpdated", message);
                break;
            case nameof(SubscriptionCanceledEvent):
                await LogActivityAsync("SubscriptionCanceled", message);
                break;
        }
    }

    private async Task LogActivityAsync(string activityType, string eventData)
    {
        var log = new UserActivityLog
        {
            UserId = ExtractUserId(eventData),
            ActivityType = activityType,
            Description = $"{activityType} at {DateTime.UtcNow}",
            Metadata = eventData,
            OccurredAt = DateTime.UtcNow
        };

        await _activityLogRepository.AddAsync(log);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

---

## 📊 UserActivityLog Entries

### Example Log Records

#### 1. Plan Created
```json
{
    "userId": "admin-guid",
    "activityType": "SubscriptionPlanCreated",
    "description": "Created new subscription plan: Premium Plan",
    "metadata": {
        "planName": "premium",
        "displayName": "Premium Plan",
        "amount": 99000,
        "currency": "VND"
    },
    "occurredAt": "2025-10-02T10:00:00Z"
}
```

#### 2. Plan Updated
```json
{
    "userId": "admin-guid",
    "activityType": "SubscriptionPlanUpdated",
    "description": "Updated subscription plan: Premium Plan",
    "metadata": {
        "subscriptionPlanId": "plan-guid",
        "updatedFields": ["amount", "trialDays"],
        "updatedBy": "admin-guid"
    },
    "occurredAt": "2025-10-02T11:00:00Z"
}
```

#### 3. Subscription Canceled
```json
{
    "userId": "user-guid",
    "activityType": "SubscriptionCanceled",
    "description": "Canceled subscription for user",
    "metadata": {
        "subscriptionId": "sub-guid",
        "planName": "Premium Plan",
        "reason": "User requested cancellation",
        "cancelAtPeriodEnd": true,
        "canceledBy": "admin-guid"
    },
    "occurredAt": "2025-10-02T12:00:00Z"
}
```

---

## ✅ Benefits of Outbox Pattern

### 1. **Transactional Consistency**
- Event and entity saved in same transaction
- No lost events if save fails
- Atomic operations

### 2. **Guaranteed Delivery**
- Events persisted to database
- Background service publishes to RabbitMQ
- Retry mechanism for failures

### 3. **Audit Trail**
- All actions logged in UserActivityLog
- Complete history of changes
- Compliance and debugging

### 4. **Decoupling**
- SubscriptionService doesn't know about UserService
- Events processed asynchronously
- Easy to add new consumers

### 5. **Scalability**
- Events processed in background
- No blocking on message publish
- Can handle high throughput

---

## 🔍 Monitoring & Debugging

### Check Outbox Messages
```sql
SELECT * FROM outbox_messages 
WHERE event_type LIKE '%Subscription%'
ORDER BY created_at DESC;
```

### Check Activity Logs
```sql
SELECT * FROM user_activity_logs 
WHERE activity_type LIKE '%Subscription%'
ORDER BY occurred_at DESC;
```

### RabbitMQ Management
```bash
# Check queue
rabbitmqctl list_queues name messages

# Check consumers
rabbitmqctl list_consumers
```

---

## 📦 Event Payload Examples

### CreateSubscriptionPlanEvent
```json
{
    "id": "event-guid",
    "eventType": "CreateSubscriptionPlanEvent",
    "sourceService": "SubscriptionService",
    "creationDate": "2025-10-02T10:00:00Z",
    "name": "premium",
    "displayName": "Premium Plan",
    "description": "Full access to all features",
    "featureConfig": "{\"maxProjects\": 10}",
    "currency": "VND",
    "billingPeriodCount": 1,
    "billingPeriodUnit": "Month",
    "amount": 99000,
    "trialDays": 7
}
```

### SubscriptionCanceledEvent
```json
{
    "id": "event-guid",
    "eventType": "SubscriptionCanceledEvent",
    "sourceService": "SubscriptionService",
    "creationDate": "2025-10-02T12:00:00Z",
    "subscriptionId": "sub-guid",
    "userProfileId": "user-guid",
    "subscriptionPlanId": "plan-guid",
    "planName": "Premium Plan",
    "cancelAtPeriodEnd": true,
    "cancelAt": "2025-11-02T12:00:00Z",
    "canceledAt": null,
    "reason": "User requested",
    "canceledBy": "admin-guid"
}
```

---

## 🚀 Testing

### Test Event Publishing
```csharp
[Fact]
public async Task CreatePlan_Should_PublishEvent()
{
    // Arrange
    var command = new CreateSubscriptionPlanCommand(...);
    
    // Act
    var result = await _handler.Handle(command, CancellationToken.None);
    
    // Assert
    Assert.True(result.IsSuccess);
    
    var outboxEvents = await _dbContext.OutboxMessages
        .Where(x => x.EventType == nameof(CreateSubscriptionPlanEvent))
        .ToListAsync();
    
    Assert.Single(outboxEvents);
}
```

### Test Consumer
```csharp
[Fact]
public async Task Consumer_Should_CreateActivityLog()
{
    // Arrange
    var @event = new CreateSubscriptionPlanEvent { ... };
    var message = JsonSerializer.Serialize(@event);
    
    // Act
    await _consumer.HandleEventAsync(nameof(CreateSubscriptionPlanEvent), message);
    
    // Assert
    var logs = await _dbContext.UserActivityLogs
        .Where(x => x.ActivityType == "SubscriptionPlanCreated")
        .ToListAsync();
    
    Assert.Single(logs);
}
```

---

## 📋 Summary

✅ **5 Integration Events** created for all subscription operations  
✅ **5 Command Handlers** updated to publish events  
✅ **Outbox Pattern** implemented for transactional consistency  
✅ **RabbitMQ Integration** ready for async processing  
✅ **UserActivityLog** ready for audit trail  
✅ **Complete traceability** of all admin actions  

---

**Status:** ✅ Complete  
**Last Updated:** October 2, 2025  
**Next Steps:** Implement RabbitMQ Consumer in UserService to write activity logs

