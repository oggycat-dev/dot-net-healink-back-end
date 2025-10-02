using SharedLibrary.Commons.Entities;
using PaymentService.Domain.Enums;

namespace PaymentService.Domain.Entities;

public class PaymentTransaction : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Guid PaymentProviderId { get; set; }
    public TransactionType TransactionType { get; set; } = TransactionType.Subscription;
    public Guid ReferenceId { get; set; } // SubscriptionId, OrderId, or OriginalTransactionId (for Refund)
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string ProviderChargeRef { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}


