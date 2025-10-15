using MediatR;
using SharedLibrary.Commons.Models;

namespace SubscriptionService.Application.Features.Subscriptions.HandleSubscriptionSagaCommand;

/// <summary>
/// Unified command to handle subscription saga actions (Activate or Cancel)
/// Returns Result<object> with SubscriptionSagaResponse data for Activate, null for Cancel
/// </summary>
public record HandleSubscriptionSagaCommand : IRequest<Result<object>>
{
    public Guid SubscriptionId { get; init; }
    public SubscriptionSagaAction Action { get; init; }
    
    // For Activate
    public Guid? PaymentIntentId { get; init; }
    public string? PaymentProvider { get; init; }
    public string? TransactionId { get; init; }

    public Guid? UpdatedBy { get; init; }
    
    // For Cancel/Compensation
    public string? Reason { get; init; }
    public bool IsCompensation { get; init; }
}

/// <summary>
/// Action type for subscription saga
/// </summary>
public enum SubscriptionSagaAction
{
    Activate,
    Cancel
}
