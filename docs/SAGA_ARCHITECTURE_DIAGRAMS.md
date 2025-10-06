# 🏗️ Saga Architecture - Before vs After

## ❌ BEFORE: Saga in SharedLibrary (Anti-Pattern)

```
┌─────────────────────────────────────────────────────────────┐
│                    SharedLibrary                             │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  RegistrationSaga.cs                                  │  │
│  │  RegistrationSagaState.cs                             │  │
│  │  SagaDbContextExtensions.cs                           │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ Referenced by ALL services
                            ↓
    ┌──────────────┬────────────────┬────────────────┬────────────────┐
    │              │                │                │                │
    ↓              ↓                ↓                ↓                ↓
┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐
│  Auth    │  │   User   │  │Notification│ │Subscription│ │ Payment  │
│ Service  │  │ Service  │  │  Service   │ │  Service   │ │ Service  │
└──────────┘  └──────────┘  └──────────┘  └──────────┘  └──────────┘
     │             │              │              │              │
     ↓             ↓              ↓              ↓              ↓
┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐
│ Auth DB  │  │ User DB  │  │Notif DB  │  │ Subs DB  │  │Payment DB│
│          │  │          │  │          │  │          │  │          │
│ ✅ Saga  │  │ ❌ Saga  │  │ ❌ Saga  │  │ ❌ Saga  │  │ ❌ Saga  │
│   Table  │  │   Table  │  │   Table  │  │   Table  │  │   Table  │
└──────────┘  └──────────┘  └──────────┘  └──────────┘  └──────────┘

Problem: Every service gets saga table even if it doesn't need it!
```

## ✅ AFTER: Saga in AuthService (Best Practice)

```
┌─────────────────────────────────────────────────────────────┐
│                    SharedLibrary                             │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  MassTransitSagaConfiguration.cs (Generic)            │  │
│  │  Event Contracts (RegistrationStarted, etc.)          │  │
│  │  Base classes and interfaces                          │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ Generic configuration only
                            ↓
    ┌──────────────┬────────────────┬────────────────┬────────────────┐
    │              │                │                │                │
    ↓              ↓                ↓                ↓                ↓
┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐
│  Auth    │  │   User   │  │Notification│ │Subscription│ │ Payment  │
│ Service  │  │ Service  │  │  Service   │ │  Service   │ │ Service  │
│          │  │          │  │            │  │            │  │          │
│ ┌──────┐ │  │          │  │            │  │            │  │          │
│ │ Saga │ │  │          │  │            │  │            │  │          │
│ │Folder│ │  │          │  │            │  │            │  │          │
│ └──────┘ │  │          │  │            │  │            │  │          │
└──────────┘  └──────────┘  └──────────┘  └──────────┘  └──────────┘
     │             │              │              │              │
     ↓             ↓              ↓              ↓              ↓
┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐
│ Auth DB  │  │ User DB  │  │Notif DB  │  │ Subs DB  │  │Payment DB│
│          │  │          │  │          │  │          │  │          │
│ ✅ Saga  │  │ ✅ Clean │  │ ✅ Clean │  │ ✅ Clean │  │ ✅ Clean │
│   Table  │  │          │  │          │  │          │  │          │
└──────────┘  └──────────┘  └──────────┘  └──────────┘  └──────────┘

Solution: Only AuthService (saga owner) has saga table!
```

## 📊 Registration Saga Flow

```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │ 1. POST /register
       ↓
┌─────────────────────────────────────────────────────────────┐
│                      AuthService                             │
│                                                              │
│  Controller → RegistrationStarted Event                     │
│                      ↓                                       │
│  ┌────────────────────────────────────────────────────┐    │
│  │          RegistrationSaga (State Machine)          │    │
│  │                                                     │    │
│  │  State: Initial → Started                          │    │
│  │  Action: Publish SendOtpNotification               │    │
│  └────────────────────────────────────────────────────┘    │
│                      ↓                                       │
│            Save to RegistrationSagaStates table             │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       │ 2. SendOtpNotification Event
                       ↓
┌─────────────────────────────────────────────────────────────┐
│              NotificationService (Consumer)                  │
│                                                              │
│  Consume → Send Email/SMS → Publish OtpSent Event          │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       │ 3. OtpSent Event
                       ↓
┌─────────────────────────────────────────────────────────────┐
│                      AuthService                             │
│  ┌────────────────────────────────────────────────────┐    │
│  │          RegistrationSaga                          │    │
│  │  State: Started → OtpSent                          │    │
│  │  Action: Wait for OtpVerified                      │    │
│  └────────────────────────────────────────────────────┘    │
└──────────────────────┬──────────────────────────────────────┘
                       │
       ┌───────────────┘
       │ User verifies OTP
       │ 4. POST /verify-otp
       ↓
┌─────────────────────────────────────────────────────────────┐
│                      AuthService                             │
│  Controller → OtpVerified Event                             │
│                      ↓                                       │
│  ┌────────────────────────────────────────────────────┐    │
│  │          RegistrationSaga                          │    │
│  │  State: OtpSent → OtpVerified                      │    │
│  │  Action: Publish CreateAuthUser                    │    │
│  └────────────────────────────────────────────────────┘    │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       │ 5. CreateAuthUser Event
                       ↓
┌─────────────────────────────────────────────────────────────┐
│              AuthService (CreateAuthUserConsumer)            │
│                                                              │
│  Consume → Create User in DB → Publish AuthUserCreated     │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       │ 6. AuthUserCreated Event
                       ↓
┌─────────────────────────────────────────────────────────────┐
│                      AuthService                             │
│  ┌────────────────────────────────────────────────────┐    │
│  │          RegistrationSaga                          │    │
│  │  State: OtpVerified → AuthUserCreated              │    │
│  │  Action: Publish CreateUserProfile                 │    │
│  └────────────────────────────────────────────────────┘    │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       │ 7. CreateUserProfile Event
                       ↓
┌─────────────────────────────────────────────────────────────┐
│                      UserService                             │
│                                                              │
│  Consume → Create Profile → Publish UserProfileCreated     │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       │ 8. UserProfileCreated Event
                       ↓
┌─────────────────────────────────────────────────────────────┐
│                      AuthService                             │
│  ┌────────────────────────────────────────────────────┐    │
│  │          RegistrationSaga                          │    │
│  │  State: AuthUserCreated → Completed                │    │
│  │  Action: Publish SendWelcomeNotification           │    │
│  │  Result: Mark saga as complete                     │    │
│  └────────────────────────────────────────────────────┘    │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       │ 9. SendWelcomeNotification Event
                       ↓
┌─────────────────────────────────────────────────────────────┐
│              NotificationService                             │
│                                                              │
│  Consume → Send Welcome Email                               │
└─────────────────────────────────────────────────────────────┘

✅ Registration Complete!
```

## 🗄️ Database Schema: RegistrationSagaStates

```sql
-- ONLY in AuthService Database

CREATE TABLE "RegistrationSagaStates" (
    "CorrelationId" uuid PRIMARY KEY,           -- Unique saga instance
    "CurrentState" varchar(64) NOT NULL,        -- State: Started, OtpSent, etc.
    "Email" varchar(256) NOT NULL,              -- User email
    "EncryptedPassword" varchar(500),
    "FullName" varchar(100),
    "PhoneNumber" varchar(20),
    "OtpCode" varchar(10),
    "Channel" integer NOT NULL,                 -- Email or SMS
    "ExpiresInMinutes" integer NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "StartedAt" timestamptz,
    "OtpSentAt" timestamptz,
    "OtpVerifiedAt" timestamptz,
    "AuthUserCreatedAt" timestamptz,
    "UserProfileCreatedAt" timestamptz,
    "CompletedAt" timestamptz,
    "ErrorMessage" varchar(1000),
    "RetryCount" integer NOT NULL DEFAULT 0,
    "OtpTimeoutTokenId" uuid,
    "AuthUserId" uuid,                          -- ID from AuthService
    "UserProfileId" uuid,                       -- ID from UserService
    "IsCompleted" boolean NOT NULL DEFAULT false,
    "IsFailed" boolean NOT NULL DEFAULT false
);

-- Performance Indexes
CREATE INDEX "IX_RegistrationSagaStates_Email" ON "RegistrationSagaStates"("Email");
CREATE INDEX "IX_RegistrationSagaStates_CurrentState" ON "RegistrationSagaStates"("CurrentState");
CREATE INDEX "IX_RegistrationSagaStates_Email_State_Created" 
    ON "RegistrationSagaStates"("Email", "CurrentState", "CreatedAt");
CREATE INDEX "IX_RegistrationSagaStates_Status" 
    ON "RegistrationSagaStates"("IsCompleted", "IsFailed");
```

## 🎯 Saga State Transitions

```
┌─────────────────────────────────────────────────────────────┐
│                   Saga State Machine                         │
└─────────────────────────────────────────────────────────────┘

           RegistrationStarted Event
                     ↓
              ┌──────────┐
              │ Initial  │
              └────┬─────┘
                   │ Publish SendOtpNotification
                   ↓
              ┌──────────┐
              │ Started  │
              └────┬─────┘
                   │ OtpSent Event
                   ↓
              ┌──────────┐
              │ OtpSent  │
              └────┬─────┘
                   │ OtpVerified Event
                   ↓
              ┌──────────┐
              │OtpVerified│
              └────┬─────┘
                   │ Publish CreateAuthUser
                   ↓
              ┌──────────┐
              │AuthUser  │ ─── Failure ───┐
              │ Created  │                │
              └────┬─────┘                │
                   │ AuthUserCreated      │
                   │ Success              │
                   ↓                      ↓
              ┌──────────┐         ┌──────────┐
              │UserProfile│◄────────│ Failed   │
              │  Created │         └──────────┘
              └────┬─────┘
                   │ UserProfileCreated Success
                   ↓
              ┌──────────┐
              │Completed │
              │(Finalized)│
              └──────────┘

Error Path (Rollback):
              ┌──────────┐
              │AuthUser  │
              │ Created  │
              └────┬─────┘
                   │ UserProfileCreated Failure
                   ↓
              ┌──────────┐
              │ Rolling  │
              │   Back   │
              └────┬─────┘
                   │ Publish DeleteAuthUser
                   ↓
              ┌──────────┐
              │ Rolled   │
              │   Back   │
              └──────────┘
```

## 📦 Service Ownership Matrix

```
┌─────────────────┬──────────┬──────────┬──────────┬──────────┐
│   Component     │  Owner   │  Saga?   │  Events  │Consumers │
├─────────────────┼──────────┼──────────┼──────────┼──────────┤
│ RegistrationSaga│ Auth     │    ✅    │   Pub    │   Sub    │
│ RegistrationState│Auth     │    ✅    │    -     │    -     │
│ SendOtpNotif    │ Shared   │    -     │  Event   │  Notif   │
│ OtpSent         │ Shared   │    -     │  Event   │  Auth    │
│ OtpVerified     │ Shared   │    -     │  Event   │  Auth    │
│ CreateAuthUser  │ Shared   │    -     │  Event   │  Auth    │
│ AuthUserCreated │ Shared   │    -     │  Event   │  Auth    │
│ CreateProfile   │ Shared   │    -     │  Event   │  User    │
│ ProfileCreated  │ Shared   │    -     │  Event   │  Auth    │
│ WelcomeNotif    │ Shared   │    -     │  Event   │  Notif   │
└─────────────────┴──────────┴──────────┴──────────┴──────────┘

Legend:
✅ = Owned by service
Pub = Publishes events
Sub = Subscribes to events
Event = Message contract only
```

---

**Architecture Pattern**: Saga Orchestration Pattern  
**Owner**: AuthService  
**Participants**: UserService, NotificationService  
**Message Broker**: RabbitMQ  
**Persistence**: PostgreSQL (AuthService DB only)
