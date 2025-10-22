using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Repositories;
using SubscriptionService.Domain.Entities;
using SubscriptionService.Domain.Enums;

namespace SubscriptionService.Application.Features.Subscriptions.HandleSubscriptionSagaCommand;

/// <summary>
/// Unified handler for subscription saga actions (Activate or Cancel)
/// Uses switch case to handle different actions from saga
/// Returns subscription data in Result<object>.Data for Activate (cast to SubscriptionSagaResponse in consumer)
/// Cancel case returns Result<object> with null data
/// </summary>
public class HandleSubscriptionSagaCommandHandler : IRequestHandler<HandleSubscriptionSagaCommand, Result<object>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HandleSubscriptionSagaCommandHandler> _logger;

    public HandleSubscriptionSagaCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<HandleSubscriptionSagaCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<object>> Handle(HandleSubscriptionSagaCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get subscription
            var subscription = await _unitOfWork.Repository<Subscription>()
                .GetFirstOrDefaultAsync(
                    s => s.Id == request.SubscriptionId,
                    s => s.Plan); // Eager load Plan for both actions

            if (subscription == null)
            {
                _logger.LogWarning("Subscription not found: {SubscriptionId}", request.SubscriptionId);
                return Result<object>.Success(null!, "Subscription not found (idempotent)");
            }

            // ✅ Switch case to handle different saga actions
            switch (request.Action)
            {
                case SubscriptionSagaAction.Activate:
                    return await HandleActivateAsync(subscription, request, cancellationToken);

                case SubscriptionSagaAction.Cancel:
                    return await HandleCancelAsync(subscription, request, cancellationToken);

                default:
                    _logger.LogError("Unknown action: {Action}", request.Action);
                    return Result<object>.Failure("Unknown subscription saga action", ErrorCodeEnum.InternalError);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error handling subscription saga: Action={Action}, SubscriptionId={SubscriptionId}", 
                request.Action, request.SubscriptionId);
            return Result<object>.Failure("Error processing subscription saga action", ErrorCodeEnum.InternalError);
        }
    }

    /// <summary>
    /// Handle subscription activation after successful payment
    /// Returns subscription data in Result<object>.Data (no re-query needed in consumer)
    /// </summary>
    private async Task<Result<object>> HandleActivateAsync(
        Subscription subscription, 
        HandleSubscriptionSagaCommand request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Activating subscription: SubscriptionId={SubscriptionId}, PaymentIntentId={PaymentIntentId}",
            request.SubscriptionId, request.PaymentIntentId);

        var activatedAt = DateTime.UtcNow;

        // Check if already active (idempotent)
        if (subscription.SubscriptionStatus == SubscriptionStatus.Active)
        {
            _logger.LogInformation("Subscription already active: {SubscriptionId}", request.SubscriptionId);
            
            // Return existing data for idempotent notification
            var existingData = new SubscriptionSagaResponse
            {
                SubscriptionId = subscription.Id,
                UserProfileId = subscription.UserProfileId,
                SubscriptionPlanId = subscription.SubscriptionPlanId,
                SubscriptionPlanName = subscription.Plan.Name,
                SubscriptionPlanDisplayName = subscription.Plan.DisplayName,
                Amount = subscription.Plan.Amount,
                Currency = subscription.Plan.Currency,
                ActivatedAt = subscription.CurrentPeriodStart ?? activatedAt,
                CurrentPeriodStart = subscription.CurrentPeriodStart,
                CurrentPeriodEnd = subscription.CurrentPeriodEnd
            };
            
            return Result<object>.Success(existingData, "Subscription already active (idempotent)");
        }

        // Update subscription status
        subscription.SubscriptionStatus = SubscriptionStatus.Active;
        subscription.Status = EntityStatusEnum.Active;
        subscription.CurrentPeriodStart = activatedAt;
        subscription.ActivatedAt = activatedAt;
        subscription.CurrentPeriodEnd = subscription.Plan.BillingPeriodUnit == BillingPeriodUnit.Month
            ? activatedAt.AddMonths(subscription.Plan.BillingPeriodCount)
            : activatedAt.AddYears(subscription.Plan.BillingPeriodCount);
        subscription.UpdateEntity(request.UpdatedBy);

        _unitOfWork.Repository<Subscription>().Update(subscription);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Subscription activated successfully: SubscriptionId={SubscriptionId}",
            request.SubscriptionId);

        // ✅ Return subscription data for notification (consumer will cast to SubscriptionSagaResponse)
        var responseData = new SubscriptionSagaResponse
        {
            SubscriptionId = subscription.Id,
            UserProfileId = subscription.UserProfileId,
            SubscriptionPlanId = subscription.SubscriptionPlanId,
            SubscriptionPlanName = subscription.Plan.Name,
            SubscriptionPlanDisplayName = subscription.Plan.DisplayName,
            Amount = subscription.Plan.Amount,
            Currency = subscription.Plan.Currency,
            ActivatedAt = activatedAt,
            CurrentPeriodStart = subscription.CurrentPeriodStart,
            CurrentPeriodEnd = subscription.CurrentPeriodEnd
        };

        return Result<object>.Success(responseData, "Subscription activated successfully");
    }

    /// <summary>
    /// Handle subscription cancellation (compensation/rollback)
    /// Returns null data (no notification needed for cancel)
    /// </summary>
    private async Task<Result<object>> HandleCancelAsync(
        Subscription subscription, 
        HandleSubscriptionSagaCommand request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Canceling subscription: SubscriptionId={SubscriptionId}, IsCompensation={IsCompensation}, Reason={Reason}",
            request.SubscriptionId, request.IsCompensation, request.Reason);

        // Check if already canceled (idempotent)
        if (subscription.SubscriptionStatus == SubscriptionStatus.Canceled)
        {
            _logger.LogInformation("Subscription already canceled: {SubscriptionId}", request.SubscriptionId);
            return Result<object>.Success(null!, "Subscription already canceled (idempotent)");
        }

        // Mark as canceled + inactive (for audit)        
        subscription.SubscriptionStatus = SubscriptionStatus.Canceled;
        subscription.Status = EntityStatusEnum.Inactive;
        subscription.CanceledAt = DateTime.UtcNow;
        subscription.CancelReason = request.Reason;
        subscription.UpdateEntity(request.UpdatedBy);

        _unitOfWork.Repository<Subscription>().Update(subscription);

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Subscription canceled successfully: SubscriptionId={SubscriptionId}, Reason={Reason}",
            request.SubscriptionId, request.Reason);

        // ✅ Return null data (no notification for cancel case)
        return Result<object>.Success(null!, "Subscription canceled successfully");
    }
}