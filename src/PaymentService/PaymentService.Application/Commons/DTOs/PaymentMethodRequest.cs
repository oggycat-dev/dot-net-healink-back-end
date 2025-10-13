using PaymentService.Domain.Enums;
using SharedLibrary.Commons.Enums;

namespace PaymentService.Application.Commons.DTOs;

public record PaymentMethodRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public PaymentType Type { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public string? Configuration { get; init; }
    public EntityStatusEnum Status { get; init; }
}

