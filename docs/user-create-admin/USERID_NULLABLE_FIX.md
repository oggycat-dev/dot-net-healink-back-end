# Fix: Duplicate Key Violation on UserProfile Creation

## Problem

When creating users via `CreateUserByAdminCommand`, we encountered this error:

```
Npgsql.PostgresException (0x80004005): 23505: duplicate key value violates unique constraint "IX_UserProfiles_UserId"
```

### Root Cause

The issue occurred because of the **Saga pattern** used for user creation:

1. **Pre-create UserProfile** with `Status = Pending` (before AuthUser exists)
2. **Publish event** to AuthService to create AuthUser
3. **Update UserProfile** with real `UserId` once AuthUser is created

The problem was that `UserId` was:
- **Required** (`NOT NULL`)
- **Had a unique constraint**
- **Defaulted to `Guid.Empty`** when not set

When creating multiple users simultaneously, they all started with `UserId = Guid.Empty`, violating the unique constraint.

## Solution

### 1. Make `UserId` Nullable

**Entity Update** (`UserProfile.cs`):
```csharp
/// <summary>
/// User ID từ Auth Service (Foreign Key)
/// Nullable to allow pre-creation of UserProfile before AuthUser exists (Saga pattern)
/// Will be set to actual UserId when AuthUser is created
/// </summary>
public Guid? UserId { get; set; }
```

### 2. Use Filtered Unique Index

**DbContext Configuration** (`UserDbContext.cs`):
```csharp
// UserId unique index - using filtered index to allow multiple NULL values
// This allows pre-creation of UserProfile (with NULL UserId) during Saga workflow
// Once UserId is set, it must be unique
entity.HasIndex(x => x.UserId)
    .IsUnique()
    .HasFilter("\"UserId\" IS NOT NULL"); // PostgreSQL syntax for filtered index

entity.Property(x => x.UserId).IsRequired(false); // Make nullable
```

### 3. Update DTO

**UserProfileResponse.cs**:
```csharp
/// <summary>
/// UserId from AuthService - nullable during pending state
/// </summary>
public Guid? UserId { get; set; }
```

### 4. Database Migration

**Migration created**: `20251015100954_MakeUserIdNullableInUserProfile.cs`

The migration:
- Changes `UserId` column to nullable (`uuid NULL`)
- Drops the old unique index
- Creates a filtered unique index: `WHERE "UserId" IS NOT NULL`

This allows multiple UserProfiles with `NULL` UserId (during pending state), but enforces uniqueness once UserId is set.

### 5. Code Updates

Updated all code locations that use `UserId` to handle nullable values:

**Handler Updates**:
- `CreateUserByAdminCommandHandler.cs`: Explicitly set `UserId = null` for pending profiles
- `GetUserByIdQueryHandler.cs`: Check `UserId.HasValue` before RPC calls
- `GetUsersQueryHandler.cs`: Filter out users with null UserId for role fetching
- `ApproveCreatorApplicationHandler.cs`: Use `UserId ?? Guid.Empty` for events
- `RejectCreatorApplicationHandler.cs`: Use `UserId ?? Guid.Empty` for events
- `GetUserProfileByUserIdConsumer.cs`: Use `UserId ?? Guid.Empty` in response
- `InternalController.cs`: Use `UserId?.ToString() ?? string.Empty`

**Consumer Updates**:
- `UpdateUserProfileUserIdConsumer.cs`: Already had idempotency check for `UserId`

## Saga Workflow Now

```
┌─────────────────────────────────────────────────────────────────────┐
│ 1. CreateUserByAdminCommand                                         │
│    ↓                                                                 │
│    Create UserProfile:                                               │
│    - UserId: NULL            ← Allows multiple pending profiles     │
│    - Status: Pending                                                 │
│    - Email: unique                                                   │
└─────────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────────┐
│ 2. AdminUserCreationStarted Event → AuthService                     │
│    ↓                                                                 │
│    AuthService creates AuthUser with real Guid                      │
└─────────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────────┐
│ 3. UpdateUserProfileUserId Command → UserService                    │
│    ↓                                                                 │
│    Update UserProfile:                                               │
│    - UserId: <real-guid>     ← Now unique constraint applies        │
│    - Status: Active                                                  │
└─────────────────────────────────────────────────────────────────────┘
```

## Benefits

1. **Solves duplicate key error**: Multiple pending profiles can coexist
2. **Maintains data integrity**: Uniqueness enforced once UserId is set
3. **Saga-friendly**: Supports pre-creation pattern used in RegistrationSaga
4. **Database-level enforcement**: Filtered index provides constraint at DB level
5. **Backward compatible**: Migration handles existing data

## Testing

To apply the migration:

```bash
dotnet ef database update --context UserDbContext \
  --project src/UserService/UserService.Infrastructure \
  --startup-project src/UserService/UserService.API
```

To test:
1. Create multiple users via admin API simultaneously
2. Verify they all start with `UserId = NULL` and `Status = Pending`
3. Verify saga completes and updates UserId correctly
4. Verify no duplicate key errors occur

## Related Files

- **Entity**: `src/UserService/UserService.Domain/Entities/UserProfile.cs`
- **DbContext**: `src/UserService/UserService.Infrastructure/Context/UserDbContext.cs`
- **Migration**: `src/UserService/UserService.Infrastructure/Migrations/20251015100954_MakeUserIdNullableInUserProfile.cs`
- **Handler**: `src/UserService/UserService.Application/Features/Users/Commands/CreateUserByAdmin/CreateUserByAdminCommandHandler.cs`
- **DTO**: `src/UserService/UserService.Application/Commons/DTOs/UserProfileResponse.cs`

## Note

This fix aligns with the pattern already used in the **RegistrationSaga** where UserProfile is pre-created before AuthUser exists. The key difference is that now we properly handle the nullable UserId during the pending state.
