using MediatR;
using SubscriptionService.Application.Commons.DTOs;
using SharedLibrary.Commons.Models;

namespace SubscriptionService.Application.Features.SubscriptionPlans.Commands.UpdateSubscriptionPlan;

public record UpdateSubscriptionPlanCommand(Guid Id, SubscriptionPlanRequest Request) : IRequest<Result>;
