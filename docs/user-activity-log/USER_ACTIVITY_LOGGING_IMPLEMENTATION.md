# ✅ User Activity Logging Implementation - Clean Architecture

## 🎯 Overview

Triển khai hệ thống ghi log hoạt động user khi admin thao tác với Subscription Plans, tuân thủ **Clean Architecture** và **CQRS Pattern**.

---

## 🏗️ Architecture Design

### Layer Separation (Clean Architecture)

```
┌─────────────────────────────────────────────────────────────┐
│                     Subscription Service                     │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  Command Handlers (Create/Update/Delete)               │ │
│  │  → Publish Events to Outbox                            │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ (via RabbitMQ)
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                        User Service                          │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐│
│  │  Infrastructure Layer (Event Handlers)                 ││
│  │  ✅ SubscriptionPlanCreatedEventHandler                ││
│  │  ✅ SubscriptionPlanUpdatedEventHandler                ││
│  │  ✅ SubscriptionPlanDeletedEventHandler                ││
│  │  → Subscribe từ RabbitMQ                               ││
│  │  → Delegate to CQRS Command                            ││
│  └────────────────────────────────────────────────────────┘│
│                              │                               │
│                              ▼                               │
│  ┌────────────────────────────────────────────────────────┐│
│  │  Application Layer (CQRS Command)                      ││
│  │  ✅ CreateUserActivityLogCommand                       ││
│  │  ✅ CreateUserActivityLogCommandHandler                ││
│  │  → Contains business logic                             ││
│  │  → Uses Repository & UnitOfWork                        ││
│  └────────────────────────────────────────────────────────┘│
│                              │                               │
│                              ▼                               │
│  ┌────────────────────────────────────────────────────────┐│
│  │  Infrastructure Layer (Data Access)                    ││
│  │  ✅ UnitOfWork → Repository<UserActivityLog>           ││
│  │  → Save to Database                                    ││
│  └────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
```

---

## 📦 Components Created

### 1. **Application Layer - CQRS Command**

#### `CreateUserActivityLogCommand.cs`
```csharp
public record CreateUserActivityLogCommand : IRequest<Result>
{
    public Guid UserId { get; init; }
    public string ActivityType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Metadata { get; init; } = "{}";
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}
```

**Purpose:** Command object chứa data cần thiết để ghi log

#### `CreateUserActivityLogCommandHandler.cs`
```csharp
public class CreateUserActivityLogCommandHandler 
    : IRequestHandler<CreateUserActivityLogCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateUserActivityLogCommandHandler> _logger;

    public async Task<Result> Handle(
        CreateUserActivityLogCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Create UserActivityLog entity
        var activityLog = new UserActivityLog { ... };
        
        // 2. Add to repository
        await repository.AddAsync(activityLog);
        
        // 3. Save changes via UnitOfWork
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

**Key Points:**
- ✅ Contains business logic (entity creation)
- ✅ Uses Repository & UnitOfWork pattern
- ✅ Transaction management
- ✅ Logging for audit trail

---

### 2. **Infrastructure Layer - Event Handlers**

#### `SubscriptionPlanCreatedEventHandler.cs`
```csharp
public class SubscriptionPlanCreatedEventHandler 
    : IIntegrationEventHandler<SubscriptionPlanCreatedEvent>
{
    private readonly IMediator _mediator;
    
    public async Task Handle(
        SubscriptionPlanCreatedEvent @event, 
        CancellationToken cancellationToken)
    {
        // 1. Check if user action (skip if system)
        if (@event.CreatedBy == null || @event.CreatedBy == Guid.Empty)
            return;
        
        // 2. Build metadata
        var metadata = new { EventId, PlanName, Amount, ... };
        
        // 3. Delegate to CQRS Command
        var command = new CreateUserActivityLogCommand
        {
            UserId = @event.CreatedBy.Value,
            ActivityType = "SubscriptionPlanCreated",
            Description = $"Created subscription plan '{@event.DisplayName}'",
            Metadata = JsonSerializer.Serialize(metadata)
        };
        
        await _mediator.Send(command, cancellationToken);
    }
}
```

**Key Points:**
- ✅ Infrastructure layer (subscribes to RabbitMQ)
- ✅ Thin handler - delegates to Command
- ✅ Filters system actions (no user)
- ✅ Builds rich metadata for audit

#### Similarly Created:
- ✅ `SubscriptionPlanUpdatedEventHandler.cs`
- ✅ `SubscriptionPlanDeletedEventHandler.cs`

---

## 🔧 Configuration

### DependencyInjection Registration

**File:** `UserInfrastructureDependencyInjection.cs`

```csharp
// Register Event Handlers for User Activity Logging
services.AddScoped<IIntegrationEventHandler<SubscriptionPlanCreatedEvent>, 
    SubscriptionPlanCreatedEventHandler>();
services.AddScoped<IIntegrationEventHandler<SubscriptionPlanUpdatedEvent>, 
    SubscriptionPlanUpdatedEventHandler>();
services.AddScoped<IIntegrationEventHandler<SubscriptionPlanDeletedEvent>, 
    SubscriptionPlanDeletedEventHandler>();
```

---

## 📊 Data Flow

### Complete Flow Example: Admin Creates Subscription Plan

```
1. Admin calls API: POST /api/subscription-plans
                              ↓
2. CreateSubscriptionPlanCommandHandler (Subscription Service)
   → Creates SubscriptionPlan entity
   → Publishes SubscriptionPlanCreatedEvent to Outbox
   → SaveChangesWithOutboxAsync() (atomic transaction)
                              ↓
3. OutboxPublisher Background Service
   → Reads from OutboxEvents table
   → Publishes to RabbitMQ
   → Marks event as Published
                              ↓
4. RabbitMQ Consumer (User Service)
   → SubscriptionPlanCreatedEventHandler receives event
                              ↓
5. Event Handler (Infrastructure)
   → Validates: Has user? (skip if system action)
   → Builds metadata (plan details)
   → Creates CreateUserActivityLogCommand
   → Sends via MediatR
                              ↓
6. CreateUserActivityLogCommandHandler (Application)
   → Creates UserActivityLog entity
   → Uses Repository.AddAsync()
   → UnitOfWork.SaveChangesAsync()
                              ↓
7. Database: UserActivityLogs table
   → New log entry created
   ✅ Admin action tracked!
```

---

## 🎨 Activity Log Structure

### UserActivityLog Entity

```csharp
public class UserActivityLog : BaseEntity
{
    public Guid UserId { get; set; }              // Admin user who performed action
    public string ActivityType { get; set; }       // "SubscriptionPlanCreated"
    public string Description { get; set; }        // "Created subscription plan 'Premium'"
    public string Metadata { get; set; }           // JSON with full details
    public string? IpAddress { get; set; }         // Future: track IP
    public string? UserAgent { get; set; }         // Future: track browser
    public DateTime OccurredAt { get; set; }       // Timestamp
}
```

### Example Log Entry

```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "userId": "admin-user-id",
  "activityType": "SubscriptionPlanCreated",
  "description": "Created subscription plan 'Premium Plan' (premium)",
  "metadata": {
    "eventId": "event-id",
    "eventType": "SubscriptionPlanCreatedEvent",
    "subscriptionPlanId": "plan-id",
    "planName": "premium",
    "displayName": "Premium Plan",
    "amount": 99.99,
    "currency": "VND",
    "billingPeriod": "1 Month",
    "trialDays": 7
  },
  "occurredAt": "2025-10-02T10:30:00Z",
  "createdAt": "2025-10-02T10:30:01Z"
}
```

---

## ✅ Clean Architecture Principles Applied

### 1. **Separation of Concerns** ✅
```
Event Handlers (Infrastructure)  → Subscribe to events, minimal logic
      ↓
CQRS Commands (Application)      → Business logic, validation
      ↓
Repositories (Infrastructure)    → Data access
```

### 2. **Dependency Rule** ✅
```
Infrastructure → Application → Domain
(can depend)    (can depend)   (independent)

Event Handlers use MediatR → Commands use Repositories
```

### 3. **Single Responsibility** ✅
- **Event Handler**: Only subscribe and delegate
- **Command**: Only business logic
- **Repository**: Only data access

### 4. **Testability** ✅
Each layer can be tested independently:
- Mock `IMediator` for Event Handler tests
- Mock `IUnitOfWork` for Command Handler tests

---

## 🔒 Key Benefits

### 1. **Audit Trail** 📝
- Track every admin action on subscription plans
- Rich metadata for compliance
- Query by user, activity type, date range

### 2. **Decoupled Services** 🔌
- Subscription Service doesn't know about User Service
- Event-driven communication via RabbitMQ
- Services can scale independently

### 3. **Clean Architecture** 🏛️
- Clear layer boundaries
- Easy to test and maintain
- Business logic isolated in Command handlers

### 4. **CQRS Pattern** 📋
- Commands handle writes (activity logs)
- Future: Queries for activity log reports
- Separation of read/write concerns

### 5. **Resilience** 💪
- Outbox Pattern ensures event delivery
- If UserService is down, events wait in Outbox
- Retry mechanism for failed event processing

---

## 📈 Future Enhancements

### 1. **IP Address & User Agent Tracking**
```csharp
// In API layer, extract from HttpContext
var command = new CreateUserActivityLogCommand
{
    UserId = userId,
    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
    UserAgent = HttpContext.Request.Headers["User-Agent"]
};
```

### 2. **Activity Log Query APIs**
```csharp
// GetUserActivityLogsQuery
public record GetUserActivityLogsQuery : IRequest<Result<PaginatedList<UserActivityLogDto>>>
{
    public Guid? UserId { get; init; }
    public string? ActivityType { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}
```

### 3. **Real-time Notifications**
- SignalR hub for real-time activity feed
- Push notifications for critical actions

### 4. **Activity Retention Policy**
- Archive old logs to cold storage
- Compliance with data retention regulations

---

## 🧪 Testing Recommendations

### Unit Tests

```csharp
[Fact]
public async Task Handle_ValidCommand_CreatesActivityLog()
{
    // Arrange
    var mockUnitOfWork = new Mock<IUnitOfWork>();
    var handler = new CreateUserActivityLogCommandHandler(
        mockUnitOfWork.Object, 
        Mock.Of<ILogger>());
    
    var command = new CreateUserActivityLogCommand
    {
        UserId = Guid.NewGuid(),
        ActivityType = "TestActivity",
        Description = "Test Description"
    };
    
    // Act
    var result = await handler.Handle(command, CancellationToken.None);
    
    // Assert
    Assert.True(result.IsSuccess);
    mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), 
        Times.Once);
}
```

### Integration Tests

```csharp
[Fact]
public async Task SubscriptionPlanCreated_LogsUserActivity()
{
    // Arrange
    var @event = new SubscriptionPlanCreatedEvent
    {
        SubscriptionPlanId = Guid.NewGuid(),
        Name = "premium",
        CreatedBy = Guid.NewGuid()
    };
    
    // Act
    await PublishEvent(@event);
    await Task.Delay(1000); // Wait for processing
    
    // Assert
    var logs = await GetActivityLogs(@event.CreatedBy.Value);
    Assert.Contains(logs, l => l.ActivityType == "SubscriptionPlanCreated");
}
```

---

## ✅ Implementation Checklist

- [x] Create CQRS Command & Handler (Application layer)
- [x] Create Event Handlers (Infrastructure layer)
- [x] Register Event Handlers in DI
- [x] Configure DbContext for UserActivityLog
- [x] Build verification (UserService.API)
- [ ] Add migration for UserActivityLog (if needed)
- [ ] Integration testing
- [ ] API endpoints for querying activity logs
- [ ] Frontend integration

---

## 📝 Summary

### Files Created (5 new files)

**Application Layer:**
1. `CreateUserActivityLogCommand.cs`
2. `CreateUserActivityLogCommandHandler.cs`

**Infrastructure Layer:**
3. `SubscriptionPlanCreatedEventHandler.cs`
4. `SubscriptionPlanUpdatedEventHandler.cs`
5. `SubscriptionPlanDeletedEventHandler.cs`

**Modified Files:**
- `UserInfrastructureDependencyInjection.cs` (registered event handlers)

### Architecture Summary

```
✅ Clean Architecture: Clear separation of Infrastructure, Application, Domain
✅ CQRS Pattern: Commands for writes, isolation of business logic
✅ Event-Driven: Decoupled services via RabbitMQ
✅ Outbox Pattern: Reliable event delivery
✅ Repository Pattern: Data access abstraction
✅ UnitOfWork Pattern: Transaction management
```

### Key Principles

1. **Event Handlers → Infrastructure** (subscribe & delegate)
2. **Commands → Application** (business logic)
3. **Repository/UnitOfWork → Used in Commands** (data access)
4. **Thin handlers, fat commands** (business logic in right place)

---

**Status:** ✅ **Implementation Complete - Build Successful**  
**Build Time:** 3.13s (UserService.API)  
**Warnings:** 3 (XML documentation only)  
**Errors:** 0  

**Last Updated:** October 2, 2025
