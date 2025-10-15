using MediatR;
using SharedLibrary.Commons.Models;
using SubscriptionService.Application.Commons.DTOs;

namespace SubscriptionService.Application.Features.Subscriptions.Queries.GetMySubscription;

/// <summary>
/// Query to get current user's subscription (uses UserProfileId from cache)
/// </summary>
public record GetMySubscriptionQuery : IRequest<Result<SubscriptionResponse>>;

