using MediatR;
using SubscriptionService.Application.Commons.DTOs;
using SharedLibrary.Commons.Models;

namespace SubscriptionService.Application.Features.SubscriptionPlans.Commands.CreateSubscriptionPlan;

public record CreateSubscriptionPlanCommand(SubscriptionPlanRequest Request) : IRequest<Result>;
