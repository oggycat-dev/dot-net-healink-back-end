using MediatR;
using SharedLibrary.Commons.Models;
using SubscriptionService.Application.Commons.DTOs;

namespace SubscriptionService.Application.Features.Subscriptions.Commands.RegisterSubscription;

/// <summary>
/// Command returns payment intent data for frontend redirect
/// </summary>
public record RegisterSubscriptionCommand(RegisterSubscriptionRequest Request) : IRequest<Result<object>>;