# Fix: Optimistic Concurrency Failure in UpdateUserInfoConsumer

## Problem

When updating user information in AuthService via RPC, we encountered this error:

```
AuthService.Infrastructure.Consumers.UpdateUserInfoConsumer[0]
RPC: Failed to update user - UserId: 3e71f02a-63ed-4bff-a5bf-4a1ff448ee8e, 
Errors: Optimistic concurrency failure, object has been modified.
```

### Root Cause

**Optimistic Concurrency Control** in ASP.NET Identity:

1. `AppUser` extends `IdentityUser<Guid>`, which includes a `ConcurrencyStamp` property
2. When `UserManager.UpdateAsync()` is called, Entity Framework checks if the database `ConcurrencyStamp` matches the entity's stamp
3. If another operation modified the user in between query and update, the stamps don't match
4. Entity Framework throws `DbUpdateConcurrencyException` or returns an error

**Why This Happens:**
- Multiple concurrent operations updating the same user (e.g., saga updating user during admin creation, RPC calls from UserService)
- User entity is queried, then modified, but database changes in between
- Common in distributed systems with multiple services

## Solution

Implemented **Retry Logic with Exponential Backoff** to handle transient concurrency conflicts.

### Implementation

**Updated File**: `UpdateUserInfoConsumer.cs`

```csharp
public async Task Consume(ConsumeContext<UpdateUserInfoRpcRequest> context)
{
    const int MaxRetries = 3;
    var retryCount = 0;

    while (retryCount < MaxRetries)
    {
        try
        {
            // Find user - get FRESH copy from database
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            
            // ... update logic ...
            
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                
                // ✅ Check if it's a concurrency error
                if (errors.Contains("Optimistic concurrency failure") || errors.Contains("concurrency"))
                {
                    retryCount++;
                    
                    if (retryCount < MaxRetries)
                    {
                        _logger.LogWarning(
                            "RPC: Concurrency conflict on attempt {Attempt} - UserId: {UserId}. Retrying...",
                            retryCount, request.UserId);
                        
                        // ⏱️ Exponential backoff: 100ms, 200ms, 300ms
                        await Task.Delay(100 * retryCount);
                        continue; // Retry the operation
                    }
                }
                
                // Other errors or max retries reached
                await context.RespondAsync(new UpdateUserInfoRpcResponse
                {
                    Success = false,
                    ErrorMessage = $"Failed to update user: {errors}",
                    UserId = request.UserId
                });
                return;
            }

            // ✅ Success
            await context.RespondAsync(new UpdateUserInfoRpcResponse
            {
                Success = true,
                UserId = request.UserId,
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
                _logger.LogWarning(ex,
                    "RPC: DbUpdateConcurrencyException on attempt {Attempt} - UserId: {UserId}. Retrying...",
                    retryCount, context.Message.UserId);
                
                // ⏱️ Exponential backoff
                await Task.Delay(100 * retryCount);
                continue; // Retry the operation
            }
            
            // Max retries reached
            _logger.LogError(ex, "RPC: Concurrency failure after {Retries} attempts - UserId: {UserId}",
                MaxRetries, context.Message.UserId);
            
            await context.RespondAsync(new UpdateUserInfoRpcResponse
            {
                Success = false,
                ErrorMessage = $"Concurrency conflict after {MaxRetries} retries. Please try again.",
                UserId = context.Message.UserId
            });
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RPC: Error updating user info - UserId: {UserId}", context.Message.UserId);
            
            await context.RespondAsync(new UpdateUserInfoRpcResponse
            {
                Success = false,
                ErrorMessage = "Internal error updating user info",
                UserId = context.Message.UserId
            });
            return;
        }
    }
}
```

## Key Features

### 1. **Retry Loop**
- Maximum of **3 attempts**
- Each iteration fetches a fresh copy of the user from the database
- Ensures `ConcurrencyStamp` is up-to-date

### 2. **Exponential Backoff**
```csharp
await Task.Delay(100 * retryCount);
```
- Attempt 1: No delay
- Attempt 2: 100ms delay
- Attempt 3: 200ms delay

Gives time for concurrent operations to complete.

### 3. **Dual Concurrency Detection**

**Method 1**: Check `IdentityResult` errors
```csharp
if (errors.Contains("Optimistic concurrency failure") || errors.Contains("concurrency"))
```

**Method 2**: Catch `DbUpdateConcurrencyException`
```csharp
catch (DbUpdateConcurrencyException ex)
```

### 4. **Logging**
- Logs each retry attempt with attempt number
- Logs final failure after max retries
- Helps diagnose persistent concurrency issues

## Retry Flow

```
┌────────────────────────────────────────────────────────┐
│ Attempt 1: Query user, update, save                   │
│ ↓                                                       │
│ Concurrency error? → Retry with 100ms delay            │
└────────────────────────────────────────────────────────┘
                        ↓
┌────────────────────────────────────────────────────────┐
│ Attempt 2: Query user (fresh), update, save           │
│ ↓                                                       │
│ Concurrency error? → Retry with 200ms delay            │
└────────────────────────────────────────────────────────┘
                        ↓
┌────────────────────────────────────────────────────────┐
│ Attempt 3: Query user (fresh), update, save           │
│ ↓                                                       │
│ Success → Return success response                      │
│ OR                                                      │
│ Concurrency error → Return failure (max retries)       │
└────────────────────────────────────────────────────────┘
```

## Benefits

1. **Handles Transient Conflicts**: Most concurrency conflicts resolve on retry
2. **Fresh Data**: Each retry fetches the latest user data from database
3. **Exponential Backoff**: Reduces database load and allows time for conflicts to resolve
4. **Graceful Degradation**: Returns clear error message after max retries
5. **Observable**: Detailed logging for monitoring and debugging

## Testing

### Scenario 1: Single Update (No Conflict)
- **Expected**: Success on first attempt
- **Result**: No retry needed

### Scenario 2: Concurrent Updates
- **Expected**: First attempt may fail, succeeds on retry
- **Result**: Log shows retry, final success

### Scenario 3: Persistent Conflict
- **Expected**: Fails after 3 attempts
- **Result**: Clear error message to client

## Alternative Solutions (Not Used)

### 1. **Pessimistic Locking**
```csharp
// Not recommended for distributed systems
var user = await _context.Users
    .FromSqlRaw("SELECT * FROM Users WHERE Id = {0} FOR UPDATE", userId)
    .FirstOrDefaultAsync();
```
❌ Causes deadlocks in distributed systems

### 2. **Last Write Wins**
```csharp
_context.Entry(user).State = EntityState.Modified;
await _context.SaveChangesAsync();
```
❌ Can lose data, violates consistency

### 3. **Remove Concurrency Check**
```csharp
[ConcurrencyCheck]
public string ConcurrencyStamp { get; set; }
```
❌ Cannot remove from `IdentityUser`, built-in property

## Related Files

- **Consumer**: `src/AuthService/AuthService.Infrastructure/Consumers/UpdateUserInfoConsumer.cs`
- **Entity**: `src/AuthService/AuthService.Domain/Entities/AppUser.cs` (extends `IdentityUser<Guid>`)
- **Contract**: `src/SharedLibrary/Contracts/User/Rpc/UpdateUserInfoRpcRequest.cs`

## Best Practices

1. **Always fetch fresh data** before update in retry scenarios
2. **Use exponential backoff** to avoid hammering the database
3. **Limit retry attempts** (3-5 is standard)
4. **Log retry attempts** for monitoring
5. **Return clear error messages** when max retries exceeded

## Monitoring

Watch for these log patterns:

**Success after retry**:
```
RPC: Concurrency conflict on attempt 1 - UserId: xxx. Retrying...
RPC: User info updated successfully - UserId: xxx
```

**Failure after max retries** (needs investigation):
```
RPC: Concurrency conflict on attempt 1 - UserId: xxx. Retrying...
RPC: Concurrency conflict on attempt 2 - UserId: xxx. Retrying...
RPC: Concurrency failure after 3 attempts - UserId: xxx
```

If you see frequent failures after max retries, consider:
- Increasing max retry count
- Investigating why so many concurrent updates to same user
- Adding request deduplication/queuing

## Additional Notes

This fix complements the **UserId nullable fix** for `CreateUserByAdminCommand`. Both issues were related to concurrent operations during user creation:

1. **UserId nullable fix**: Allows multiple pending UserProfiles during saga
2. **Concurrency retry fix**: Handles concurrent updates to AppUser during saga

Together, these fixes make the user creation flow more resilient in distributed scenarios.
