using MassTransit;
using SharedLibrary.Commons.Enums;

namespace SubscriptionService.Infrastructure.Saga;

/// <summary>
/// Saga state for managing subscription registration workflow
/// Tracks the entire lifecycle from subscription creation to payment and activation
/// Implements ISagaVersion for optimistic concurrency control
/// </summary>
public class RegisterSubscriptionSagaState : SagaStateMachineInstance, ISagaVersion
{
    /// <summary>
    /// Correlation ID - Maps to SubscriptionId for unified tracking
    /// This allows us to track subscription state across the entire workflow
    /// </summary>
    public Guid CorrelationId { get; set; }
    
    /// <summary>
    /// Version for optimistic concurrency control
    /// Prevents race conditions during saga state updates
    /// </summary>
    public int Version { get; set; }
    
    /// <summary>
    /// Current state of the saga (e.g., "AwaitingPayment", "PaymentProcessing", "Completed")
    /// </summary>
    public string CurrentState { get; set; } = null!;
    
    // Subscription Information
    public Guid UserProfileId { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public Guid PaymentMethodId { get; set; }
    public string? SubscriptionPlanName { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    
    // Payment Tracking
    public Guid? PaymentIntentId { get; set; }
    public string? PaymentStatus { get; set; }
    public string? PaymentProvider { get; set; }
    public string? TransactionId { get; set; }

    public Guid? CreatedBy { get; set; }
    
    // Timestamps
    public DateTime StartedAt { get; set; }
    public DateTime? PaymentRequestedAt { get; set; }
    public DateTime? PaymentCompletedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    
    // Error Handling
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    
    // Status Flags
    public bool IsPaymentCompleted { get; set; }
    public bool IsSubscriptionActivated { get; set; }
    public bool IsFailed { get; set; }
}