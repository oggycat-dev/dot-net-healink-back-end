using MediatR;
using SharedLibrary.Commons.Models;
using SubscriptionService.Application.Commons.DTOs;

namespace SubscriptionService.Application.Features.Subscriptions.Queries.GetSubscriptions;

/// <summary>
/// Query to get paginated subscriptions with filters
/// </summary>
public record GetSubscriptionsQuery(SubscriptionFilter Filter) : IRequest<PaginationResult<SubscriptionResponse>>;
