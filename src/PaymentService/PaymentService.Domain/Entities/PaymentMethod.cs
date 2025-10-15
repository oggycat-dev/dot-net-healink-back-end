using PaymentService.Domain.Enums;
using SharedLibrary.Commons.Entities;

namespace PaymentService.Domain.Entities;

public class PaymentMethod : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public PaymentType Type { get; set; }
    public string ProviderName { get; set; } = null!;
    public string? Configuration { get; set; }
}