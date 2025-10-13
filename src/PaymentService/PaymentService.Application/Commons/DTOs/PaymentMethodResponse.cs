namespace PaymentService.Application.Commons.DTOs;

public record PaymentMethodResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string TypeName { get; init; } = string.Empty;
    public int Type { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public string? Configuration { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? CreatedAt { get; init; }
    public Guid? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public Guid? UpdatedBy { get; init; }
}

