using AutoMapper;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Commons.Services;
using SharedLibrary.Contracts.Subscription.Events;
using SubscriptionService.Domain.Entities;

namespace SubscriptionService.Application.Features.Subscriptions.Commands.UpgradeSubscription;

/// <summary>
/// Handler for upgrading/downgrading subscription to a different plan
/// Flow:
/// 1. Validate new plan exists and is different from current
/// 2. Cancel current subscription (immediately or at period end)
/// 3. Calculate proration credit if applicable
/// 4. Create new subscription with proration applied
/// 5. Trigger payment for difference (if upgrade) or issue credit (if downgrade)
/// </summary>
public class UpgradeSubscriptionCommandHandler : IRequestHandler<UpgradeSubscriptionCommand, Result>
{
    private readonly IOutboxUnitOfWork _outboxUnitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IMapper _mapper;
    private readonly ILogger<UpgradeSubscriptionCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    
    public UpgradeSubscriptionCommandHandler(
        IOutboxUnitOfWork outboxUnitOfWork,
        IPublishEndpoint publishEndpoint,
        IMapper mapper,
        ILogger<UpgradeSubscriptionCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _outboxUnitOfWork = outboxUnitOfWork;
        _publishEndpoint = publishEndpoint;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
    }
    
    public async Task<Result> Handle(UpgradeSubscriptionCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var userIdStr = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Result.Failure("User not authenticated", ErrorCodeEnum.Unauthorized);
            }

            // 1. Get current active subscription
            var currentSubscription = await _outboxUnitOfWork.Repository<Subscription>()
                .GetFirstOrDefaultAsync(
                    s => s.UserProfileId == userId && 
                         s.SubscriptionStatus == Domain.Enums.SubscriptionStatus.Active,
                    s => s.Plan);
            
            if (currentSubscription == null)
            {
                return Result.Failure(
                    "No active subscription found. Please subscribe to a plan first.", 
                    ErrorCodeEnum.NotFound);
            }

            // 2. Validate new plan
            var newPlan = await _outboxUnitOfWork.Repository<SubscriptionPlan>()
                .GetFirstOrDefaultAsync(
                    p => p.Id == command.NewSubscriptionPlanId && 
                         p.Status == EntityStatusEnum.Active);
            
            if (newPlan == null)
            {
                return Result.Failure(
                    "New subscription plan not found or inactive", 
                    ErrorCodeEnum.NotFound);
            }

            // 3. Check if it's the same plan
            if (currentSubscription.SubscriptionPlanId == command.NewSubscriptionPlanId)
            {
                return Result.Failure(
                    $"You are already subscribed to the '{newPlan.DisplayName}' plan", 
                    ErrorCodeEnum.DuplicateEntry);
            }

            // 4. Determine upgrade vs downgrade
            var isUpgrade = newPlan.Amount > currentSubscription.Plan.Amount;
            var priceDifference = newPlan.Amount - currentSubscription.Plan.Amount;

            _logger.LogInformation(
                "User {UserId} is {Action} from '{OldPlan}' ({OldAmount}) to '{NewPlan}' ({NewAmount})",
                userId, 
                isUpgrade ? "upgrading" : "downgrading",
                currentSubscription.Plan.DisplayName,
                currentSubscription.Plan.Amount,
                newPlan.DisplayName,
                newPlan.Amount);

            // 5. Calculate proration credit (if applicable)
            decimal prorationCredit = 0;
            if (command.ApplyProration && currentSubscription.CurrentPeriodEnd.HasValue)
            {
                var remainingDays = (currentSubscription.CurrentPeriodEnd.Value - DateTime.UtcNow).Days;
                var totalPeriodDays = (currentSubscription.CurrentPeriodEnd.Value - 
                                      currentSubscription.CurrentPeriodStart!.Value).Days;
                
                if (remainingDays > 0 && totalPeriodDays > 0)
                {
                    prorationCredit = (currentSubscription.Plan.Amount * remainingDays) / totalPeriodDays;
                    _logger.LogInformation(
                        "Proration credit calculated: {Credit} ({RemainingDays}/{TotalDays} days)",
                        prorationCredit, remainingDays, totalPeriodDays);
                }
            }

            // 6. Cancel current subscription
            currentSubscription.SubscriptionStatus = command.CancelAtPeriodEnd 
                ? Domain.Enums.SubscriptionStatus.Active  // Keep active until period end
                : Domain.Enums.SubscriptionStatus.Canceled;
            
            if (command.CancelAtPeriodEnd)
            {
                currentSubscription.CancelAtPeriodEnd = true;
                currentSubscription.CanceledAt = currentSubscription.CurrentPeriodEnd;
            }
            else
            {
                currentSubscription.CanceledAt = DateTime.UtcNow;
            }
            
            currentSubscription.UpdateEntity();
            _outboxUnitOfWork.Repository<Subscription>().Update(currentSubscription);

            // 7. Create new subscription
            var newSubscription = new Subscription
            {
                UserProfileId = userId,
                SubscriptionPlanId = newPlan.Id,
                SubscriptionStatus = Domain.Enums.SubscriptionStatus.Pending, // Pending payment
                RenewalBehavior = currentSubscription.RenewalBehavior, // Inherit renewal behavior
                CancelAtPeriodEnd = false,
                Plan = newPlan
            };
            newSubscription.InitializeEntity(userId);

            await _outboxUnitOfWork.Repository<Subscription>().AddAsync(newSubscription);

            // 8. Calculate amount to charge (considering proration)
            var amountToCharge = Math.Max(0, newPlan.Amount - prorationCredit);

            // 9. Publish SubscriptionUpgradeStarted event (triggers saga)
            var upgradeEvent = new SubscriptionUpgradeStarted
            {
                NewSubscriptionId = newSubscription.Id,
                OldSubscriptionId = currentSubscription.Id,
                UserProfileId = userId,
                NewPlanId = newPlan.Id,
                NewPlanName = newPlan.Name,
                OldPlanId = currentSubscription.SubscriptionPlanId,
                OldPlanName = currentSubscription.Plan.Name,
                IsUpgrade = isUpgrade,
                OriginalAmount = newPlan.Amount,
                ProrationCredit = prorationCredit,
                AmountToCharge = amountToCharge,
                Currency = newPlan.Currency,
                IpAddress = _currentUserService.IpAddress,
                UserAgent = _currentUserService.UserAgent
            };
            
            await _publishEndpoint.Publish(upgradeEvent, cancellationToken);

            // 10. Atomic commit
            await _outboxUnitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Subscription {Action} initiated: OldSubscription={OldId}, NewSubscription={NewId}, AmountToCharge={Amount}",
                isUpgrade ? "upgrade" : "downgrade",
                currentSubscription.Id,
                newSubscription.Id,
                amountToCharge);

            return Result.Success(
                $"Subscription {(isUpgrade ? "upgrade" : "downgrade")} initiated successfully. " +
                (amountToCharge > 0 
                    ? $"You will be charged {amountToCharge:N2} {newPlan.Currency}." 
                    : $"You have received a credit of {prorationCredit:N2} {newPlan.Currency}.")
                );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upgrading subscription");
            return Result.Failure("An error occurred while upgrading subscription", ErrorCodeEnum.InternalError);
        }
    }
}

