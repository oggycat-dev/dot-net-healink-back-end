using SharedLibrary.Commons.Entities;

namespace PaymentService.Domain.Entities;

public class Invoice : BaseEntity
{
    public Guid UserProfileId { get; set; }
    public Guid PaymentProviderId { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public DateTime? DueDate { get; set; }
    public string Discounts { get; set; } = "[]";
    public string CorrelationId { get; set; } = string.Empty;
}

