# 📚 Cache Write Pattern - Command Handler vs Event Consumer

## 📋 TL;DR

**Rule**: 
- ✅ **Synchronous operations** (Login, Refresh) → **Command Handler** ghi cache trực tiếp
- ❌ **Event Consumers** → KHÔNG ghi cache (chỉ verify/react)

**Reason**: Cache cần được update NGAY LẬP TỨC, không thể đợi event processing.

---

## 🤔 Câu hỏi: Ai nên ghi cache?

### Đề xuất 1: AuthEventConsumer ghi cache ❌

```
LoginCommandHandler
└─ Publish UserLoggedInEvent
    ↓
AuthEventConsumer  
└─ Ghi cache ✍️

RefreshTokenCommandHandler
└─ Publish UserLoggedInEvent
    ↓
AuthEventConsumer  
└─ Ghi cache ✍️
```

**Problems**:
1. ❌ **Delay**: User phải đợi event được process (50-200ms)
2. ❌ **Inconsistency**: Nếu event fail, cache không được update
3. ❌ **Race condition**: LoginCommandHandler CẦN cache ngay để return response
4. ❌ **Confusion**: LoginCommandHandler vẫn phải ghi cache → double write!

---

### Đề xuất 2: Command Handler ghi cache ✅ (CORRECT!)

```
LoginCommandHandler
├─ Ghi cache ✍️ (ngay lập tức)
└─ Publish UserLoggedInEvent (for activity logging)
    ↓
AuthEventConsumer  
└─ Verify cache 👁️ (no write)

RefreshTokenCommandHandler
├─ Ghi cache ✍️ (ngay lập tức)
└─ Publish UserLoggedInEvent (for activity logging)
    ↓
AuthEventConsumer  
└─ Verify cache 👁️ (no write)
```

**Benefits**:
1. ✅ **Immediate**: Cache available ngay sau khi operation complete
2. ✅ **Consistent**: Same pattern for all command handlers
3. ✅ **Reliable**: Cache update is part of the transaction
4. ✅ **Clear responsibility**: Command handler owns state changes

---

## 🏗️ Architecture Pattern

### Synchronous Operations (Commands)

Commands are **requests that require immediate response**:

```csharp
// User calls API → Expects immediate result
POST /api/auth/login
POST /api/auth/refresh
POST /api/subscriptions/register

// Handler MUST:
// 1. Process request
// 2. Update cache (immediate!)
// 3. Return response
// 4. Publish event (async, for side effects)
```

**Pattern**:
```csharp
public async Task<Result> Handle(Command request)
{
    // 1. Business logic
    var result = await ProcessRequest();
    
    // 2. ✅ Update cache IMMEDIATELY (part of main flow)
    await _cache.SetAsync(cacheKey, data);
    
    // 3. Publish event (for side effects: logging, notifications, etc.)
    await _publisher.Publish(new SomethingHappenedEvent { ... });
    
    // 4. Return response (cache is ready!)
    return Result.Success();
}
```

---

### Asynchronous Events (Event Consumers)

Events are **notifications about what happened**:

```csharp
// Event: "UserLoggedIn" - informing other services
// NOT a command to "please update cache"

// Consumer CAN:
// ✅ Log activity
// ✅ Send notifications
// ✅ Update own service's data
// ✅ Verify cache (monitoring)

// Consumer SHOULD NOT:
// ❌ Update shared cache (already done by command handler)
// ❌ Duplicate state changes
```

**Pattern**:
```csharp
public async Task Consume(ConsumeContext<UserLoggedInEvent> context)
{
    var @event = context.Message;
    
    // ✅ Verify cache (monitoring)
    var cached = await _cache.GetAsync(@event.UserId);
    if (cached == null)
    {
        _logger.LogWarning("Cache sync issue!");
    }
    
    // ✅ Service-specific side effects
    await _activityLogger.LogUserActivity(@event);
    await _contentService.PreloadUserPreferences(@event.UserId);
    
    // ❌ DO NOT: Update shared cache
    // await _cache.SetAsync(...); // NO!
}
```

---

## 🔍 Detailed Comparison

### Scenario: User Refresh Token

#### ❌ Wrong Approach: Event Consumer Writes Cache

```
Time: 0ms
User → POST /api/auth/refresh

Time: 10ms
RefreshTokenCommandHandler:
├─ Generate new token ✅
├─ Update DB ✅
└─ Publish UserLoggedInEvent
    ↓ (no cache write here)

Time: 50ms (event processing delay)
AuthEventConsumer:
└─ Receive event
    └─ Write cache ✍️

Time: 60ms
User receives response
❌ But cache still has OLD token (0-50ms gap!)

Time: 70ms
Another request with new token
❌ Cache validation fails (cache has old token!)
```

**Problem**: Race condition! New token issued but cache not updated yet.

---

#### ✅ Correct Approach: Command Handler Writes Cache

```
Time: 0ms
User → POST /api/auth/refresh

Time: 10ms
RefreshTokenCommandHandler:
├─ Generate new token ✅
├─ Update DB ✅
├─ Write cache ✍️ (immediate!)
└─ Publish UserLoggedInEvent
    ↓ (for activity logging only)

Time: 15ms
User receives response
✅ Cache already has NEW token!

Time: 20ms
Another request with new token
✅ Cache validation succeeds!

Time: 50ms (async)
AuthEventConsumer:
└─ Verify cache ✅
    └─ Log activity ✅
```

**Benefit**: No race condition! Cache immediately consistent.

---

## 📊 Pattern Matrix

| Operation Type | Example | Cache Write? | Why? |
|----------------|---------|--------------|------|
| **Command** | Login | ✅ Handler | Needs immediate cache for validation |
| **Command** | Refresh Token | ✅ Handler | Needs immediate cache for new token |
| **Command** | Register Subscription | ✅ Handler | Needs immediate cache for business logic |
| **Event** | UserLoggedIn | ❌ Consumer | Cache already written by command |
| **Event** | SubscriptionActivated | ✅ Consumer | Updates ONLY Subscription field (partial update) |
| **Event** | UserRolesChanged | ✅ Consumer | Updates ONLY Roles field (partial update) |

**Key Insight**:
- **Full cache writes** → Command handlers (Login, Refresh)
- **Partial cache updates** → Event consumers (Subscription status, Roles)
- **No duplicate writes** → Each field has ONE owner

---

## 🎯 Responsibilities

### AuthService - Login/Refresh Handlers

**Owns**: Full `UserStateInfo` cache

```csharp
public class LoginCommandHandler
{
    public async Task<Result> Handle()
    {
        // ✅ OWNS: Full user state write
        var userState = new UserStateInfo
        {
            UserId = user.Id,
            UserProfileId = userProfileId,
            Email = user.Email,
            Roles = roles,
            Status = user.Status,
            RefreshToken = refreshToken,      // ✅ AuthService responsibility
            RefreshTokenExpiryTime = expiry,
            LastLoginAt = DateTime.UtcNow,
            Subscription = existingCache?.Subscription // ✅ Preserve from other service
        };
        
        await _cache.SetUserStateAsync(userState);
        await _publisher.Publish(new UserLoggedInEvent { ... }); // Notification only
    }
}
```

---

### SubscriptionService - Activation Consumer

**Owns**: Only `Subscription` field

```csharp
public class ActivateSubscriptionConsumer
{
    public async Task Consume()
    {
        // ✅ Activate subscription in DB
        subscription.Status = Active;
        await _unitOfWork.SaveChangesAsync();
        
        // ✅ Update ONLY Subscription field in cache (partial update)
        await _publisher.Publish(new UserSubscriptionStatusChangedEvent
        {
            UserId = userId,
            Subscription = new UserSubscriptionInfo { ... }
        });
        // → Another consumer updates cache.Subscription field only
    }
}
```

---

### ContentService - Auth Event Consumer

**Owns**: Nothing (read-only)

```csharp
public class AuthEventConsumer
{
    public async Task Consume(ConsumeContext<UserLoggedInEvent> context)
    {
        // ✅ Verify cache (monitoring)
        var cached = await _cache.GetUserStateAsync(context.Message.UserId);
        
        // ✅ Service-specific logic
        await _contentService.PreloadUserContent(context.Message.UserId);
        
        // ❌ DO NOT: Write cache
        // Cache already written by AuthService's LoginCommandHandler
    }
}
```

---

## 🧪 Testing

### Test 1: Refresh Token Immediate Cache Update

```csharp
[Fact]
public async Task RefreshToken_ShouldUpdateCacheImmediately()
{
    // Arrange
    var user = await LoginUser();
    var oldRefreshToken = user.RefreshToken;
    
    // Act - Refresh token
    var response = await RefreshToken(oldRefreshToken);
    
    // Assert - Cache updated immediately (no event delay)
    var cached = await _cache.GetUserStateAsync(user.Id);
    
    Assert.NotNull(cached);
    Assert.NotEqual(oldRefreshToken, cached.RefreshToken); // ✅ NEW token in cache
    Assert.Equal(user.Id, cached.UserId);
    
    // Verify new token works immediately
    var validationResult = await ValidateToken(response.AccessToken);
    Assert.True(validationResult); // ✅ No race condition
}
```

---

### Test 2: Event Consumer Does Not Overwrite Cache

```csharp
[Fact]
public async Task UserLoggedInEvent_ConsumerDoesNotOverwriteCache()
{
    // Arrange
    await LoginUser(); // Cache written by LoginCommandHandler
    
    // Activate subscription (updates cache)
    await ActivateSubscription(userId);
    
    var cacheBeforeEvent = await _cache.GetUserStateAsync(userId);
    Assert.NotNull(cacheBeforeEvent.Subscription); // Has subscription
    
    // Act - Publish UserLoggedInEvent (simulating refresh token)
    await _publisher.Publish(new UserLoggedInEvent { UserId = userId });
    await Task.Delay(100); // Wait for consumer
    
    // Assert - Subscription NOT overwritten
    var cacheAfterEvent = await _cache.GetUserStateAsync(userId);
    
    Assert.NotNull(cacheAfterEvent.Subscription); // ✅ Still has subscription
    Assert.Equal(cacheBeforeEvent.Subscription.SubscriptionId, 
                 cacheAfterEvent.Subscription.SubscriptionId); // ✅ Preserved
}
```

---

## 🚦 Decision Tree

```
┌────────────────────────────────────┐
│  Need to update UserStateInfo?    │
└────────────────────────────────────┘
                 ↓
         ┌───────────────┐
         │  Is this a    │
         │  Command?     │
         │  (API request)│
         └───────────────┘
         ↓ Yes       ↓ No (Event)
    ┌─────────┐   ┌─────────┐
    │ Handler │   │ Consumer│
    │ writes  │   │ verifies│
    │ cache   │   │ only    │
    └─────────┘   └─────────┘
         ↓             ↓
    ┌─────────┐   ┌─────────┐
    │ Publish │   │ Service │
    │ event   │   │ specific│
    │ after   │   │ logic   │
    └─────────┘   └─────────┘
```

**Examples**:
- **Command → Write**: Login, Refresh, UpdateProfile
- **Event → Verify**: UserLoggedInEvent, UserLoggedOutEvent
- **Event → Partial Update**: SubscriptionActivated (only Subscription field)

---

## ✅ Summary

### ✅ DO

1. **Command handlers write cache immediately**
   ```csharp
   await ProcessBusinessLogic();
   await _cache.SetAsync(...); // ✅ Immediate
   await _publisher.Publish(...); // Then notify
   ```

2. **Event consumers verify cache**
   ```csharp
   var cached = await _cache.GetAsync(...);
   if (cached == null) _logger.LogWarning("Cache miss!");
   ```

3. **Partial updates via dedicated events**
   ```csharp
   // SubscriptionService
   await _publisher.Publish(new UserSubscriptionStatusChangedEvent { ... });
   // → Dedicated consumer updates ONLY Subscription field
   ```

---

### ❌ DON'T

1. **Event consumers don't duplicate writes**
   ```csharp
   // ❌ NO!
   public async Task Consume(ConsumeContext<UserLoggedInEvent> context)
   {
       await _cache.SetAsync(...); // Already done by command handler!
   }
   ```

2. **Commands don't rely on events for cache**
   ```csharp
   // ❌ NO!
   public async Task<Result> Handle()
   {
       await ProcessBusinessLogic();
       await _publisher.Publish(...);
       // ❌ NO cache write - expecting event consumer to do it
       return Result.Success(); // Cache not ready!
   }
   ```

3. **Don't mix responsibilities**
   ```csharp
   // ❌ NO! AuthService shouldn't update Subscription field
   userState.Subscription = new UserSubscriptionInfo { ... }; // Not your field!
   ```

---

## 🎯 Final Pattern

```csharp
// ✅ LoginCommandHandler (AuthService)
public async Task<Result> Handle(LoginCommand request)
{
    // 1. Authenticate
    var user = await Authenticate(request);
    
    // 2. Generate tokens
    var (accessToken, refreshToken, roles) = GenerateTokens(user);
    
    // 3. ✅ Write cache IMMEDIATELY (full state)
    var userState = new UserStateInfo
    {
        UserId = user.Id,
        // ... all fields ...
        Subscription = existingCache?.Subscription // Preserve
    };
    await _cache.SetUserStateAsync(userState);
    
    // 4. Publish event (for side effects)
    await _publisher.Publish(new UserLoggedInEvent { ... });
    
    // 5. Return (cache is ready!)
    return Result.Success(new AuthResponse { accessToken, ... });
}

// ✅ RefreshTokenCommandHandler (AuthService)
public async Task<Result> Handle(RefreshTokenCommand request)
{
    // Same pattern as Login!
    var user = await GetUser(request);
    var (accessToken, refreshToken, roles) = GenerateTokens(user);
    
    // ✅ Write cache IMMEDIATELY
    await _cache.SetUserStateAsync(new UserStateInfo { ... });
    
    await _publisher.Publish(new UserLoggedInEvent { ... });
    
    return Result.Success(new AuthResponse { accessToken, ... });
}

// ✅ AuthEventConsumer (ContentService)
public async Task Consume(ConsumeContext<UserLoggedInEvent> context)
{
    // ✅ Verify cache (monitoring)
    var cached = await _cache.GetUserStateAsync(context.Message.UserId);
    if (cached == null)
    {
        _logger.LogWarning("Cache sync issue for UserId={UserId}", context.Message.UserId);
    }
    
    // ✅ Service-specific logic
    await _contentService.PreloadUserContent(context.Message.UserId);
    
    // ❌ NO cache write!
}
```

---

**Status**: ✅ DOCUMENTED
**Pattern**: Command Handler Writes, Event Consumer Verifies
**Category**: Architecture Pattern & Best Practices

