using AutoMapper;
using FluentValidation;
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

namespace SubscriptionService.Application.Features.Subscriptions.Commands.UpdateSubscription;

public class UpdateSubscriptionCommandHandler : IRequestHandler<UpdateSubscriptionCommand, Result<SubscriptionResponse>>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<UpdateSubscriptionCommand> _validator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateSubscriptionCommandHandler> _logger;

    public UpdateSubscriptionCommandHandler(
        IOutboxUnitOfWork unitOfWork,
        IMapper mapper,
        IValidator<UpdateSubscriptionCommand> validator,
        ICurrentUserService currentUserService,
        ILogger<UpdateSubscriptionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _validator = validator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<SubscriptionResponse>> Handle(
        UpdateSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Validate
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Result<SubscriptionResponse>.Failure(
                    "Validation failed",
                    ErrorCodeEnum.ValidationFailed,
                    errors);
            }

            // 2. Find subscription
            var repository = _unitOfWork.Repository<Subscription>();
            var subscriptions = await repository.FindAsync(
                x => x.Id == request.Id,
                includes: x => x.Plan);

            var subscription = subscriptions.FirstOrDefault();
            if (subscription == null)
            {
                _logger.LogWarning("Subscription {SubscriptionId} not found for update", request.Id);
                return Result<SubscriptionResponse>.Failure(
                    "Subscription not found",
                    ErrorCodeEnum.NotFound);
            }

            // 3. Update fields if provided
            if (request.Request.SubscriptionStatus.HasValue)
            {
                subscription.SubscriptionStatus = request.Request.SubscriptionStatus.Value;
            }

            if (request.Request.RenewalBehavior.HasValue)
            {
                subscription.RenewalBehavior = request.Request.RenewalBehavior.Value;
            }

            if (request.Request.CancelAtPeriodEnd.HasValue)
            {
                subscription.CancelAtPeriodEnd = request.Request.CancelAtPeriodEnd.Value;
            }

            if (request.Request.CurrentPeriodStart.HasValue)
            {
                subscription.CurrentPeriodStart = request.Request.CurrentPeriodStart.Value;
            }

            if (request.Request.CurrentPeriodEnd.HasValue)
            {
                subscription.CurrentPeriodEnd = request.Request.CurrentPeriodEnd.Value;
            }

            if (request.Request.CancelAt.HasValue)
            {
                subscription.CancelAt = request.Request.CancelAt.Value;
            }

            // 4. Update metadata
            var userId = _currentUserService.UserId != null
                ? Guid.Parse(_currentUserService.UserId)
                : (Guid?)null;
            subscription.UpdateEntity(userId);

            // 5. Publish integration event
            var updateEvent = new SubscriptionUpdatedEvent
            {
                SubscriptionId = subscription.Id,
                UserProfileId = subscription.UserProfileId,
                SubscriptionPlanId = subscription.SubscriptionPlanId,
                PlanName = subscription.Plan.DisplayName,
                SubscriptionStatus = subscription.SubscriptionStatus.ToString(),
                RenewalBehavior = subscription.RenewalBehavior.ToString(),
                CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
                CurrentPeriodEnd = subscription.CurrentPeriodEnd,
                UpdatedBy = userId
            };
            await _unitOfWork.AddOutboxEventAsync(updateEvent);

            // 6. Save changes
            await _unitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

            _logger.LogInformation(
                "Subscription {SubscriptionId} updated successfully by user {UserId}",
                request.Id,
                userId);

            // 7. Return response
            var response = _mapper.Map<SubscriptionResponse>(subscription);
            return Result<SubscriptionResponse>.Success(
                response,
                "Subscription updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription {SubscriptionId}", request.Id);
            return Result<SubscriptionResponse>.Failure(
                "An error occurred while updating the subscription",
                ErrorCodeEnum.InternalError);
        }
    }
}
