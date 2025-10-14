using MediatR;
using PaymentService.Application.Commons.Models;
using SharedLibrary.Commons.Models;

namespace PaymentService.Application.Features.Payments.Commands.ProcessPayment;

public record ProcessPaymentCommand : IRequest<Result<PaymentIntentResult>>
{
    public Guid SubscriptionId { get; init; }
    public Guid PaymentMethodId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "VND";
    public string Description { get; init; } = string.Empty;
    public Dictionary<string, string>? Metadata { get; init; }
    public Guid? CreatedBy { get; init; }
}
