# ✅ Fixed: User Activity Logging for Subscription Plan Events

> **Issue:** Event handlers registered but not subscribed → RabbitMQ couldn't find handlers

---

## 🐛 Problem

### Error Log:
```
warn: SharedLibrary.Commons.EventBus.RabbitMQEventBus[0]
Handler IIntegrationEventHandler`1 not found in DI container
```

### Root Cause:
Event Handlers were **registered in DI container** but **NOT subscribed to RabbitMQ Event Bus**.

```csharp
// ✅ Registered in DI (UserInfrastructureDependencyInjection.cs)
services.AddScoped<IIntegrationEventHandler<SubscriptionPlanCreatedEvent>, SubscriptionPlanCreatedEventHandler>();

// ❌ BUT not subscribed to EventBus
// Missing: eventBus.Subscribe<SubscriptionPlanCreatedEvent, SubscriptionPlanCreatedEventHandler>();
```

---

## ✅ Solution

### 1. Created Extension Method

**File:** `UserService.Infrastructure/Extensions/SubscriptionEventSubscriptionExtension.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Contracts.Subscription;
using UserService.Infrastructure.EventHandlers;

namespace UserService.Infrastructure.Extensions;

public static class SubscriptionEventSubscriptionExtension
{
    /// <summary>
    /// Subscribe to Subscription Plan events from SubscriptionService
    /// </summary>
    public static void SubscribeToSubscriptionEvents(this IServiceProvider serviceProvider)
    {
        var eventBus = serviceProvider.GetRequiredService<IEventBus>();
        
        // Subscribe to SubscriptionPlan events for User Activity Logging
        eventBus.Subscribe<SubscriptionPlanCreatedEvent, SubscriptionPlanCreatedEventHandler>();
        eventBus.Subscribe<SubscriptionPlanUpdatedEvent, SubscriptionPlanUpdatedEventHandler>();
        eventBus.Subscribe<SubscriptionPlanDeletedEvent, SubscriptionPlanDeletedEventHandler>();
        
        // Event bus StartConsuming() is already called in SubscribeToAuthEvents
        // No need to call it again
    }
}
```

**Purpose:**
- ✅ Subscribe event handlers to RabbitMQ Event Bus
- ✅ Follow same pattern as `SubscribeToAuthEvents()`
- ✅ Reusable extension method

---

### 2. Updated Program.cs

**File:** `UserService.API/Program.cs`

```csharp
// Add RabbitMQ Event Bus
app.AddRabbitMQEventBus();

// Subscribe to auth events
app.Services.SubscribeToAuthEvents();

// Subscribe to subscription events ✅ NEW
app.Services.SubscribeToSubscriptionEvents();

logger.LogInformation("UserService API configured successfully");
```

**Before:**
- Only subscribed to Auth events
- Subscription events ignored

**After:**
- ✅ Subscribe to Auth events
- ✅ Subscribe to Subscription events
- ✅ Complete event-driven architecture

---

## 📊 Event Flow (After Fix)

### Complete Flow:

```
1. Admin creates Subscription Plan in CMS
   → POST /api/cms/subscription-plans
                    ↓
2. CreateSubscriptionPlanCommandHandler
   → Captures IP/UserAgent from ICurrentUserService
   → Creates SubscriptionPlanCreatedEvent WITH context
   → Publishes to Outbox
                    ↓
3. OutboxPublisher (Background Service)
   → Reads event FROM Outbox
   → Publishes to RabbitMQ
   → Log: "Successfully published and marked event {id} as processed"
                    ↓
4. RabbitMQ Event Bus (UserService)
   → Receives event from queue
   → Looks up handler in DI container ✅ FOUND NOW!
   → Creates handler instance
                    ↓
5. SubscriptionPlanCreatedEventHandler
   → Receives event WITH IP/UserAgent
   → Creates CreateUserActivityLogCommand
   → Log: "Received SubscriptionPlanCreatedEvent: PlanId={id}"
                    ↓
6. CreateUserActivityLogCommandHandler
   → Saves UserActivityLog to database
   → Log: "User activity log created successfully"
                    ↓
7. UserActivityLog Table
   → Record saved with:
     - UserId: from event.CreatedBy
     - ActivityType: "SubscriptionPlanCreated"
     - Description: "Created subscription plan 'Premium Plan'"
     - IpAddress: from event (captured at source)
     - UserAgent: from event (captured at source)
     - Metadata: JSON with full event details
```

---

## 🔍 Before vs After

### Before Fix:

**DI Registration:** ✅ Registered
```csharp
services.AddScoped<IIntegrationEventHandler<SubscriptionPlanCreatedEvent>, 
    SubscriptionPlanCreatedEventHandler>();
```

**Event Subscription:** ❌ NOT subscribed
```csharp
// Missing subscription!
// eventBus.Subscribe<SubscriptionPlanCreatedEvent, SubscriptionPlanCreatedEventHandler>();
```

**Result:**
```
SubscriptionService publishes event → RabbitMQ receives event
→ UserService RabbitMQ consumer tries to resolve handler
→ Handler NOT found in subscription map
→ Warning: "Handler IIntegrationEventHandler`1 not found in DI container"
→ Event ignored ❌
→ No activity log created ❌
```

---

### After Fix:

**DI Registration:** ✅ Registered (already done)
```csharp
services.AddScoped<IIntegrationEventHandler<SubscriptionPlanCreatedEvent>, 
    SubscriptionPlanCreatedEventHandler>();
```

**Event Subscription:** ✅ Subscribed (NEW!)
```csharp
eventBus.Subscribe<SubscriptionPlanCreatedEvent, SubscriptionPlanCreatedEventHandler>();
```

**Result:**
```
SubscriptionService publishes event → RabbitMQ receives event
→ UserService RabbitMQ consumer resolves handler from subscription map ✅
→ Handler found and resolved from DI container ✅
→ Handler processes event ✅
→ Activity log created successfully ✅
```

---

## 📋 Verification Steps

### 1. Check UserService Logs (on startup):
```
info: SharedLibrary.Commons.EventBus.RabbitMQEventBus[0]
      Subscribing to event: SubscriptionPlanCreatedEvent with handler: SubscriptionPlanCreatedEventHandler

info: SharedLibrary.Commons.EventBus.RabbitMQEventBus[0]
      Starting RabbitMQ event consumption...
```

### 2. Create Subscription Plan (via CMS):
```bash
POST http://localhost:5010/api/cms/subscription-plans
Authorization: Bearer <token>
{
  "name": "test-plan",
  "displayName": "Test Plan",
  "amount": 99000,
  "currency": "VND",
  ...
}
```

### 3. Check SubscriptionService Logs:
```
info: SubscriptionService[0]
      Creating subscription plan: ...

info: SharedLibrary.Commons.Outbox.OutboxUnitOfWork[0]
      Publishing outbox event e065822c-992a-415e-9f61-07d0ba696d0d

info: SharedLibrary.Commons.Outbox.OutboxUnitOfWork[0]
      Successfully published and marked event e065822c-992a-415e-9f61-07d0ba696d0d as processed
```

### 4. Check UserService Logs (should see NOW):
```
info: UserService.Infrastructure.EventHandlers.SubscriptionPlanCreatedEventHandler[0]
      Received SubscriptionPlanCreatedEvent: PlanId={guid}, Name=test-plan, CreatedBy={userId}

info: UserService.Infrastructure.EventHandlers.SubscriptionPlanCreatedEventHandler[0]
      User activity log created successfully for SubscriptionPlanCreated event
```

### 5. Verify Database:
```sql
SELECT * FROM "UserActivityLogs" 
WHERE "ActivityType" = 'SubscriptionPlanCreated'
ORDER BY "OccurredAt" DESC 
LIMIT 1;

-- Expected result:
-- UserId: {admin-guid}
-- ActivityType: SubscriptionPlanCreated
-- Description: Created subscription plan 'Test Plan' (test-plan)
-- IpAddress: 192.168.1.x (from original HTTP request)
-- UserAgent: Mozilla/5.0... (from original HTTP request)
-- Metadata: {"EventId": "...", "PlanName": "test-plan", ...}
```

---

## 🎯 Key Learnings

### Event-Driven Architecture Pattern:

1. **Register Handler in DI:**
   ```csharp
   services.AddScoped<IIntegrationEventHandler<TEvent>, THandler>();
   ```

2. **Subscribe Handler to EventBus:**
   ```csharp
   eventBus.Subscribe<TEvent, THandler>();
   ```

3. **Start Consuming:**
   ```csharp
   eventBus.StartConsuming();
   ```

### Both Steps Required:
- ✅ DI registration → Handler can be instantiated
- ✅ EventBus subscription → Handler is mapped to event type
- ❌ Missing either → Event processing fails

---

## 📝 Files Changed

1. ✅ **Created:** `UserService.Infrastructure/Extensions/SubscriptionEventSubscriptionExtension.cs`
   - Subscribe to 3 Subscription Plan events

2. ✅ **Modified:** `UserService.API/Program.cs`
   - Added `app.Services.SubscribeToSubscriptionEvents();`

3. ✅ **Already Registered:** `UserService.Infrastructure/UserInfrastructureDependencyInjection.cs`
   - Event handlers already in DI (lines 69-71)

---

## ✅ Success Criteria

- [x] Build successful (0 errors)
- [x] Extension method created
- [x] Program.cs updated
- [x] Event handlers subscribed to EventBus
- [ ] Test: Create subscription plan → Activity log created
- [ ] Test: Update subscription plan → Activity log created
- [ ] Test: Delete subscription plan → Activity log created

---

## 🧪 Testing Checklist

### Test 1: Create Plan
```bash
POST /api/cms/subscription-plans
# Expected: UserActivityLog with ActivityType = "SubscriptionPlanCreated"
```

### Test 2: Update Plan
```bash
PUT /api/cms/subscription-plans/{id}
# Expected: UserActivityLog with ActivityType = "SubscriptionPlanUpdated"
```

### Test 3: Delete Plan
```bash
DELETE /api/cms/subscription-plans/{id}
# Expected: UserActivityLog with ActivityType = "SubscriptionPlanDeleted"
```

### Test 4: Verify IP/UserAgent
```sql
SELECT "IpAddress", "UserAgent" FROM "UserActivityLogs"
WHERE "ActivityType" LIKE 'SubscriptionPlan%'
ORDER BY "OccurredAt" DESC;

-- Should show IP addresses and User-Agent strings from HTTP requests
```

---

**Status:** ✅ **Fix Implemented - Ready for Testing**  
**Issue:** Event handlers not subscribed  
**Solution:** Created extension method and subscribed in Program.cs  
**Expected Result:** User activity logs created for all Subscription Plan CRUD operations  
**Last Updated:** October 2, 2025
