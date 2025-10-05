using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Commons.Services;
using SharedLibrary.Contracts.Subscription;
using SubscriptionService.Application.Commons.DTOs;
using SubscriptionService.Domain.Entities;

namespace SubscriptionService.Application.Features.SubscriptionPlans.Commands.UpdateSubscriptionPlan;

public class UpdateSubscriptionPlanCommandHandler : IRequestHandler<UpdateSubscriptionPlanCommand, Result>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateSubscriptionPlanCommandHandler> _logger;

    public UpdateSubscriptionPlanCommandHandler(
        IOutboxUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<UpdateSubscriptionPlanCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(
        UpdateSubscriptionPlanCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating subscription plan: {Id}", request.Id);

            var repository = _unitOfWork.Repository<SubscriptionPlan>();

            // Get existing plan directly (use GetFirstOrDefaultAsync for performance)
            var existingPlan = await repository.GetFirstOrDefaultAsync(x => x.Id == request.Id);
            if (existingPlan == null)
            {
                _logger.LogWarning("Subscription plan not found: {Id}", request.Id);
                return Result.Failure(
                    "Subscription plan not found",
                    ErrorCodeEnum.NotFound);
            }

            // Check if new name conflicts with another plan (consistent with Create)
            if (existingPlan.Name != request.Request.Name)
            {
                var nameExists = await repository.AnyAsync(x => x.Name == request.Request.Name && x.Id != request.Id);
                if (nameExists)
                {
                    _logger.LogWarning("Subscription plan name already exists: {Name}", request.Request.Name);
                    return Result.Failure(
                        "Subscription plan with this name already exists",
                        ErrorCodeEnum.ResourceConflict);
                }
            }

            // Update fields from request using AutoMapper
            _mapper.Map(request.Request, existingPlan);
           
            // Update entity metadata - CurrentUserService already validated by middleware
            var userId = Guid.Parse(_currentUserService.UserId!);
            existingPlan.UpdateEntity(userId);

            // Update in repository
            repository.Update(existingPlan);

            // Publish integration event using AutoMapper
            var updateEvent = _mapper.Map<SubscriptionPlanUpdatedEvent>(existingPlan);
            updateEvent = updateEvent with 
            { 
                UpdatedBy = userId,
                // Capture HTTP context for audit trail
                IpAddress = _currentUserService.IpAddress,
                UserAgent = _currentUserService.UserAgent
            };
            
            await _unitOfWork.AddOutboxEventAsync(updateEvent);

            // Save changes
            await _unitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

            _logger.LogInformation("Subscription plan updated successfully: {Id}", existingPlan.Id);

            return Result.Success("Subscription plan updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription plan: {Id}", request.Id);
            return Result.Failure(
                "Failed to update subscription plan",
                ErrorCodeEnum.InternalError);
        }
    }
}
