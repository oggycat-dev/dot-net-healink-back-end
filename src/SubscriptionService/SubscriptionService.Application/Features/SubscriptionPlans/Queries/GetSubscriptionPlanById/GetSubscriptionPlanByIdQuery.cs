using MediatR;
using SubscriptionService.Application.Commons.DTOs;
using SharedLibrary.Commons.Models;

namespace SubscriptionService.Application.Features.SubscriptionPlans.Queries.GetSubscriptionPlanById;

public record GetSubscriptionPlanByIdQuery(Guid Id) : IRequest<Result<SubscriptionPlanResponse>>;
