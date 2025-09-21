# Registration Saga - Updated Workflow

## Flow Overview

Đã cập nhật workflow để tạo đúng cả **AppUser** (trong AuthService) và **UserProfile** (trong UserService):

### New Workflow States:
```
Initial → Started → OtpSent → OtpVerified → AuthUserCreated → UserProfileCreated → Completed
                      ↓           ↓             ↓                ↓
                   Failed      Failed        Failed           Failed
```

## Updated Events Flow

### 1. Registration Request
- Client gửi `POST /api/auth/register`
- AuthService tạo OTP và publish `RegistrationStarted`
- Saga chuyển to **Started** state

### 2. OTP Notification
- Saga publish `SendOtpNotification` → NotificationService
- NotificationService gửi email/SMS và publish `OtpSent`
- Saga chuyển to **OtpSent** state

### 3. OTP Verification
- Client gửi `POST /api/auth/verify-otp`
- AuthService verify OTP và publish `OtpVerified`
- Saga chuyển to **OtpVerified** state

### ⚡ **NEW: 4. Create Auth User (AppUser)**
- Saga publish `CreateAuthUser` → **AuthService**
- AuthService tạo **AppUser** với Identity framework
- AuthService publish `AuthUserCreated` với **UserId**
- Saga chuyển to **AuthUserCreated** state

### ⚡ **NEW: 5. Create User Profile**
- Saga publish `CreateUserProfile` → **UserService**
- UserService tạo **UserProfile** với **UserId** làm foreign key
- UserService publish `UserProfileCreated`
- Saga chuyển to **UserProfileCreated** state

### 6. Complete Registration
- Saga publish `SendWelcomeNotification`
- Saga publish `RegistrationCompleted`
- Saga **Finalized**

## Key Changes Made

### 1. New Events Added:
- `CreateAuthUser` / `AuthUserCreated`
- `CreateUserProfile` / `UserProfileCreated`

### 2. Updated Saga State:
```csharp
public class RegistrationSagaState 
{
    // ...existing properties...
    
    // NEW: Track both user creation steps
    public DateTime? AuthUserCreatedAt { get; set; }
    public DateTime? UserProfileCreatedAt { get; set; }
    
    // NEW: Store both IDs
    public Guid? AuthUserId { get; set; }        // AppUser ID từ AuthService
    public Guid? UserProfileId { get; set; }     // UserProfile ID từ UserService
}
```

### 3. New Consumers:
- **AuthService**: `CreateAuthUserConsumer` - Tạo AppUser
- **UserService**: `CreateUserProfileConsumer` - Tạo UserProfile với UserId

### 4. Updated Database Schema:
```sql
-- Migration cần thêm columns mới vào RegistrationSagaState table
ALTER TABLE RegistrationSagaState 
ADD AuthUserCreatedAt TIMESTAMP NULL,
    UserProfileCreatedAt TIMESTAMP NULL,
    AuthUserId UUID NULL,
    UserProfileId UUID NULL;
```

## Data Flow Example

```
1. Registration Request:
   {
     "email": "user@example.com",
     "password": "password123",
     "fullName": "John Doe",
     "phoneNumber": "+1234567890"
   }

2. After OTP Verified → CreateAuthUser:
   {
     "correlationId": "guid",
     "email": "user@example.com", 
     "encryptedPassword": "encrypted_hash",
     "fullName": "John Doe",
     "phoneNumber": "+1234567890"
   }

3. AuthUserCreated Response:
   {
     "correlationId": "guid",
     "userId": "auth-user-guid",  ← AppUser ID
     "success": true
   }

4. CreateUserProfile Command:
   {
     "correlationId": "guid",
     "userId": "auth-user-guid",     ← Foreign Key
     "email": "user@example.com",
     "fullName": "John Doe", 
     "phoneNumber": "+1234567890"
   }

5. UserProfileCreated Response:
   {
     "correlationId": "guid",
     "userProfileId": "profile-guid",  ← UserProfile ID
     "userId": "auth-user-guid",       ← Reference
     "success": true
   }
```

## Benefits of This Approach

### ✅ **Proper Authentication**
- AppUser tạo trong AuthService với Identity framework
- Support login, password reset, role management

### ✅ **Proper Data Separation**
- AuthService: Authentication data (AppUser, roles, permissions)
- UserService: Business data (UserProfile, business roles)

### ✅ **Foreign Key Relationship**
- UserProfile.UserId → AppUser.Id
- Đảm bảo data integrity

### ✅ **Fault Tolerance**
- Nếu tạo AppUser thành công nhưng UserProfile fail → Saga retry
- Rollback và cleanup nếu cần thiết

### ✅ **Idempotency**
- Check duplicate trước khi tạo
- Safe retry operations

## Next Steps for Implementation

1. **Run Database Migration** cho Saga state table
2. **Update Service Configurations** để register consumers mới
3. **Test End-to-End Workflow**
4. **Monitor Saga States** trong production

Workflow này đảm bảo bạn có đầy đủ cả authentication account (AppUser) và business profile (UserProfile) với relationship đúng!