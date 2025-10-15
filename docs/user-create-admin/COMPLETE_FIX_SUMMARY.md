# User Creation Issues - Complete Fix Summary

## Overview

Fixed two critical issues that occurred during admin-initiated user creation in the Healink microservices architecture:

1. **Duplicate Key Violation** - `IX_UserProfiles_UserId` unique constraint error
2. **Optimistic Concurrency Failure** - Race condition in AuthService update

Both issues stem from the **distributed saga pattern** used for user creation and the concurrent nature of microservices.

---

## Issue 1: Duplicate Key Violation on UserProfile Creation

### Error
```
Npgsql.PostgresException (0x80004005): 23505: duplicate key value violates unique constraint "IX_UserProfiles_UserId"
DETAIL: Key (UserId)=(00000000-0000-0000-0000-000000000000) already exists.
```

### Root Cause

The saga pattern for user creation:
1. Pre-create `UserProfile` with `Status = Pending` (before AuthUser exists)
2. Publish event to AuthService to create `AuthUser`  
3. Update `UserProfile` with real `UserId` once `AuthUser` is created

**Problem**: `UserId` was:
- Required (`NOT NULL`)
- Had unique constraint
- Defaulted to `Guid.Empty` when not set

When creating multiple users simultaneously, all started with `UserId = 00000000-0000-0000-0000-000000000000`, violating uniqueness.

### Solution

#### 1. Made `UserId` Nullable

**File**: `UserProfile.cs`
```csharp
/// <summary>
/// User ID from Auth Service - nullable during pending state
/// </summary>
public Guid? UserId { get; set; }
```

#### 2. Filtered Unique Index

**File**: `UserDbContext.cs`
```csharp
entity.HasIndex(x => x.UserId)
    .IsUnique()
    .HasFilter("\"UserId\" IS NOT NULL"); // PostgreSQL filtered index

entity.Property(x => x.UserId).IsRequired(false); // Nullable
```

**Benefit**: Allows multiple `NULL` values (pending profiles), but enforces uniqueness once `UserId` is set.

#### 3. Database Migration

**Created**: `20251015100954_MakeUserIdNullableInUserProfile.cs`
```sql
-- Make UserId nullable
ALTER TABLE "UserProfiles" 
ALTER COLUMN "UserId" DROP NOT NULL;

-- Create filtered unique index
CREATE UNIQUE INDEX "IX_UserProfiles_UserId" 
ON "UserProfiles" ("UserId") 
WHERE "UserId" IS NOT NULL;
```

#### 4. Code Updates

Updated all handlers to handle nullable `UserId`:
- `CreateUserByAdminCommandHandler.cs`: Set `UserId = null` explicitly
- `GetUserByIdQueryHandler.cs`: Check `UserId.HasValue` before RPC
- `GetUsersQueryHandler.cs`: Filter null UserIds for role fetching
- `ApproveCreatorApplicationHandler.cs`: Use `UserId ?? Guid.Empty`
- `RejectCreatorApplicationHandler.cs`: Use `UserId ?? Guid.Empty`
- `UserProfileResponse.cs`: Changed to `Guid? UserId`

### Files Changed
```
src/UserService/UserService.Domain/Entities/UserProfile.cs
src/UserService/UserService.Infrastructure/Context/UserDbContext.cs
src/UserService/UserService.Application/Commons/DTOs/UserProfileResponse.cs
src/UserService/UserService.Application/Features/Users/Commands/CreateUserByAdmin/
src/UserService/UserService.Application/Features/Users/Queries/GetUserById/
src/UserService/UserService.Application/Features/Users/Queries/GetUsers/
src/UserService/UserService.Application/Features/CreatorApplications/Commands/
src/UserService/UserService.Infrastructure/Consumers/GetUserProfileByUserIdConsumer.cs
src/UserService/UserService.Infrastructure/Migrations/20251015100954_MakeUserIdNullableInUserProfile.cs
```

---

## Issue 2: Optimistic Concurrency Failure in AuthService

### Error
```
fail: AuthService.Infrastructure.Consumers.UpdateUserInfoConsumer[0]
RPC: Failed to update user - UserId: 3e71f02a-63ed-4bff-a5bf-4a1ff448ee8e
Errors: Optimistic concurrency failure, object has been modified.
```

### Root Cause

**Optimistic Concurrency Control** in ASP.NET Identity:
- `AppUser` extends `IdentityUser<Guid>` with built-in `ConcurrencyStamp`
- Entity Framework checks if database stamp matches entity stamp
- Concurrent operations (saga, RPC calls) modify same user simultaneously
- Stamp mismatch causes `DbUpdateConcurrencyException`

### Solution

Implemented **Retry Logic with Exponential Backoff**

**File**: `UpdateUserInfoConsumer.cs`

```csharp
public async Task Consume(ConsumeContext<UpdateUserInfoRpcRequest> context)
{
    const int MaxRetries = 3;
    var retryCount = 0;

    while (retryCount < MaxRetries)
    {
        try
        {
            // Get FRESH user from database
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId);
            
            // Update user...
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                
                // Check for concurrency error
                if (errors.Contains("Optimistic concurrency failure") || 
                    errors.Contains("concurrency"))
                {
                    retryCount++;
                    
                    if (retryCount < MaxRetries)
                    {
                        _logger.LogWarning(
                            "RPC: Concurrency conflict on attempt {Attempt}. Retrying...",
                            retryCount);
                        
                        // Exponential backoff: 100ms, 200ms, 300ms
                        await Task.Delay(100 * retryCount);
                        continue; // Retry
                    }
                }
                
                // Failed
                await context.RespondAsync(new UpdateUserInfoRpcResponse
                {
                    Success = false,
                    ErrorMessage = $"Failed to update user: {errors}"
                });
                return;
            }

            // Success
            await context.RespondAsync(new UpdateUserInfoRpcResponse
            {
                Success = true,
                UpdatedEmail = user.Email,
                UpdatedPhoneNumber = user.PhoneNumber
            });
            return;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            retryCount++;
            
            if (retryCount < MaxRetries)
            {
                _logger.LogWarning(ex, "DbUpdateConcurrencyException. Retrying...");
                await Task.Delay(100 * retryCount);
                continue; // Retry
            }
            
            // Max retries exceeded
            await context.RespondAsync(new UpdateUserInfoRpcResponse
            {
                Success = false,
                ErrorMessage = $"Concurrency conflict after {MaxRetries} retries"
            });
            return;
        }
    }
}
```

### Key Features

1. **Maximum 3 Retry Attempts**
2. **Fresh Data Each Retry** - Re-queries database for latest `ConcurrencyStamp`
3. **Exponential Backoff** - 100ms, 200ms, 300ms delays
4. **Dual Detection** - Catches both `IdentityResult` errors and `DbUpdateConcurrencyException`
5. **Comprehensive Logging** - Tracks each retry attempt

### Files Changed
```
src/AuthService/AuthService.Infrastructure/Consumers/UpdateUserInfoConsumer.cs
```

---

## Saga Workflow (After Fixes)

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. UserService: CreateUserByAdminCommand                       │
│    Create UserProfile:                                          │
│    - UserId: NULL          ← ✅ Multiple pending profiles OK   │
│    - Status: Pending                                            │
│    - Email: unique                                              │
└─────────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. AuthService: AdminUserCreationSaga                          │
│    Create AuthUser with real Guid                              │
│    May have concurrent updates → ✅ Retry handles this         │
└─────────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3. UserService: UpdateUserProfileUserId                        │
│    Update UserProfile:                                          │
│    - UserId: <real-guid>   ← ✅ Now unique constraint applies  │
│    - Status: Active                                             │
└─────────────────────────────────────────────────────────────────┘
```

---

## Deployment Steps

### 1. Apply Database Migration (UserService)

```bash
dotnet ef database update --context UserDbContext \
  --project src/UserService/UserService.Infrastructure \
  --startup-project src/UserService/UserService.API
```

### 2. Rebuild and Restart Services

```bash
# UserService
docker-compose up --build -d userservice-api

# AuthService
docker-compose up --build -d authservice-api
```

### 3. Verify Logs

**UserService** - Should see pending profiles created:
```
Pre-created UserProfile (Pending) - ProfileId: xxx, Email: user@example.com
Publishing AdminUserCreationStarted - CorrelationId: xxx
```

**AuthService** - Should see successful retries if concurrency occurs:
```
RPC: Concurrency conflict on attempt 1 - UserId: xxx. Retrying...
RPC: User info updated successfully - UserId: xxx
```

---

## Testing

### Test Case 1: Create Multiple Users Simultaneously

```bash
# Create 5 users concurrently
for i in {1..5}; do
  curl -X POST http://localhost:5001/api/admin/users \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $TOKEN" \
    -d "{
      \"email\": \"staff$i@healink.com\",
      \"fullName\": \"Staff $i\",
      \"password\": \"Password123!\",
      \"role\": \"Staff\"
    }" &
done
wait
```

**Expected**:
- ✅ All users created successfully
- ✅ No duplicate key errors
- ✅ No concurrency failures (or resolved via retry)

### Test Case 2: Verify Database State

```sql
-- Check for NULL UserIds (pending profiles)
SELECT Id, Email, UserId, Status 
FROM "UserProfiles" 
WHERE "UserId" IS NULL;

-- Check unique constraint still enforced
SELECT "UserId", COUNT(*) 
FROM "UserProfiles" 
WHERE "UserId" IS NOT NULL
GROUP BY "UserId" 
HAVING COUNT(*) > 1;
-- Should return 0 rows
```

---

## Monitoring

### Key Metrics to Watch

1. **UserService Logs**:
   - `Pre-created UserProfile (Pending)` - Count per minute
   - `Saga completed successfully` - Success rate
   - `Saga timeout` - Timeout rate (should be low)

2. **AuthService Logs**:
   - `Concurrency conflict on attempt X` - Retry frequency
   - `Concurrency failure after 3 attempts` - Failure rate (should be rare)
   - `User info updated successfully` - Success rate

### Alert Conditions

⚠️ **High Concurrency Retry Rate** (>50% of requests retry):
- Consider increasing retry count or delay
- Investigate concurrent operation patterns

🚨 **Frequent Max Retry Failures**:
- Indicates systemic issue
- May need request deduplication or queuing

---

## Related Documentation

- [UserId Nullable Fix](../user-create-admin/USERID_NULLABLE_FIX.md)
- [Concurrency Retry Fix](../user-management/CONCURRENCY_RETRY_FIX.md)
- [Saga Architecture Guide](../SAGA_ARCHITECTURE_GUIDE.md)
- [User Update APIs](../USER_UPDATE_APIS_COMPLETE.md)

---

## Benefits

### Reliability
- ✅ Handles concurrent user creation without errors
- ✅ Resolves transient concurrency conflicts automatically
- ✅ Maintains data consistency across services

### Scalability
- ✅ Supports multiple pending user creations simultaneously
- ✅ No artificial serialization of user creation requests
- ✅ Database-level constraint enforcement

### Observability
- ✅ Detailed logging for debugging
- ✅ Clear error messages for failures
- ✅ Retry attempts tracked for monitoring

### Maintainability
- ✅ Aligns with saga pattern best practices
- ✅ Idiomatic Entity Framework concurrency handling
- ✅ Well-documented code with clear comments

---

## Future Improvements

1. **Configurable Retry Settings**: Move retry count/delay to configuration
2. **Circuit Breaker**: Implement circuit breaker for persistent failures
3. **Metrics Collection**: Export retry metrics to monitoring system
4. **Request Deduplication**: Add message deduplication for idempotency
5. **Saga Monitoring Dashboard**: Visualize saga completion rates

---

## Conclusion

These fixes make the user creation flow production-ready by:
- Handling the distributed nature of the saga pattern
- Gracefully managing concurrent operations
- Maintaining data integrity
- Providing clear error messages and logging

Both fixes work together to ensure reliable user creation in a distributed microservices architecture.
