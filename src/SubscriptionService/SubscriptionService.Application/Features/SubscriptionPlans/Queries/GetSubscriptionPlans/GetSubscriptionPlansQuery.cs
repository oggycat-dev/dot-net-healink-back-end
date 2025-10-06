using MediatR;
using SubscriptionService.Application.Commons.DTOs;
using SharedLibrary.Commons.Models;

namespace SubscriptionService.Application.Features.SubscriptionPlans.Queries.GetSubscriptionPlans;

public record GetSubscriptionPlansQuery(SubscriptionPlanFilter Filter) : IRequest<PaginationResult<SubscriptionPlanResponse>>;
