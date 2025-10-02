using MediatR;
using SharedLibrary.Commons.Models;
using SubscriptionService.Application.Commons.DTOs;

namespace SubscriptionService.Application.Features.Subscriptions.Commands.CancelSubscription;

/// <summary>
/// Command to cancel a subscription
/// </summary>
public record CancelSubscriptionCommand(
    Guid Id,
    bool CancelAtPeriodEnd = true,
    string? Reason = null) : IRequest<Result<SubscriptionResponse>>;
