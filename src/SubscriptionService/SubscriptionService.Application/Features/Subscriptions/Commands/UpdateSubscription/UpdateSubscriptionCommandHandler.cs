using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Services;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Contracts.Subscription;
using SubscriptionService.Domain.Entities;

namespace SubscriptionService.Application.Features.Subscriptions.Commands.UpdateSubscription;

public class UpdateSubscriptionCommandHandler : IRequestHandler<UpdateSubscriptionCommand, Result>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateSubscriptionCommandHandler> _logger;

    public UpdateSubscriptionCommandHandler(
        IOutboxUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<UpdateSubscriptionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;

        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(
        UpdateSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 2. Find subscription
            var repository = _unitOfWork.Repository<Subscription>();
            var subscription = await repository.GetFirstOrDefaultAsync(
                x => x.Id == request.Id,
                x => x.Plan);
            if (subscription == null)
            {
                _logger.LogWarning("Subscription {SubscriptionId} not found for update", request.Id);
                return Result.Failure(
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

            // 4. Update metadata - CurrentUserService already validated by middleware
            var userId = Guid.Parse(_currentUserService.UserId!);
            subscription.UpdateEntity(userId);

            // 5. Publish integration event using AutoMapper
            var updateEvent = _mapper.Map<SubscriptionUpdatedEvent>(subscription);
            updateEvent = updateEvent with 
            { 
                UpdatedBy = userId,
                // Capture HTTP context for audit trail
                IpAddress = _currentUserService.IpAddress,
                UserAgent = _currentUserService.UserAgent
            };
            await _unitOfWork.AddOutboxEventAsync(updateEvent);

            // 6. Save changes
            await _unitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

            _logger.LogInformation(
                "Subscription {SubscriptionId} updated successfully by user {UserId}",
                request.Id,
                userId);
            return Result.Success("Subscription updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription {SubscriptionId}", request.Id);
            return Result.Failure(
                "An error occurred while updating the subscription",
                ErrorCodeEnum.InternalError);
        }
    }
}
