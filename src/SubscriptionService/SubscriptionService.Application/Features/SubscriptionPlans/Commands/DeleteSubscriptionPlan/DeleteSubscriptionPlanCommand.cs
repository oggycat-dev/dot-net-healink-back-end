using MediatR;
using SharedLibrary.Commons.Models;

namespace SubscriptionService.Application.Features.SubscriptionPlans.Commands.DeleteSubscriptionPlan;

/// <summary>
/// Command to soft delete a subscription plan
/// </summary>
public record DeleteSubscriptionPlanCommand(Guid Id) : IRequest<Result>;
