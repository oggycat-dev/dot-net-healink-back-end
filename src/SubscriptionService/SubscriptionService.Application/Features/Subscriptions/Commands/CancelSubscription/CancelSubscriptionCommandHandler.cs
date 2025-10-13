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

public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, Result>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CancelSubscriptionCommandHandler> _logger;
    private readonly IMapper _mapper;

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

    public async Task<Result> Handle(
        CancelSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var repository = _unitOfWork.Repository<Subscription>();

            // Find subscription
            var subscription = await repository.GetFirstOrDefaultAsync(
                x => x.Id == request.Id,
                includes: x => x.Plan);
            if (subscription == null)
            {
                _logger.LogWarning("Subscription {SubscriptionId} not found for cancellation", request.Id);
                return Result.Failure(
                    "Subscription not found",
                    ErrorCodeEnum.NotFound);
            }

            // Check if already canceled
            if (subscription.SubscriptionStatus == SubscriptionStatus.Canceled)
            {
                _logger.LogWarning("Subscription {SubscriptionId} is already canceled", request.Id);
                return Result.Failure(
                    "Subscription is already canceled",
                    ErrorCodeEnum.ValidationFailed);
            }

            // Set cancellation details
            if (request.CancelAtPeriodEnd)
            {
                // Schedule cancellation at period end
                subscription.CancelAtPeriodEnd = true;
                subscription.CanceledAt = subscription.CurrentPeriodEnd;
                
                _logger.LogInformation(
                    "Subscription {SubscriptionId} scheduled for cancellation at period end ({CancelAt})",
                    request.Id,
                    subscription.CanceledAt);
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

            // Update metadata - CurrentUserService already validated by middleware
            var userId = Guid.Parse(_currentUserService.UserId!);
            subscription.UpdateEntity(userId);

            // Publish integration event using AutoMapper
            var cancelEvent = _mapper.Map<SubscriptionCanceledEvent>(subscription);
            cancelEvent = cancelEvent with 
            { 
                CanceledBy = userId,
                Reason = request.Reason,
                // Capture HTTP context for audit trail
                IpAddress = _currentUserService.IpAddress,
                UserAgent = _currentUserService.UserAgent
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
            return Result.Success(
                request.CancelAtPeriodEnd
                    ? "Subscription will be canceled at the end of the current period"
                    : "Subscription canceled immediately");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling subscription {SubscriptionId}", request.Id);
            return Result.Failure(
                "An error occurred while canceling the subscription",
                ErrorCodeEnum.InternalError);
        }
    }
}
