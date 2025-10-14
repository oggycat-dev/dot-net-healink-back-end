# Subscription System Documentation Index

## 📚 Documentation Overview

This directory contains comprehensive documentation for the Healink Subscription System with MassTransit Saga orchestration, distributed transactions, payment gateway integration, and notification services.

---

## 📖 Main Documents

### 1. [Complete Documentation](SUBSCRIPTION_SYSTEM_COMPLETE_DOCUMENTATION.md)

**Full system documentation with detailed Mermaid diagrams**

**Content**:
- ✅ System Overview & Architecture
- ✅ 8 Detailed Mermaid Diagrams:
  - High-level architecture diagram
  - Microservices communication pattern
  - Saga compensation pattern
  - Complete sequence diagram (50+ steps)
  - Payment gateway integration flow
  - 3 State machine diagrams (Saga, Subscription, PaymentTransaction)
  - End-to-end flow diagram
- ✅ Technical Components Deep Dive
- ✅ Security & Validation (IP whitelist, HMAC signature)
- ✅ Error Handling & Compensation
- ✅ Database Schema
- ✅ API Endpoints
- ✅ Monitoring & Observability

**When to Use**: When you need complete understanding of the system architecture and implementation

---

### 2. [Quick Reference Guide](SUBSCRIPTION_SYSTEM_QUICK_REFERENCE.md)

**Quick start guide with practical examples and templates**

**Content**:
- ✅ Quick Architecture Overview
- ✅ Component Checklist (all files)
- ✅ Key Patterns:
  - UserId vs UserProfileId
  - Hybrid Outbox Pattern
  - RPC Request-Response
  - Saga Compensation
- ✅ Configuration Templates (copy-paste ready)
- ✅ Security Checklist
- ✅ Database Quick Reference
- ✅ Testing Scenarios
- ✅ Monitoring Queries
- ✅ Common Issues & Solutions
- ✅ Best Practices

**When to Use**: During development, troubleshooting, or quick reference

---

### 3. [RefreshToken UserProfileId Fix](REFRESH_TOKEN_USERPROFILEID_FIX.md)

**Bug fix documentation for token refresh cache preservation**

**Content**:
- Problem: RefreshTokenCommandHandler missing UserProfileId in event
- Solution: Query from cache and include in UserLoggedInEvent
- Impact: Prevents cache overwrite and notification failures

---

## 🎯 Mermaid Diagrams Summary

### Architecture Diagrams

1. **High-Level System Architecture**
   - All microservices and their interactions
   - RabbitMQ message bus
   - Redis cache
   - External MoMo gateway

2. **Microservices Communication**
   - Synchronous: RPC pattern
   - Asynchronous: Event-driven

3. **Saga Compensation Pattern**
   - Success path
   - Failure path with rollback

### Sequence Diagrams

4. **Complete Subscription Flow** (Most Detailed)
   - 7 phases with 50+ steps
   - Success and failure branches
   - Shows all service interactions

5. **Payment Gateway Integration**
   - Factory pattern usage
   - Request builder
   - Response validator & parser

### State Machine Diagrams

6. **Subscription Saga States**
   - Initial → AwaitingPayment → PaymentCompleted/Failed

7. **Subscription Entity States**
   - Pending → Active → Expired/Canceled

8. **Payment Transaction States**
   - Pending → Succeeded/Failed/Expired → Refunded

### Flow Diagrams

9. **End-to-End Flow**
   - Decision points
   - Error handling paths
   - Complete user journey

---

## 🔑 Key Concepts

### 1. Authentication Pattern

```
JWT Token (authUserId) → For audit fields (CreatedBy/UpdatedBy)
Redis Cache (UserProfileId) → For business logic (foreign keys)
```

### 2. Saga Orchestration

```
Saga State Machine (RegisterSubscriptionSaga)
├─ CorrelationId = SubscriptionId (unified tracking)
├─ Entity Framework Outbox (transactional)
├─ Compensation on failure (soft delete)
└─ Optimistic concurrency control (ISagaVersion)
```

### 3. Hybrid Outbox Pattern

```
MassTransit Bus Outbox → Saga orchestration (critical)
Custom Outbox → Activity logging (immediate)
```

### 4. Payment Gateway Factory

```
PaymentGatewayFactory
├─ IPaymentGatewayService interface
├─ MomoService (HMAC SHA256, IP whitelist)
└─ Helpers (RequestBuilder, ResponseValidator, ResponseParser)
```

### 5. Distributed Transactions

```
Phase 1: Create Subscription (Pending) + Custom Outbox
Phase 2: RPC Payment Intent (Synchronous) + MassTransit Outbox
Phase 3: Saga Start (AwaitingPayment state)
Phase 4: User Payment (External)
Phase 5: IPN Callback (IP validation + Signature verification)
Phase 6: Saga Continue/Compensation
Phase 7: Notification (Fire-and-forget)
```

---

## 🛠️ Quick Start

### For Developers

1. **Understanding Architecture**: Start with [Complete Documentation](SUBSCRIPTION_SYSTEM_COMPLETE_DOCUMENTATION.md) → Section 1-2
2. **Implementing**: Use [Quick Reference](SUBSCRIPTION_SYSTEM_QUICK_REFERENCE.md) → Configuration Templates
3. **Troubleshooting**: Check [Quick Reference](SUBSCRIPTION_SYSTEM_QUICK_REFERENCE.md) → Common Issues

### For Architects

1. **System Design**: [Complete Documentation](SUBSCRIPTION_SYSTEM_COMPLETE_DOCUMENTATION.md) → All Diagrams
2. **Technical Decisions**: [Complete Documentation](SUBSCRIPTION_SYSTEM_COMPLETE_DOCUMENTATION.md) → Conclusion section

### For DevOps

1. **Deployment**: [Complete Documentation](SUBSCRIPTION_SYSTEM_COMPLETE_DOCUMENTATION.md) → Environment Configuration
2. **Monitoring**: [Quick Reference](SUBSCRIPTION_SYSTEM_QUICK_REFERENCE.md) → Monitoring Queries

---

## 📊 System Stats

| Metric | Count |
|--------|-------|
| **Microservices** | 5 (Subscription, Payment, Notification, User, Auth) |
| **Saga State Machines** | 1 (RegisterSubscriptionSaga) |
| **Consumers** | 6 (Create, Activate, Cancel, Verify, Notify, Activity) |
| **Commands** | 4 (Register, Process, Verify, Handle) |
| **Events** | 7 (Registration, Payment, Activation, Cancellation, Notification, Activity) |
| **Database Tables** | 5 (Subscription, Transaction, SagaState, Outbox x3) |
| **API Endpoints** | 3 (Register, GetMy, IPN) |
| **Mermaid Diagrams** | 9 (Architecture, Sequence, State, Flow) |

---

## 🔒 Security Features

- ✅ JWT Authentication (API Gateway)
- ✅ Distributed Authentication (Redis cache)
- ✅ IP Whitelist (MoMo IPN - CIDR notation)
- ✅ HMAC SHA256 Signature Verification
- ✅ Idempotency (Duplicate IPN handling)
- ✅ Audit Trail (CreatedBy/UpdatedBy with authUserId)

---

## 🧪 Testing Coverage

### Success Flow
- User registers subscription
- Payment intent created (RPC)
- User pays via MoMo
- IPN callback received
- Subscription activated
- Notification sent

### Failure Flow
- Payment fails
- Saga compensation triggered
- Subscription canceled (soft delete)
- No orphan transactions

### Edge Cases
- Duplicate IPN callbacks (idempotency)
- Unauthorized IP (rejection)
- Invalid signature (rejection)
- Amount mismatch (rejection)
- Stuck sagas (monitoring + cleanup)

---

## 📈 Performance Considerations

- **Entity Framework Outbox**: Pessimistic locking (PostgreSQL)
- **Message Retry**: 5 retries with exponential backoff
- **RPC Timeout**: 30 seconds
- **Duplicate Detection Window**: 30 seconds
- **Outbox Query Delay**: 1 second
- **Optimistic Concurrency**: Saga state version field

---

## 🚀 Production Readiness

| Component | Status | Notes |
|-----------|--------|-------|
| **Architecture** | ✅ Ready | Clean Architecture + DDD |
| **Saga Orchestration** | ✅ Ready | MassTransit + EF Outbox |
| **Payment Gateway** | ✅ Ready | MoMo AIO v2 with IP whitelist |
| **Security** | ✅ Ready | JWT + Distributed Auth + Signature |
| **Compensation** | ✅ Ready | Soft delete for audit |
| **Notification** | ✅ Ready | Fire-and-forget email |
| **Monitoring** | ✅ Ready | SQL queries + logs |
| **Documentation** | ✅ Ready | Complete with diagrams |

---

## 📞 Support

For questions or issues:
1. Check [Quick Reference - Common Issues](SUBSCRIPTION_SYSTEM_QUICK_REFERENCE.md#common-issues--solutions)
2. Review [Complete Documentation - Error Handling](SUBSCRIPTION_SYSTEM_COMPLETE_DOCUMENTATION.md#error-handling--compensation)
3. Contact development team

---

**Last Updated**: 2025-01-12  
**Version**: 1.0  
**Status**: ✅ Production Ready

---

## 🎉 Summary

This subscription system is a **production-ready, enterprise-grade implementation** featuring:

- ✅ **Distributed Transaction Management** with MassTransit Saga
- ✅ **Event-Driven Architecture** with RabbitMQ
- ✅ **Payment Gateway Integration** with factory pattern
- ✅ **Security Best Practices** (IP whitelist, signature verification)
- ✅ **Comprehensive Documentation** with 9 Mermaid diagrams
- ✅ **Complete Audit Trail** with proper user tracking
- ✅ **Compensation Pattern** for failure handling
- ✅ **Clean Architecture** principles throughout

**Start with**: [Complete Documentation](SUBSCRIPTION_SYSTEM_COMPLETE_DOCUMENTATION.md) for architecture overview  
**Quick Reference**: [Quick Reference Guide](SUBSCRIPTION_SYSTEM_QUICK_REFERENCE.md) for implementation


