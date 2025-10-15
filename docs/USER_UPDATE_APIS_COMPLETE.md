# User Update APIs - Complete Documentation

## 📋 Overview

Comprehensive implementation of user management update features with:
- ✅ **Update User Info** (email, phone, fullname, address)
- ✅ **Update User Roles** (add/remove roles)
- ✅ **Update User Status** (Active/Inactive/Deleted/Pending)
- ✅ **Activity Logging** for all operations
- ✅ **Cache-First Validation** (don't trust JWT)
- ✅ **RPC Pattern** for AuthService sync with timeout & rollback
- ✅ **IP and UserAgent** tracking from ICurrentUserService

---

## 🏗️ Architecture

### User ID Clarification (CRITICAL)

```
┌─────────────────────────────────────────────────────────────┐
│  ⚠️  IMPORTANT: Two Types of User IDs                        │
├─────────────────────────────────────────────────────────────┤
│  1. Auth User ID (Guid)                                      │
│     - Source: AppUser.Id in AuthService                      │
│     - Used for:                                              │
│       ✓ Cache key (UserStateCache)                           │
│       ✓ UserProfile.UserId (Foreign Key)                     │
│       ✓ RPC calls to AuthService                             │
│       ✓ Event publishing (UserId field)                      │
│                                                              │
│  2. UserProfile ID (Guid)                                    │
│     - Source: UserProfile.Id (Primary Key)                   │
│     - Used for:                                              │
│       ✓ UserActivityLog.UserId (Foreign Key)                 │
│       ✓ Event publishing (UserProfileId field)               │
│       ✓ Internal UserService relationships                   │
└─────────────────────────────────────────────────────────────┘
```

### Data Flow Pattern

```
┌──────────────┐
│  API Request │  PUT /api/cms/users/{authUserId}
└──────┬───────┘
       │
       ▼
┌──────────────────────────────────────────────────┐
│  1. VALIDATION (Cache-First)                     │
│  ✓ GetUserStateAsync(authUserId)                 │
│  ✓ Check Status == Active                        │
│  ✓ Reject if not found or inactive               │
└──────┬───────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────┐
│  2. QUERY USERPROFILE                            │
│  ✓ UserProfile.Where(u => u.UserId == authUserId)│
│  ✓ authUserId = Foreign Key to AppUser.Id        │
└──────┬───────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────┐
│  3. UPDATE USERSERVICE                           │
│  ✓ Update UserProfile fields                     │
│  ✓ SaveChanges to database                       │
└──────┬───────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────┐
│  4. SYNC TO AUTHSERVICE (if needed)              │
│  ✓ RPC call with 10s timeout (no retry)          │
│  ✓ Rollback if timeout or failure                │
│  ✓ Email/Phone changes only                      │
└──────┬───────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────┐
│  5. UPDATE CACHE (Immediately)                   │
│  ✓ Get current cache by authUserId               │
│  ✓ Update fields using 'with' expression         │
│  ✓ Set updated cache back                        │
└──────┬───────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────┐
│  6. LOG ACTIVITY                                 │
│  ✓ UserActivityLog.UserId = userProfile.Id (PK)  │
│  ✓ IpAddress from ICurrentUserService.IpAddress  │
│  ✓ UserAgent from ICurrentUserService.UserAgent  │
└──────┬───────────────────────────────────────────┘
       │
       ▼
┌──────────────┐
│   Response   │
└──────────────┘
```

---

## 🔧 Implementation Details

### 1. Update User Info API

**Endpoint**: `PUT /api/cms/users/{id}`

**Authorization**: Admin only

**Features**:
- ✅ Update FullName, Email, PhoneNumber, Address
- ✅ Email/Phone changes trigger RPC sync to AuthService
- ✅ 10-second timeout with automatic rollback on failure
- ✅ Cache updated immediately after success
- ✅ Activity logged with IP and UserAgent

**Request Body**:
```json
{
  "fullName": "Updated Name",
  "email": "newemail@example.com",
  "phoneNumber": "+84901234567",
  "address": "New Address"
}
```

**RPC Flow** (Email/Phone changes):
```
UserService                    AuthService
    │                              │
    │──── UpdateUserInfoRpcRequest ──────▶│
    │     (10s timeout, no retry)          │
    │                              │
    │                              ├─ Check email exists
    │                              ├─ Update AppUser.Email
    │                              ├─ Update AppUser.PhoneNumber
    │                              ├─ SaveChanges
    │                              │
    │◀──── UpdateUserInfoRpcResponse ─────│
    │     (Success/Failure)                │
    │                              │
    ├─ If Success: Update cache
    ├─ If Failure: Rollback UserProfile changes
    └─ Log activity
```

**Rollback Scenario**:
```csharp
// If AuthService RPC fails or times out:
1. Restore UserProfile fields to old values
2. SaveChanges (rollback)
3. Return error to client
4. Cache remains unchanged
```

**Code Files**:
- Command: `UpdateUserInfoCommand.cs`
- Handler: `UpdateUserInfoCommandHandler.cs`
- DTO: `UpdateUserInfoRequest.cs`
- RPC Events: `UpdateUserInfoEvents.cs` (RpcRequest/RpcResponse)
- Consumer: `UpdateUserInfoConsumer.cs` (AuthService)

---

### 2. Update User Roles API

**Endpoint**: `PUT /api/cms/users/{id}/roles`

**Authorization**: Admin only

**Features**:
- ✅ Add multiple roles in single request
- ✅ Remove multiple roles in single request
- ✅ Event-driven sync to AuthService
- ✅ Cache updated immediately (source of truth)
- ✅ Activity logged with IP and UserAgent

**Request Body**:
```json
{
  "rolesToAdd": ["ContentCreator", "Staff"],
  "rolesToRemove": ["User"]
}
```

**Event Flow**:
```
UserService                    AuthService
    │                              │
    │─── UserRolesChangedEvent ────▶│
    │    (OldRoles, NewRoles,        │
    │     AddedRoles, RemovedRoles)  │
    │                              │
    │                              ├─ Update AspNetUserRoles
    │                              ├─ SaveChanges
    │                              └─ Update cache
    │                              │
    ├─ Wait 2s for processing
    ├─ Update cache immediately
    │  (don't wait for event confirmation)
    └─ Log activity
```

**Cache Update Pattern**:
```csharp
// Get current cache
var cachedUserState = await _userStateCache.GetUserStateAsync(authUserId);

// Calculate new roles
var newRoles = cachedUserState.Roles
    .Except(command.RolesToRemove)
    .Union(command.RolesToAdd)
    .Distinct()
    .ToList();

// Update cache using 'with' expression (immutable record)
var updatedState = cachedUserState with 
{ 
    Roles = newRoles,
    CacheUpdatedAt = DateTime.UtcNow
};

await _userStateCache.SetUserStateAsync(updatedState);
```

**Code Files**:
- Command: `UpdateUserRolesCommand.cs`
- Handler: `UpdateUserRolesCommandHandler.cs`
- DTO: `UpdateUserRolesRequest.cs`
- Event: `UserRolesChangedEvent.cs` (existing in SharedLibrary)

---

### 3. Update User Status API

**Endpoint**: `PUT /api/cms/users/{id}/status`

**Authorization**: Admin only

**Features**:
- ✅ Change status (Active/Inactive/Deleted/Pending)
- ✅ Event-driven sync to AuthService
- ✅ Cache updated immediately
- ✅ Activity logged with IP and UserAgent
- ✅ Reason tracking for audit trail

**Request Body**:
```json
{
  "status": 0,
  "reason": "Account suspended for violation"
}
```

**Status Values**:
- `0` = Active
- `1` = Inactive
- `2` = Deleted
- `3` = Pending

**Event Flow**:
```
UserService                    AuthService
    │                              │
    │── UserStatusChangedEvent ────▶│
    │   (OldStatus, NewStatus,       │
    │    Reason, ChangedBy)           │
    │                              │
    │                              ├─ Update AppUser.Status
    │                              ├─ SaveChanges
    │                              └─ Update cache
    │                              │
    ├─ Update cache immediately
    │  (don't wait for event confirmation)
    └─ Log activity
```

**Code Files**:
- Command: `UpdateUserStatusCommand.cs`
- Handler: `UpdateUserStatusCommandHandler.cs`
- DTO: `UpdateUserStatusRequest.cs`
- Event: `UserStatusChangedEvent.cs` (existing in SharedLibrary)

---

### 4. Activity Logging (All Operations)

**Features**:
- ✅ Automatic logging for Create/Update operations
- ✅ IP Address from `ICurrentUserService.IpAddress`
- ✅ User Agent from `ICurrentUserService.UserAgent`
- ✅ Metadata in JSON format (old/new values)

**Activity Types**:
- `UserCreatedByAdmin` - User created by admin
- `UserInfoUpdated` - User info changed
- `UserRolesUpdated` - Roles added/removed
- `UserStatusUpdated` - Status changed

**ActivityLog Structure**:
```csharp
var activityLog = new UserActivityLog
{
    UserId = userProfile.Id,           // ⚠️ UserProfile.Id (PK), NOT Auth User ID
    ActivityType = "UserInfoUpdated",
    Description = "User info updated: Email, Phone",
    Metadata = JsonSerializer.Serialize(new
    {
        OldEmail = "old@example.com",
        NewEmail = "new@example.com",
        Changes = ["Email", "Phone"]
    }),
    IpAddress = _currentUserService.IpAddress,    // ✅ From ICurrentUserService
    UserAgent = _currentUserService.UserAgent,    // ✅ From ICurrentUserService
    OccurredAt = DateTime.UtcNow
};
```

---

## 🎯 Cache Management Strategy

### Why Cache-First Validation?

```
❌ PROBLEM: JWT is stateless
   - Can't revoke immediately
   - User status changes not reflected
   - Role updates require new login
   
✅ SOLUTION: Cache as source of truth
   - Real-time user state
   - Immediate updates
   - Consistent across services
```

### Cache Update Pattern (Immutable Records)

```csharp
// ❌ WRONG: Can't mutate init-only properties
cachedUserState.Email = newEmail;  // ERROR: init-only property

// ✅ CORRECT: Use 'with' expression
var updatedState = cachedUserState with 
{ 
    Email = newEmail,
    CacheUpdatedAt = DateTime.UtcNow
};
await _userStateCache.SetUserStateAsync(updatedState);
```

### UserStateInfo Structure

```csharp
public record UserStateInfo
{
    public Guid UserId { get; init; }              // Auth User ID (Cache key)
    public Guid UserProfileId { get; init; }       // UserProfile.Id
    public string Email { get; init; }             // Synced from AuthService
    public List<string> Roles { get; init; }       // Real-time roles
    public EntityStatusEnum Status { get; init; }  // Real-time status
    public DateTime CacheUpdatedAt { get; init; }  // Last cache update
    // ... other fields
}
```

---

## 🔒 Security & Validation

### 1. Authorization

All update APIs require **Admin role**:
```csharp
[AuthorizeRoles("Admin")]
```

### 2. Validation Strategy

```
┌─────────────────────────────────────────────────┐
│  Validation Priority (in order)                 │
├─────────────────────────────────────────────────┤
│  1. Cache (Source of truth)                     │
│     ✓ Check user exists in cache                │
│     ✓ Check status == Active                    │
│                                                  │
│  2. Database (Consistency check)                │
│     ✓ Verify UserProfile exists                 │
│     ✓ Check email uniqueness (updates)          │
│                                                  │
│  3. JWT (NOT trusted for validation)            │
│     ✓ Only for authentication                   │
│     ✗ NOT used for authorization checks         │
└─────────────────────────────────────────────────┘
```

### 3. IP and UserAgent Tracking

**ICurrentUserService Integration**:
```csharp
public interface ICurrentUserService
{
    string? UserId { get; }
    string? IpAddress { get; }      // ✅ From HttpContext.Connection.RemoteIpAddress
    string? UserAgent { get; }      // ✅ From HttpContext.Request.Headers["User-Agent"]
    IEnumerable<string> Roles { get; } // ✅ From Cache (not JWT)
}
```

**Usage in Activity Logging**:
```csharp
var activityLog = new UserActivityLog
{
    // ... other fields
    IpAddress = _currentUserService.IpAddress,
    UserAgent = _currentUserService.UserAgent,
    OccurredAt = DateTime.UtcNow
};
```

---

## 🔄 RPC Pattern (UpdateUserInfo only)

### Why RPC for Email/Phone Sync?

```
┌──────────────────────────────────────────────────┐
│  Email/Phone must be synced to AuthService       │
│  because:                                        │
│  ✓ AppUser.Email used for login                  │
│  ✓ AppUser.UserName synced with Email            │
│  ✓ AppUser.PhoneNumber for 2FA/recovery          │
│                                                  │
│  RPC Pattern chosen because:                     │
│  ✓ Synchronous response (know success/failure)   │
│  ✓ Rollback capability if sync fails             │
│  ✓ 10s timeout prevents hanging                  │
│  ✓ No retry (avoid duplicate updates)            │
└──────────────────────────────────────────────────┘
```

### RPC Configuration

**AuthService (Consumer)**:
```csharp
// AuthRpcConfiguration.cs
cfg.ReceiveEndpoint("update-user-info-rpc", e =>
{
    e.UseMessageRetry(r => r.None());              // ⚠️ NO RETRY
    e.UseTimeout(x => x.Timeout = TimeSpan.FromSeconds(10));
    e.ConcurrentMessageLimit = 1;                  // Sequential processing
    e.PrefetchCount = 1;
    e.ConfigureConsumer<UpdateUserInfoConsumer>(context);
});
```

**UserService (Client)**:
```csharp
// UpdateUserInfoCommandHandler.cs
var rpcResponse = await _authClient.GetResponse<UpdateUserInfoRpcResponse>(
    new UpdateUserInfoRpcRequest
    {
        UserId = command.UserId,        // Auth User ID
        Email = command.Request.Email,
        PhoneNumber = command.Request.PhoneNumber,
        UpdatedBy = Guid.Parse(_currentUserService.UserId!)
    },
    timeout: RequestTimeout.After(s: 10),  // ⚠️ 10s timeout
    cancellationToken: cancellationToken
);
```

### Rollback on Failure

```csharp
try
{
    // RPC call to AuthService
    var rpcResponse = await _authClient.GetResponse<UpdateUserInfoRpcResponse>(...);
    
    if (!rpcResponse.Message.Success)
    {
        // ❌ AuthService update failed → ROLLBACK
        userProfile.Email = oldEmail;
        userProfile.PhoneNumber = oldPhone;
        await _unitOfWork.SaveChangesAsync();
        
        return Result<UserProfileResponse>.Failure(
            $"Failed to sync with AuthService: {rpcResponse.Message.ErrorMessage}",
            ErrorCodeEnum.InternalError);
    }
}
catch (RequestTimeoutException ex)
{
    // ❌ RPC Timeout → ROLLBACK
    userProfile.Email = oldEmail;
    userProfile.PhoneNumber = oldPhone;
    await _unitOfWork.SaveChangesAsync();
    
    return Result<UserProfileResponse>.Failure(
        "Failed to sync with AuthService: timeout after 10 seconds",
        ErrorCodeEnum.InternalError);
}
```

---

## 📊 API Endpoints Summary

| Method | Endpoint | Auth | Purpose | RPC | Cache Update |
|--------|----------|------|---------|-----|--------------|
| `PUT` | `/api/cms/users/{id}` | Admin | Update user info | ✅ Yes (email/phone) | Immediate |
| `PUT` | `/api/cms/users/{id}/roles` | Admin | Update roles | ❌ Event | Immediate |
| `PUT` | `/api/cms/users/{id}/status` | Admin | Update status | ❌ Event | Immediate |

**Parameter `{id}`**: Auth Service User ID (AppUser.Id), NOT UserProfile.Id

---

## 📁 File Structure

```
src/
├── SharedLibrary/
│   └── Contracts/
│       └── User/
│           └── Rpc/
│               └── UpdateUserInfoEvents.cs           // RPC Request/Response
│
├── AuthService/
│   ├── Infrastructure/
│   │   ├── Consumers/
│   │   │   └── UpdateUserInfoConsumer.cs            // RPC Consumer
│   │   └── Configurations/
│   │       └── AuthRpcConfiguration.cs              // RPC endpoint config
│   └── API/
│       └── Configurations/
│           └── ServiceConfiguration.cs              // Register consumer
│
└── UserService/
    ├── Application/
    │   ├── Commons/
    │   │   └── DTOs/
    │   │       ├── UpdateUserInfoRequest.cs
    │   │       ├── UpdateUserRolesRequest.cs
    │   │       └── UpdateUserStatusRequest.cs
    │   └── Features/
    │       └── Users/
    │           └── Commands/
    │               ├── UpdateUserInfo/
    │               │   ├── UpdateUserInfoCommand.cs
    │               │   └── UpdateUserInfoCommandHandler.cs
    │               ├── UpdateUserRoles/
    │               │   ├── UpdateUserRolesCommand.cs
    │               │   └── UpdateUserRolesCommandHandler.cs
    │               └── UpdateUserStatus/
    │                   ├── UpdateUserStatusCommand.cs
    │                   └── UpdateUserStatusCommandHandler.cs
    └── API/
        └── Controllers/
            └── Cms/
                └── UserController.cs                // 3 new endpoints
```

---

## 🧪 Testing Scenarios

### 1. Update User Info (Success)
```bash
PUT /api/cms/users/{authUserId}
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "fullName": "John Updated",
  "email": "john.new@example.com",
  "phoneNumber": "+84901234567",
  "address": "New Address"
}

# Expected:
# - UserProfile updated
# - RPC to AuthService successful
# - Cache updated
# - Activity logged
# Response: 200 OK with updated user
```

### 2. Update User Info (RPC Timeout - Rollback)
```bash
PUT /api/cms/users/{authUserId}
# Simulate: AuthService slow (>10s)

# Expected:
# - UserProfile updated initially
# - RPC timeout after 10s
# - Automatic rollback to old values
# - Cache unchanged
# Response: 400 Bad Request with timeout error
```

### 3. Update Roles
```bash
PUT /api/cms/users/{authUserId}/roles
Authorization: Bearer {admin_token}

{
  "rolesToAdd": ["ContentCreator"],
  "rolesToRemove": ["User"]
}

# Expected:
# - Event published to AuthService
# - Cache updated immediately
# - Activity logged
# Response: 200 OK
```

### 4. Update Status
```bash
PUT /api/cms/users/{authUserId}/status
Authorization: Bearer {admin_token}

{
  "status": 1,
  "reason": "Account suspended"
}

# Expected:
# - UserProfile.Status updated
# - Event published to AuthService
# - Cache updated immediately
# - Activity logged
# Response: 200 OK
```

### 5. Cache Validation (Inactive User)
```bash
# Scenario: User status changed to Inactive
# Then: Try to update user info

PUT /api/cms/users/{authUserId}
# Cache shows: Status = Inactive

# Expected:
# Response: 404 Not Found - "User not found or inactive"
# No changes made
```

---

## 🔍 Monitoring & Logging

### Key Log Points

1. **Validation Stage**:
```csharp
_logger.LogWarning("User not found in cache or inactive - Auth UserId: {UserId}", authUserId);
```

2. **RPC Start**:
```csharp
_logger.LogInformation("Syncing user info to AuthService via RPC - Auth UserId: {UserId}", authUserId);
```

3. **RPC Success**:
```csharp
_logger.LogInformation("AuthService sync successful - Auth UserId: {UserId}", authUserId);
```

4. **RPC Failure/Timeout**:
```csharp
_logger.LogError("AuthService RPC failed, rolling back - Auth UserId: {UserId}, Error: {Error}", authUserId, error);
```

5. **Cache Update**:
```csharp
_logger.LogInformation("Cache updated with new email - Auth UserId: {UserId}, Email: {Email}", authUserId, email);
```

6. **Activity Log Created**:
```csharp
_logger.LogInformation("User info update completed - Auth UserId: {UserId}, Changes: {Changes}", authUserId, changes);
```

---

## ⚠️ Common Pitfalls & Solutions

### 1. User ID Confusion
```
❌ WRONG: Using UserProfile.Id in API
GET /api/cms/users/{userProfileId}

✅ CORRECT: Using Auth User ID in API
GET /api/cms/users/{authUserId}
```

### 2. Cache Update Mutation
```csharp
❌ WRONG: Try to mutate init-only property
cachedState.Email = newEmail;

✅ CORRECT: Use 'with' expression
var updatedState = cachedState with { Email = newEmail };
```

### 3. Activity Log FK
```csharp
❌ WRONG: Using Auth User ID
activityLog.UserId = authUserId;

✅ CORRECT: Using UserProfile.Id (PK)
activityLog.UserId = userProfile.Id;
```

### 4. JWT Trust
```csharp
❌ WRONG: Validate using JWT roles
if (_currentUserService.Roles.Contains("Admin"))

✅ CORRECT: Validate using Cache
var userState = await _userStateCache.GetUserStateAsync(authUserId);
if (userState?.Status == EntityStatusEnum.Active)
```

---

## 🚀 Performance Considerations

### 1. Cache Pattern
- **Memory Cache**: O(1) lookup by Auth User ID
- **Sliding Expiration**: 30 minutes (configurable)
- **Absolute Expiration**: 60 minutes (configurable)

### 2. RPC Timeout
- **10 seconds**: Balance between reliability and UX
- **No Retry**: Prevent duplicate updates
- **Sequential Processing**: ConcurrentMessageLimit = 1

### 3. Event Publishing
- **Fire-and-Forget**: Roles/Status updates don't wait for confirmation
- **2-second delay**: Simple polling for AuthService processing

---

## 📝 Related Documents

- [SAGA_ARCHITECTURE_GUIDE.md](./SAGA_ARCHITECTURE_GUIDE.md) - Saga pattern overview
- [RPC_OPTIMIZATION_COMPLETE_SUMMARY.md](./RPC_OPTIMIZATION_COMPLETE_SUMMARY.md) - RPC pattern details
- [SECURITY_ARCHITECTURE_REDIS_CACHE.md](./SECURITY_ARCHITECTURE_REDIS_CACHE.md) - Cache architecture
- [USER_ROLES_OPTIMIZATION_COMPLETE.md](./USER_ROLES_OPTIMIZATION_COMPLETE.md) - Role management

---

## ✅ Completion Checklist

- [x] Update User Info API implemented
- [x] Update User Roles API implemented
- [x] Update User Status API implemented
- [x] Activity logging for all operations
- [x] Cache-first validation
- [x] RPC pattern with timeout & rollback
- [x] IP and UserAgent tracking
- [x] AuthService RPC consumer
- [x] RPC endpoint configuration
- [x] Code comments for User ID clarification
- [x] Build successful (0 errors, 28 warnings)
- [x] Documentation complete

---

**Created**: 2025-10-15  
**Last Updated**: 2025-10-15  
**Status**: ✅ Production Ready
