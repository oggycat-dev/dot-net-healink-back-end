using SharedLibrary.Commons.Entities;
using PaymentService.Domain.Enums;
using SharedLibrary.Commons.Enums;

namespace PaymentService.Domain.Entities;

public class PaymentTransaction : BaseEntity
{
    /// <summary>
    /// Provider's transaction ID (e.g., MoMo's transId)
    /// Null at initial phase, set after payment callback
    /// </summary>
    public string? TransactionId { get; set; }
    
    public Guid PaymentMethodId { get; set; }
    public TransactionType TransactionType { get; set; } = TransactionType.Subscription;
    
    /// <summary>
    /// CRITICAL: ReferenceId is used as OrderId for payment gateway
    /// - For Subscription: ReferenceId = SubscriptionId (becomes MoMo's OrderId)
    /// - For Order: ReferenceId = OrderId
    /// - For Refund: ReferenceId = OriginalTransactionId
    /// </summary>
    public Guid ReferenceId { get; set; }
    
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public virtual PaymentMethod PaymentMethod { get; set; } = null!;
    public PayementStatus PaymentStatus { get; set; } = PayementStatus.Pending;
}


