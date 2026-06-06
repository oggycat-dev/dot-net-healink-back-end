# 📊 Register Subscription Flow - Compact Sequence Diagram

## 🎯 Phiên Bản Ngắn Gọn - Thể Hiện Rõ Các Cụm Service

```mermaid
sequenceDiagram
    autonumber
    
    box rgb(230, 240, 255) User & Gateway
        participant User
        participant Gateway as API Gateway
    end
    
    box rgb(255, 240, 230) Subscription Service
        participant SubAPI as Subscription API
        participant SubHandler as Register Handler
        participant SubDB as Subscription DB
    end
    
    box rgb(240, 255, 240) Payment Service
        participant PayAPI as Payment API
        participant PayHandler as Payment Handler
        participant PayDB as Payment DB
    end
    
    box rgb(255, 255, 230) External Gateway
        participant MoMo as MoMo Gateway
    end
    
    box rgb(250, 240, 255) Saga Orchestration
        participant RabbitMQ
        participant Saga as Subscription Saga
    end
    
    box rgb(240, 250, 255) Supporting Services
        participant Cache as Redis Cache
        participant Notify as Notification
    end
    
    %% ============================================================
    %% Phase 1: Registration & Payment Intent (Synchronous RPC)
    %% ============================================================
    rect rgb(230, 240, 255)
        Note over User,Cache: Phase 1: Registration & Payment Intent Creation
        User->>Gateway: POST /subscriptions/register<br/>{planId, paymentMethodId}
        Gateway->>SubAPI: Route with JWT
        
        SubAPI->>SubHandler: RegisterSubscriptionCommand
        SubHandler->>Cache: Get UserProfileId from cache
        Cache-->>SubHandler: UserState
        
        SubHandler->>SubDB: Create Subscription (Pending)
        SubHandler->>SubDB: ✅ Publish(SubscriptionRegistrationStarted)<br/>BEFORE SaveChanges
        SubHandler->>SubDB: ✅ SaveChanges (ATOMIC:<br/>Subscription + OutboxState)
        
        Note over SubHandler,PayHandler: RPC Call for Payment URL
        SubHandler->>RabbitMQ: RPC: CreatePaymentIntent
        RabbitMQ->>PayAPI: Request
        PayAPI->>PayHandler: ProcessPaymentCommand
        
        alt RPC Success
            PayHandler->>PayDB: Create Transaction (Pending)
            PayHandler->>MoMo: POST /create {orderId, amount}
            MoMo-->>PayHandler: {payUrl, qrCodeUrl}
            PayHandler->>PayDB: Save Transaction
            
            PayHandler-->>PayAPI: PaymentIntentCreated
            PayAPI-->>RabbitMQ: Response
            RabbitMQ-->>SubHandler: PaymentIntentCreated
            
            SubHandler-->>SubAPI: {PaymentUrl, QrCode}
            SubAPI-->>Gateway: 200 OK
            Gateway-->>User: Payment Data
            
        else RPC Error/Timeout
            Note over SubHandler: RPC fails or times out
            SubHandler->>RabbitMQ: ✅ Publish PaymentFailed<br/>(ErrorCode: RPC_FAILED/RPC_TIMEOUT)
            SubHandler-->>SubAPI: Error Response
            SubAPI-->>Gateway: 500 Error
            Gateway-->>User: Error: Payment service unavailable
        end
    end
    
    %% ============================================================
    %% Phase 2: Saga Initialization & RPC Failure Handling (Background)
    %% ============================================================
    rect rgb(250, 240, 255)
        Note over RabbitMQ,Saga: Phase 2: Saga Orchestration & Failure Handling
        RabbitMQ->>Saga: SubscriptionRegistrationStarted<br/>(from OutboxState)
        Saga->>Saga: State: Initial → AwaitingPayment<br/>✅ Tracking payment status
        
        alt If RPC Failed (from Phase 1)
            RabbitMQ->>Saga: PaymentFailed (RPC_FAILED/RPC_TIMEOUT)
            Saga->>Saga: State: AwaitingPayment → Failed
            Saga->>RabbitMQ: Publish CancelSubscription<br/>(Compensation)
            
            RabbitMQ->>SubAPI: CancelSubscription Command
            SubAPI->>SubHandler: HandleSagaCommand (Cancel)
            SubHandler->>SubDB: Update: Status = Canceled
            Note over Saga: ✅ Automatic cleanup for RPC failures
        end
    end
    
    %% ============================================================
    %% Phase 3: User Payment
    %% ============================================================
    rect rgb(255, 255, 230)
        Note over User,MoMo: Phase 3: User Completes Payment
        User->>MoMo: Scan QR / Pay via MoMo App
        MoMo->>MoMo: Process Payment
    end
    
    %% ============================================================
    %% Phase 4: IPN Callback & Verification
    %% ============================================================
    rect rgb(240, 255, 240)
        Note over MoMo,Saga: Phase 4: Payment Callback & Saga Activation
        MoMo->>Gateway: POST /payment-callback/ipn
        Gateway->>PayAPI: Route to PaymentService
        
        PayAPI->>PayHandler: VerifyMomoIpnCommand
        PayHandler->>PayHandler: Validate IP + Signature
        PayHandler->>PayDB: Find Transaction by OrderId
        
        alt Payment Success
            PayHandler->>PayDB: Update: Status = Succeeded
            PayHandler->>RabbitMQ: Publish PaymentSucceeded
            PayHandler-->>MoMo: 200 OK
            
            RabbitMQ->>Saga: PaymentSucceeded
            Saga->>Saga: State: AwaitingPayment → Completed
            Saga->>RabbitMQ: Publish ActivateSubscription
            
            RabbitMQ->>SubAPI: ActivateSubscription Command
            SubAPI->>SubHandler: HandleSagaCommand (Activate)
            SubHandler->>SubDB: Update: Status = Active
            SubHandler-->>SubAPI: Success
            
            SubAPI->>RabbitMQ: Fire Events:<br/>1. SubscriptionActivated<br/>2. CacheSync
            
            RabbitMQ->>Cache: Update Subscription Cache
            RabbitMQ->>Notify: Send Email Notification
            
        else Payment Failed
            PayHandler->>PayDB: Update: Status = Failed
            PayHandler->>RabbitMQ: Publish PaymentFailed
            PayHandler-->>MoMo: 200 OK
            
            RabbitMQ->>Saga: PaymentFailed
            Saga->>Saga: State: AwaitingPayment → Failed
            Saga->>RabbitMQ: Publish CancelSubscription
            
            RabbitMQ->>SubAPI: CancelSubscription Command
            SubAPI->>SubHandler: HandleSagaCommand (Cancel)
            SubHandler->>SubDB: Update: Status = Canceled
        end
    end
    
    Note over User,Notify: ✅ End of Flow
```

---

## 🎯 Giải Thích Các Cụm Service

### 1. 🔵 User & Gateway (Blue)
- **User**: End user
- **API Gateway**: Ocelot - routing, authentication, load balancing

### 2. 🟠 Subscription Service (Orange)
- **Subscription API**: HTTP endpoints
- **Register Handler**: Business logic cho đăng ký subscription
- **Subscription DB**: PostgreSQL - lưu subscriptions

### 3. 🟢 Payment Service (Green)
- **Payment API**: HTTP endpoints + RPC consumer
- **Payment Handler**: Xử lý payment logic + MoMo integration
- **Payment DB**: PostgreSQL - lưu transactions

### 4. 🟡 External Gateway (Yellow)
- **MoMo Gateway**: Third-party payment provider

### 5. 🟣 Saga Orchestration (Purple)
- **RabbitMQ**: Message broker
- **Subscription Saga**: MassTransit state machine - orchestrate workflow

### 6. 🔷 Supporting Services (Light Blue)
- **Redis Cache**: User state + subscription cache
- **Notification**: Email/SMS notifications

---

## ✅ Key Features Highlighted

### 🔒 Outbox Pattern (Fixed)
```
Line 53-54:
✅ Publish(SubscriptionRegistrationStarted) BEFORE SaveChanges
✅ SaveChanges (ATOMIC: Subscription + OutboxState)
```

**Benefit:**
- Zero message loss
- Guaranteed saga initialization
- Auto-retry if RabbitMQ down

### 🔄 RPC Pattern with Failure Handling (NEW!)
```
Line 61-81:
Alt path:
  Success: Get PaymentUrl immediately
  Failure: Publish PaymentFailed → Saga compensation
```

**Benefit:**
- Immediate PaymentUrl for success case
- Automatic cleanup for RPC failures (NEW!)
- No orphaned subscriptions (NEW!)

### 🎭 Saga Pattern
```
Line 88-100:
Background process handles saga orchestration
State transitions: 
  - Initial → AwaitingPayment → Completed/Failed (payment)
  - AwaitingPayment → Failed (RPC error - NEW!)
```

**Benefit:**
- Reliable orchestration
- Automatic compensation on all failures

### ⚡ Cache Sync
```
Line 122:
Async event updates Redis cache for fast subscription checks
```

---

## 📊 Flow Summary

| Phase | Duration | Type | Services Involved |
|-------|----------|------|-------------------|
| **1. Registration** | ~1-2s | Synchronous | User → Gateway → SubService → PaymentService → MoMo |
| **2. Saga Init** | ~100ms | Background | RabbitMQ → Saga |
| **3. User Payment** | Variable | User Action | User → MoMo |
| **4. Activation** | ~500ms | Async | MoMo → PaymentService → Saga → SubService → Cache/Notify |

**Total Time (User Perspective):**
- Registration API: 1-2 seconds (get PaymentUrl)
- Payment: User dependent
- Activation: Background (~500ms after payment)

---

## 🔍 Differences from Original (Simplified)

| Original | Compact |
|----------|---------|
| ~135 lines | ~110 lines |
| 13 participants | 11 participants (grouped) |
| Detailed implementation | High-level flow |
| Every DB operation shown | Grouped operations |
| All intermediate steps | Key steps only |

**What's Preserved:**
- ✅ All service boundaries
- ✅ Outbox pattern implementation
- ✅ RPC flow
- ✅ Saga orchestration
- ✅ Success/failure paths
- ✅ Cache sync

**What's Simplified:**
- 🔹 Internal handler details
- 🔹 Database query specifics
- 🔹 QR code generation
- 🔹 Validation steps
- 🔹 Multiple return paths consolidated

---

## 🎨 Color Coding

- 🔵 **Blue**: Client-facing (User, Gateway)
- 🟠 **Orange**: Subscription domain
- 🟢 **Green**: Payment domain
- 🟡 **Yellow**: External dependencies
- 🟣 **Purple**: Infrastructure (messaging, saga)
- 🔷 **Light Blue**: Supporting services

---

**Version:** Compact v2.1  
**Last Updated:** 2025-10-31 (Afternoon)  
**Status:** ✅ Complete Failure Handling (Outbox + RPC Failures)

