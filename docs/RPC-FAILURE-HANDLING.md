# 🔧 RPC Failure Handling - PaymentFailed Event

## 🎯 Vấn Đề

### ❌ Scenario: RPC Call Fails

```
Timeline:
1. RegisterSubscriptionCommandHandler:
   ├─ Create Subscription ✅
   ├─ Publish SubscriptionRegistrationStarted ✅
   ├─ SaveChanges (ATOMIC commit) ✅
   ├─ RPC: CreatePaymentIntent → PaymentService
   │  └─ ❌ FAILS (timeout hoặc error)
   └─ Return error to user ❌

2. RegisterSubscriptionSaga:
   ├─ Receive SubscriptionRegistrationStarted ✅
   ├─ State: Initial → AwaitingPayment ✅
   └─ ⏳ Wait for PaymentSucceeded/PaymentFailed...
      └─ ❌ NEVER RECEIVES (stuck forever!)

Result:
❌ Subscription stuck ở Pending status
❌ Saga stuck ở AwaitingPayment state
❌ No compensation triggered
❌ Orphaned subscription record
```

---

## ✅ Giải Pháp

### Publish PaymentFailed Event When RPC Fails

Có 2 failure scenarios cần handle:

#### 1. RPC Returns Error (paymentResult.Success = false)
```csharp
if (!paymentResult.Success)
{
    // ✅ Publish PaymentFailed event
    await _publishEndpoint.Publish(new PaymentFailed
    {
        PaymentIntentId = Guid.Empty,
        SubscriptionId = subscription.Id,
        Reason = "Payment intent creation failed via RPC",
        ErrorCode = "RPC_FAILED",
        ErrorMessage = paymentResult.ErrorMessage,
        FailedAt = DateTime.UtcNow
    });
    
    return Result.Failure("Failed to initialize payment");
}
```

#### 2. RPC Timeout (RequestTimeoutException after 30s)
```csharp
catch (RequestTimeoutException timeoutEx)
{
    // ✅ Publish PaymentFailed event
    await _publishEndpoint.Publish(new PaymentFailed
    {
        PaymentIntentId = Guid.Empty,
        SubscriptionId = subscriptionId,
        Reason = "Payment service RPC timeout (30s)",
        ErrorCode = "RPC_TIMEOUT",
        ErrorMessage = "Payment service did not respond in time",
        FailedAt = DateTime.UtcNow
    });
    
    return Result.Failure("Payment service timeout");
}
```

---

## 🔄 Updated Flow

### Complete Flow With RPC Failure Handling

```
Success Path:
═══════════════════════════════════════════════════════════════
1. HTTP Handler: Create + Publish + SaveChanges ✅
2. RPC: Get PaymentUrl ✅
3. Return PaymentUrl to user ✅
4. Saga: AwaitingPayment ✅
5. User pays → MoMo IPN → PaymentSucceeded ✅
6. Saga: Activate subscription ✅

Failure Path 1: RPC Error
═══════════════════════════════════════════════════════════════
1. HTTP Handler: Create + Publish + SaveChanges ✅
2. RPC: Error response ❌
3. ✅ Publish PaymentFailed event (NEW!)
4. Return error to user ❌
5. Saga receives PaymentFailed ✅
6. Saga: Trigger compensation → Cancel subscription ✅

Failure Path 2: RPC Timeout
═══════════════════════════════════════════════════════════════
1. HTTP Handler: Create + Publish + SaveChanges ✅
2. RPC: Timeout after 30s ⏱️
3. ✅ Publish PaymentFailed event (NEW!)
4. Return error to user ❌
5. Saga receives PaymentFailed ✅
6. Saga: Trigger compensation → Cancel subscription ✅
```

---

## 📊 Saga State Transitions

### Before Fix (❌)

```
RegisterSubscriptionSaga:

Initial 
  │
  │ SubscriptionRegistrationStarted
  ▼
AwaitingPayment
  │
  │ (RPC fails - no event)
  │ 
  ⏳ Waiting forever... ❌
  │
  └─ STUCK (no timeout mechanism)
```

### After Fix (✅)

```
RegisterSubscriptionSaga:

Initial 
  │
  │ SubscriptionRegistrationStarted
  ▼
AwaitingPayment
  │
  ├─ Success: PaymentSucceeded → Completed ✅
  │                             → ActivateSubscription
  │
  └─ Failure: PaymentFailed → Failed ✅
                            → CancelSubscription (compensation)
```

---

## 🔧 Code Changes

### 1. Store SubscriptionId for Exception Handling

**Line 206-207 - NEW:**
```csharp
// Store subscriptionId for exception handling (timeout case)
var subscriptionId = subscription.Id;
```

**Reason:** Need subscriptionId in catch block scope

---

### 2. Handle RPC Error Response

**Line 236-261 - UPDATED:**
```csharp
if (!paymentResult.Success)
{
    _logger.LogError(...);

    // ✅ NEW: Publish PaymentFailed event
    await _publishEndpoint.Publish(new PaymentFailed
    {
        PaymentIntentId = Guid.Empty,
        SubscriptionId = subscription.Id,
        Reason = "Payment intent creation failed via RPC",
        ErrorCode = "RPC_FAILED",
        ErrorMessage = paymentResult.ErrorMessage ?? "Failed to initialize payment",
        FailedAt = DateTime.UtcNow
    }, cancellationToken);

    _logger.LogInformation(
        "Published PaymentFailed event for SubscriptionId={SubscriptionId}. " +
        "Saga will trigger compensation.",
        subscription.Id);

    return Result<object>.Failure(...);
}
```

---

### 3. Handle RPC Timeout

**Line 337-365 - UPDATED:**
```csharp
catch (RequestTimeoutException timeoutEx)
{
    _logger.LogError(timeoutEx, "Payment RPC timeout...");
    
    // ✅ NEW: Publish PaymentFailed event
    try
    {
        await _publishEndpoint.Publish(new PaymentFailed
        {
            PaymentIntentId = Guid.Empty,
            SubscriptionId = subscriptionId,
            Reason = "Payment service RPC timeout (30s)",
            ErrorCode = "RPC_TIMEOUT",
            ErrorMessage = "Payment service did not respond in time",
            FailedAt = DateTime.UtcNow
        }, cancellationToken);

        _logger.LogInformation(
            "Published PaymentFailed event due to timeout. " +
            "Saga will trigger compensation.");
    }
    catch (Exception publishEx)
    {
        _logger.LogError(publishEx, "Failed to publish PaymentFailed event...");
        // Continue with error response even if publish fails
    }
    
    return Result<object>.Failure("Payment service timeout...");
}
```

**Note:** Try-catch around publish để đảm bảo user vẫn nhận error response ngay cả khi publish fail.

---

## ✅ Benefits

### 1. No Orphaned Subscriptions ✅
- RPC fail → Saga receives PaymentFailed
- Saga triggers compensation
- Subscription status: Pending → Canceled
- Clean database state

### 2. Automatic Cleanup ✅
- No manual intervention needed
- Saga handles rollback automatically
- Consistent with other failure paths

### 3. Better User Experience ✅
- User sees error message immediately
- Backend handles cleanup in background
- User can retry without duplicates

### 4. Complete Observability ✅
- All failure paths logged
- Events published for audit trail
- Easy to debug issues

---

## 🔍 Edge Cases Handled

### Case 1: Publish Event Also Fails
```csharp
try {
    await _publishEndpoint.Publish(new PaymentFailed {...});
} catch (Exception publishEx) {
    _logger.LogError(publishEx, "Failed to publish...");
    // Continue - user still gets error response
}
```

**Fallback:** 
- Saga có thể implement timeout mechanism (future work)
- Manual cleanup script có thể detect orphaned subscriptions
- Monitoring alerts cho stuck subscriptions

---

### Case 2: Duplicate PaymentFailed Events
**Scenario:** RPC timeout, then PaymentService publishes PaymentFailed sau đó

**Solution:**
- Saga idempotent - handle duplicate PaymentFailed gracefully
- Check subscription status before canceling
- If already Canceled → No-op

```csharp
// In RegisterSubscriptionSaga
When(PaymentFailed)
    .Then(context =>
    {
        // Idempotent - check if already failed
        if (context.Saga.IsFailed) return;
        
        // Update state
        context.Saga.IsFailed = true;
        // ...
    })
```

---

### Case 3: Network Partition
**Scenario:** RPC timeout, but PaymentService actually created payment intent

**Possible Issues:**
- Frontend shows error
- Saga cancels subscription
- But payment intent exists in PaymentService
- User might try to pay → Payment succeeds but subscription canceled

**Mitigation:**
- Payment callback should check subscription status
- If subscription canceled → Refund payment
- Log warning for manual review

**Future Work:**
- Implement payment refund flow
- Add correlation tracking for debugging

---

## 📈 Metrics to Monitor

### New Metrics

1. **RPC Failure Rate**
   - Metric: `payment_rpc_failures / total_rpc_calls`
   - Threshold: < 1%
   - Alert if > 5%

2. **PaymentFailed Event Count (by ErrorCode)**
   - `RPC_FAILED`: RPC returned error
   - `RPC_TIMEOUT`: RPC timed out
   - Track separately for analysis

3. **Saga Compensation Rate**
   - Metric: `canceled_subscriptions_by_saga / total_subscriptions`
   - Expected: Match RPC failure rate
   - Alert if mismatch (indicates missed compensation)

4. **Orphaned Subscription Detection**
   - Query: `Subscriptions WHERE Status=Pending AND CreatedAt < NOW() - 1 hour`
   - Should be: 0
   - Alert if > 0

---

## 🧪 Testing Scenarios

### Manual Testing

1. **Test RPC Error Response**
   ```
   Setup: Mock PaymentService to return error
   Action: Create subscription
   Expected:
   - User sees error message ✅
   - PaymentFailed event published ✅
   - Saga cancels subscription ✅
   - Subscription status = Canceled ✅
   ```

2. **Test RPC Timeout**
   ```
   Setup: Mock PaymentService to delay > 30s
   Action: Create subscription
   Expected:
   - User sees timeout error ✅
   - PaymentFailed event published ✅
   - Saga cancels subscription ✅
   ```

3. **Test Publish Failure**
   ```
   Setup: Mock RabbitMQ failure during publish
   Action: Trigger RPC error
   Expected:
   - User still sees error message ✅
   - Log shows publish failure ✅
   - Subscription remains Pending (for manual cleanup) ⚠️
   ```

---

## 🔮 Future Improvements

### 1. Saga Timeout Mechanism
```csharp
// Add timeout event to saga
Schedule(PaymentTimeout, 
    context => context.Init<PaymentTimeoutExpired>(new {...}),
    context => TimeSpan.FromMinutes(15));

When(PaymentTimeoutExpired)
    .Then(context => {
        _logger.LogWarning("Payment timeout for SubscriptionId...");
    })
    .PublishAsync(context => context.Init<CancelSubscription>(...))
    .TransitionTo(Failed);
```

### 2. Payment Refund Flow
- Detect payment success for canceled subscription
- Trigger refund via payment gateway
- Notify user about refund

### 3. Retry Mechanism
- Store failed payment attempts
- Allow user to retry with same subscription
- Track retry count

---

## 📝 Summary

### What Changed
- ✅ Store subscriptionId before RPC call
- ✅ Publish PaymentFailed when RPC error
- ✅ Publish PaymentFailed when RPC timeout
- ✅ Handle publish failures gracefully

### Why
- Prevent orphaned subscriptions
- Enable saga compensation
- Complete failure handling

### Impact
- ✅ No stuck subscriptions
- ✅ Automatic cleanup
- ✅ Better observability
- ✅ Complete failure paths

---

**Date:** 2025-10-31  
**Type:** Bug Fix - Failure Handling  
**Priority:** HIGH  
**Status:** ✅ **COMPLETED**  
**Breaking Change:** ❌ No

