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

namespace SubscriptionService.Application.Features.SubscriptionPlans.Commands.CreateSubscriptionPlan;

public class CreateSubscriptionPlanCommandHandler : IRequestHandler<CreateSubscriptionPlanCommand, Result>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateSubscriptionPlanCommandHandler> _logger;

    public CreateSubscriptionPlanCommandHandler(
        IOutboxUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<CreateSubscriptionPlanCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(
        CreateSubscriptionPlanCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating subscription plan: {@Request}", request.Request);

            var repository = _unitOfWork.Repository<SubscriptionPlan>();

            // Check if plan name already exists (use AnyAsync for performance)
            var nameExists = await repository.AnyAsync(x => x.Name == request.Request.Name);
            if (nameExists)
            {
                _logger.LogWarning("Subscription plan name already exists: {Name}", request.Request.Name);
                return Result.Failure(
                    "Subscription plan with this name already exists",
                    ErrorCodeEnum.ResourceConflict);
            }

            // Map to entity
            var plan = _mapper.Map<SubscriptionPlan>(request.Request);
            
            // Initialize entity - CurrentUserService already validated by AuthorizeRoles middleware
            var currentUserID = Guid.Parse(_currentUserService.UserId!);
            plan.InitializeEntity(currentUserID);

            // Add to repository
            await repository.AddAsync(plan);

            // Publish integration event using AutoMapper
            var createEvent = _mapper.Map<SubscriptionPlanCreatedEvent>(plan);
            createEvent = createEvent with 
            { 
                CreatedBy = currentUserID,
                // Capture HTTP context for audit trail
                IpAddress = _currentUserService.IpAddress,
                UserAgent = _currentUserService.UserAgent
            };
            
            await _unitOfWork.AddOutboxEventAsync(createEvent);

            // Save changes
            await _unitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

            _logger.LogInformation("Subscription plan created successfully: {Id}", plan.Id);

            return Result.Success("Subscription plan created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription plan");
            return Result.Failure(
                "Failed to create subscription plan",
                ErrorCodeEnum.InternalError);
        }
    }
}
