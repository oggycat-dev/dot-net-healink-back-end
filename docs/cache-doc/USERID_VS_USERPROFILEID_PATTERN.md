# UserId vs UserProfileId Pattern

## 🎯 Overview

Phân biệt rõ ràng giữa **UserId (Authentication)** và **UserProfileId (Business Logic)** để đảm bảo security và data integrity.

**Critical Rule**: UserId from JWT token is for AUTHENTICATION only. UserProfileId from Redis cache is for BUSINESS LOGIC.

---

## 🔑 Key Concepts

### 1. **UserId (Authentication)**

**Source**: JWT Token  
**Purpose**: Authentication & Authorization only  
**Managed by**: AuthService  
**Stored in**: JWT Claims

**Properties**:
- ✅ Used for verifying user identity
- ✅ Used for Gateway authentication
- ❌ **NEVER** use directly in business logic
- ❌ **NEVER** store in database as foreign key

**Example**:
```csharp
var authUserId = _currentUserService.UserId; // From JWT token
// ✅ Use for: Authentication check
// ❌ DON'T use for: Creating subscriptions, orders, etc.
```

---

### 2. **UserProfileId (Business Logic)**

**Source**: Redis Cache (UserStateCache)  
**Purpose**: Business operations & database relations  
**Managed by**: UserService  
**Stored in**: Redis + Database

**Properties**:
- ✅ Used for all business logic operations
- ✅ Used as foreign key in database
- ✅ Retrieved from cache using UserId
- ✅ Represents actual user profile in UserService

**Example**:
```csharp
var userState = await _userStateCache.GetUserStateAsync(authUserId);
var userProfileId = userState.UserId; // This is UserProfileId
// ✅ Use for: Creating subscriptions, orders, transactions
// ✅ Use for: Database foreign keys
```

---

## 🏗️ Architecture Flow

```
1. User Login
   ↓
2. AuthService generates JWT Token
   - Contains: UserId (for auth)
   ↓
3. UserService caches user state in Redis
   - Contains: UserId (UserProfileId), Email, Roles, Status
   ↓
4. User Request → Gateway
   - Gateway validates JWT token (uses UserId for auth)
   - Gateway queries Redis cache
   - Gateway sets user claims from cache
   ↓
5. Service receives request
   - Get UserId from JWT (authentication)
   - Query UserProfileId from Redis cache
   - Use UserProfileId for business logic
```

---

## 📊 Implementation Pattern

### Step 1: Inject Dependencies

```csharp
public class RegisterSubscriptionCommandHandler 
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserStateCache _userStateCache;
    
    public RegisterSubscriptionCommandHandler(
        ICurrentUserService currentUserService,
        IUserStateCache userStateCache)
    {
        _currentUserService = currentUserService;
        _userStateCache = userStateCache;
    }
}
```

---

### Step 2: Get UserId (Authentication)

```csharp
// ✅ Step 1: Get UserId from JWT token (for AUTHENTICATION only)
var userIdStr = _currentUserService.UserId;
if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var authUserId))
{
    return Result.Failure("User not authenticated", ErrorCodeEnum.Unauthorized);
}
```

---

### Step 3: Get UserProfileId from Cache

```csharp
// ✅ Step 2: Get UserProfileId from Redis cache (for BUSINESS LOGIC)
var userState = await _userStateCache.GetUserStateAsync(authUserId);
if (userState == null)
{
    _logger.LogWarning("User state not found in cache for UserId={UserId}", authUserId);
    return Result.Failure("User session not found. Please login again.", ErrorCodeEnum.Unauthorized);
}

if (!userState.IsActive)
{
    _logger.LogWarning("User {UserId} is inactive. Status={Status}", authUserId, userState.Status);
    return Result.Failure("User account is inactive", ErrorCodeEnum.Forbidden);
}

// ✅ UserProfileId from cache - THIS is used for business logic
var userProfileId = userState.UserId; // This is the actual UserProfileId from UserService
```

---

### Step 4: Use UserProfileId for Business Logic

```csharp
// ✅ Create entity with UserProfileId
var subscription = new Subscription
{
    UserProfileId = userProfileId, // ✅ Business logic uses UserProfileId
    SubscriptionPlanId = planId,
    SubscriptionStatus = SubscriptionStatus.Pending
};
subscription.InitializeEntity(userProfileId); // ✅ CreatedBy = UserProfileId

// ✅ Query existing data
var existingSubscription = await _repository.GetFirstOrDefaultAsync(
    s => s.UserProfileId == userProfileId && // ✅ Use UserProfileId
         s.SubscriptionStatus == SubscriptionStatus.Active);

// ✅ Send events
var sagaEvent = new SubscriptionRegistrationStarted
{
    UserProfileId = userProfileId, // ✅ Use UserProfileId
    CreatedBy = userProfileId // ✅ Use UserProfileId
};
```

---

## ✅ Example: RegisterSubscriptionCommandHandler

### Complete Implementation

```csharp
public async Task<Result<object>> Handle(RegisterSubscriptionCommand command, CancellationToken cancellationToken)
{
    try
    {
        // ✅ Step 1: Get UserId from JWT token (for AUTHENTICATION only)
        var userIdStr = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var authUserId))
        {
            return Result<object>.Failure("User not authenticated", ErrorCodeEnum.Unauthorized);
        }

        // ✅ Step 2: Get UserProfileId from Redis cache (for BUSINESS LOGIC)
        var userState = await _userStateCache.GetUserStateAsync(authUserId);
        if (userState == null)
        {
            _logger.LogWarning("User state not found in cache for UserId={UserId}", authUserId);
            return Result<object>.Failure("User session not found. Please login again.", ErrorCodeEnum.Unauthorized);
        }

        if (!userState.IsActive)
        {
            _logger.LogWarning("User {UserId} is inactive. Status={Status}", authUserId, userState.Status);
            return Result<object>.Failure("User account is inactive", ErrorCodeEnum.Forbidden);
        }

        // ✅ UserProfileId from cache - THIS is used for business logic
        var userProfileId = userState.UserId;

        _logger.LogInformation(
            "Processing subscription registration: AuthUserId={AuthUserId}, UserProfileId={UserProfileId}",
            authUserId, userProfileId);

        // ✅ Step 3: Use UserProfileId for all business operations
        
        // Validate plan
        var plan = await _repository.GetFirstOrDefaultAsync(
            p => p.Id == command.Request.SubscriptionPlanId);
        
        // Check existing subscription (use UserProfileId)
        var existingSubscription = await _repository.GetFirstOrDefaultAsync(
            s => s.UserProfileId == userProfileId && 
                 s.SubscriptionStatus == SubscriptionStatus.Active);
        
        if (existingSubscription != null)
        {
            return Result<object>.Failure("Already has active subscription", ErrorCodeEnum.DuplicateEntry);
        }

        // Create subscription (use UserProfileId)
        var subscription = new Subscription
        {
            UserProfileId = userProfileId, // ✅ Business logic uses UserProfileId
            SubscriptionPlanId = plan.Id,
            SubscriptionStatus = SubscriptionStatus.Pending
        };
        subscription.InitializeEntity(userProfileId); // ✅ CreatedBy = UserProfileId

        await _repository.AddAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        // Publish event (use UserProfileId)
        var sagaEvent = new SubscriptionRegistrationStarted
        {
            SubscriptionId = subscription.Id,
            UserProfileId = userProfileId, // ✅ Use UserProfileId
            CreatedBy = userProfileId // ✅ Use UserProfileId
        };
        await _publishEndpoint.Publish(sagaEvent, cancellationToken);

        return Result<object>.Success(new { SubscriptionId = subscription.Id });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error registering subscription");
        return Result<object>.Failure("Error registering subscription", ErrorCodeEnum.InternalError);
    }
}
```

---

## 🚫 Common Mistakes

### ❌ WRONG: Using UserId directly for business logic

```csharp
// ❌ BAD - Using JWT UserId for business logic
var userId = _currentUserService.UserId;

var subscription = new Subscription
{
    UserProfileId = userId, // ❌ WRONG - This is auth UserId, not UserProfileId
};

var existingSubscription = await _repository.GetFirstOrDefaultAsync(
    s => s.UserProfileId == userId); // ❌ WRONG - Won't find correct user's data
```

**Problem**: 
- UserId from JWT might not match UserProfileId in database
- Can cause data integrity issues
- Can expose other users' data

---

### ✅ CORRECT: Query UserProfileId from cache

```csharp
// ✅ GOOD - Get UserProfileId from cache
var authUserId = _currentUserService.UserId; // For authentication
var userState = await _userStateCache.GetUserStateAsync(authUserId);
var userProfileId = userState.UserId; // For business logic

var subscription = new Subscription
{
    UserProfileId = userProfileId, // ✅ CORRECT
};

var existingSubscription = await _repository.GetFirstOrDefaultAsync(
    s => s.UserProfileId == userProfileId); // ✅ CORRECT
```

---

## 🔐 Security Benefits

### 1. **Separation of Concerns**
- Authentication logic separate from business logic
- Clear boundaries between services

### 2. **Data Integrity**
- Always use correct UserProfileId from UserService
- Prevent data leakage between users

### 3. **Cache Validation**
- Verify user is active before processing
- Check user state from Redis

### 4. **Audit Trail**
- Clear tracking: AuthUserId → UserProfileId
- Easy to debug issues

---

## 📚 Related Services

### AuthService
- **Responsibility**: User authentication & JWT token generation
- **Manages**: UserId (for authentication)
- **Database**: AppUser table

### UserService
- **Responsibility**: User profile management
- **Manages**: UserProfileId, Email, Roles, Status
- **Database**: UserProfile table
- **Cache**: Redis (UserStateCache)

### Other Services
- **Responsibility**: Business logic (subscriptions, payments, etc.)
- **Uses**: UserProfileId from cache
- **Never**: Use UserId directly for business logic

---

## 🧪 Testing

### Test User Authentication

```csharp
[Fact]
public async Task Handle_ValidUser_ReturnsUserProfileId()
{
    // Arrange
    var authUserId = Guid.NewGuid(); // From JWT
    var userProfileId = Guid.NewGuid(); // From UserService
    
    _currentUserService.UserId.Returns(authUserId.ToString());
    _userStateCache.GetUserStateAsync(authUserId).Returns(new UserStateInfo
    {
        UserId = userProfileId, // This is UserProfileId
        IsActive = true
    });
    
    // Act
    var result = await _handler.Handle(command, CancellationToken.None);
    
    // Assert
    // Verify UserProfileId was used, not authUserId
    _repository.Received(1).AddAsync(Arg.Is<Subscription>(
        s => s.UserProfileId == userProfileId)); // ✅ Should use UserProfileId
}
```

---

## ⚠️ Important Notes

### 1. **Always Query Cache First**
```csharp
// ✅ DO THIS
var userState = await _userStateCache.GetUserStateAsync(authUserId);
var userProfileId = userState.UserId;

// ❌ DON'T DO THIS
var userProfileId = _currentUserService.UserId; // This is auth UserId!
```

### 2. **Handle Cache Miss**
```csharp
if (userState == null)
{
    // User session expired or invalid
    return Result.Failure("User session not found. Please login again.", ErrorCodeEnum.Unauthorized);
}
```

### 3. **Verify User Status**
```csharp
if (!userState.IsActive)
{
    return Result.Failure("User account is inactive", ErrorCodeEnum.Forbidden);
}
```

### 4. **Log Both IDs for Debugging**
```csharp
_logger.LogInformation(
    "Processing request: AuthUserId={AuthUserId}, UserProfileId={UserProfileId}",
    authUserId, userProfileId);
```

---

## 📊 Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│ 1. User Login                                               │
│    POST /api/user/auth/login                                │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ 2. AuthService                                              │
│    - Validates credentials                                  │
│    - Generates JWT with UserId (auth ID)                    │
│    - Returns JWT token                                      │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ 3. UserService                                              │
│    - Caches user state in Redis                             │
│    - Key: AuthUserId                                        │
│    - Value: { UserId (UserProfileId), Email, Roles, ... }   │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ 4. User Request (with JWT token)                            │
│    POST /api/user/subscriptions/register                    │
│    Authorization: Bearer <JWT with UserId>                  │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ 5. Gateway                                                  │
│    - Validates JWT (extracts UserId)                        │
│    - Queries Redis cache (UserId → UserProfileId)           │
│    - Sets user claims from cache                            │
│    - Forwards to SubscriptionService                        │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ 6. SubscriptionService Handler                              │
│    Step 1: Get UserId from JWT (authentication)             │
│            var authUserId = _currentUserService.UserId;     │
│                                                             │
│    Step 2: Query UserProfileId from Redis                   │
│            var userState = await _cache.Get(authUserId);    │
│            var userProfileId = userState.UserId;            │
│                                                             │
│    Step 3: Use UserProfileId for business logic             │
│            subscription.UserProfileId = userProfileId;      │
└─────────────────────────────────────────────────────────────┘
```

---

## ✅ Checklist for Handlers

When implementing any command/query handler:

- [ ] ✅ Inject `ICurrentUserService` for authentication
- [ ] ✅ Inject `IUserStateCache` for UserProfileId
- [ ] ✅ Get `UserId` from JWT token first
- [ ] ✅ Query `UserStateCache` with UserId
- [ ] ✅ Extract `UserProfileId` from cache
- [ ] ✅ Validate user is active (`userState.IsActive`)
- [ ] ✅ Use `UserProfileId` for all business logic
- [ ] ✅ Use `UserProfileId` for database operations
- [ ] ✅ Use `UserProfileId` in events
- [ ] ✅ Log both IDs for debugging
- [ ] ❌ **NEVER** use JWT UserId directly for business logic

---

## 🎯 Summary

| Aspect | UserId (JWT) | UserProfileId (Cache) |
|--------|--------------|----------------------|
| **Source** | JWT Token | Redis Cache |
| **Purpose** | Authentication | Business Logic |
| **Service** | AuthService | UserService |
| **Usage** | Verify identity | Database operations |
| **In Code** | `_currentUserService.UserId` | `userState.UserId` |
| **For Business** | ❌ No | ✅ Yes |
| **Foreign Key** | ❌ No | ✅ Yes |

**Golden Rule**: UserId for Auth, UserProfileId for Business! 🎯

---

**Status**: ✅ Pattern Documented  
**Applies to**: All services (Subscription, Payment, Order, etc.)  
**Last Updated**: 2025-10-12

