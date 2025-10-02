using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Services;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Contracts.Subscription;
using SubscriptionService.Application.Commons.DTOs;
using SubscriptionService.Domain.Entities;
using SubscriptionService.Domain.Enums;

namespace SubscriptionService.Application.Features.Subscriptions.Commands.CancelSubscription;

public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, Result<SubscriptionResponse>>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CancelSubscriptionCommandHandler> _logger;

    public CancelSubscriptionCommandHandler(
        IOutboxUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<CancelSubscriptionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<SubscriptionResponse>> Handle(
        CancelSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var repository = _unitOfWork.Repository<Subscription>();

            // Find subscription
            var subscriptions = await repository.FindAsync(
                x => x.Id == request.Id,
                includes: x => x.Plan);

            var subscription = subscriptions.FirstOrDefault();
            if (subscription == null)
            {
                _logger.LogWarning("Subscription {SubscriptionId} not found for cancellation", request.Id);
                return Result<SubscriptionResponse>.Failure(
                    "Subscription not found",
                    ErrorCodeEnum.NotFound);
            }

            // Check if already canceled
            if (subscription.SubscriptionStatus == SubscriptionStatus.Canceled)
            {
                _logger.LogWarning("Subscription {SubscriptionId} is already canceled", request.Id);
                return Result<SubscriptionResponse>.Failure(
                    "Subscription is already canceled",
                    ErrorCodeEnum.ValidationFailed);
            }

            // Set cancellation details
            if (request.CancelAtPeriodEnd)
            {
                // Schedule cancellation at period end
                subscription.CancelAtPeriodEnd = true;
                subscription.CancelAt = subscription.CurrentPeriodEnd;
                
                _logger.LogInformation(
                    "Subscription {SubscriptionId} scheduled for cancellation at period end ({CancelAt})",
                    request.Id,
                    subscription.CancelAt);
            }
            else
            {
                // Cancel immediately
                subscription.SubscriptionStatus = SubscriptionStatus.Canceled;
                subscription.CanceledAt = DateTime.UtcNow;
                subscription.CancelAtPeriodEnd = false;
                
                _logger.LogInformation(
                    "Subscription {SubscriptionId} canceled immediately",
                    request.Id);
            }

            // Update metadata
            var userId = _currentUserService.UserId != null
                ? Guid.Parse(_currentUserService.UserId)
                : (Guid?)null;
            subscription.UpdateEntity(userId);

            // Publish integration event
            var cancelEvent = new SubscriptionCanceledEvent
            {
                SubscriptionId = subscription.Id,
                UserProfileId = subscription.UserProfileId,
                SubscriptionPlanId = subscription.SubscriptionPlanId,
                PlanName = subscription.Plan.DisplayName,
                CancelAtPeriodEnd = request.CancelAtPeriodEnd,
                CancelAt = subscription.CancelAt,
                CanceledAt = subscription.CanceledAt,
                Reason = request.Reason,
                CanceledBy = userId
            };
            await _unitOfWork.AddOutboxEventAsync(cancelEvent);

            // Save changes
            await _unitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

            _logger.LogInformation(
                "Subscription {SubscriptionId} cancellation processed successfully by user {UserId}. Reason: {Reason}",
                request.Id,
                userId,
                request.Reason ?? "Not specified");

            // Return response
            var response = _mapper.Map<SubscriptionResponse>(subscription);
            return Result<SubscriptionResponse>.Success(
                response,
                request.CancelAtPeriodEnd
                    ? "Subscription will be canceled at the end of the current period"
                    : "Subscription canceled immediately");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling subscription {SubscriptionId}", request.Id);
            return Result<SubscriptionResponse>.Failure(
                "An error occurred while canceling the subscription",
                ErrorCodeEnum.InternalError);
        }
    }
}
