using MediatR;
using SharedLibrary.Commons.Models;
using SubscriptionService.Application.Commons.DTOs;

namespace SubscriptionService.Application.Features.Subscriptions.Commands.UpdateSubscription;

/// <summary>
/// Command to update subscription settings
/// </summary>
public record UpdateSubscriptionCommand(Guid Id, UpdateSubscriptionRequest Request)
    : IRequest<Result>;
