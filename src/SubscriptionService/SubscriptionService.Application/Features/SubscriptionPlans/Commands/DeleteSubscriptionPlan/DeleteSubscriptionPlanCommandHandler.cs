using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Services;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Contracts.Subscription;
using SubscriptionService.Domain.Entities;
using SubscriptionService.Domain.Enums;
using SharedLibrary.Commons.Entities;

namespace SubscriptionService.Application.Features.SubscriptionPlans.Commands.DeleteSubscriptionPlan;

public class DeleteSubscriptionPlanCommandHandler : IRequestHandler<DeleteSubscriptionPlanCommand, Result>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteSubscriptionPlanCommandHandler> _logger;

    public DeleteSubscriptionPlanCommandHandler(
        IOutboxUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<DeleteSubscriptionPlanCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeleteSubscriptionPlanCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var repository = _unitOfWork.Repository<SubscriptionPlan>();

            // Find the plan directly (use GetFirstOrDefaultAsync for performance)
            var existingPlan = await repository.GetFirstOrDefaultAsync(x => x.Id == request.Id);
            if (existingPlan == null)
            {
                _logger.LogWarning(
                    "Subscription plan with ID {PlanId} not found for deletion",
                    request.Id);

                return Result.Failure(
                    "Subscription plan not found",
                    ErrorCodeEnum.NotFound);
            }

            // Check if plan has any active subscriptions (use AnyAsync for performance)
            var subscriptionRepository = _unitOfWork.Repository<Subscription>();
            var hasActiveSubscriptions = await subscriptionRepository.AnyAsync(
                x => x.SubscriptionPlanId == request.Id &&
                     x.SubscriptionStatus == SubscriptionStatus.Active);

            if (hasActiveSubscriptions)
            {
                _logger.LogWarning(
                    "Cannot delete subscription plan {PlanId} - has active subscriptions",
                    request.Id);

                return Result.Failure(
                    "Cannot delete subscription plan. It has active subscriptions. Please cancel them first.",
                    ErrorCodeEnum.ValidationFailed);
            }

            // Soft delete the plan - CurrentUserService already validated by middleware
            var userId = Guid.Parse(_currentUserService.UserId!);
            existingPlan.SoftDeleteEnitity(userId);
            repository.Update(existingPlan);

            // Publish integration event using AutoMapper
            var deleteEvent = _mapper.Map<SubscriptionPlanDeletedEvent>(existingPlan);
            deleteEvent = deleteEvent with 
            { 
                DeletedBy = userId, 
                DeletedAt = DateTime.UtcNow,
                // Capture HTTP context for audit trail
                IpAddress = _currentUserService.IpAddress,
                UserAgent = _currentUserService.UserAgent
            };
            
            await _unitOfWork.AddOutboxEventAsync(deleteEvent);

            // Save changes
            await _unitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

            _logger.LogInformation(
                "Subscription plan {PlanId} soft deleted successfully by user {UserId}",
                request.Id,
                userId);

            return Result.Success("Subscription plan deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting subscription plan {PlanId}",
                request.Id);

            return Result.Failure(
                "An error occurred while deleting the subscription plan",
                ErrorCodeEnum.InternalError);
        }
    }
}
