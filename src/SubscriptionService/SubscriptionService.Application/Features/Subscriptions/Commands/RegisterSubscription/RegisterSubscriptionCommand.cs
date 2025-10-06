using MediatR;
using SharedLibrary.Commons.Models;

namespace SubscriptionService.Application.Features.Subscriptions.Commands.RegisterSubscription;

public record RegisterSubscriptionCommand(Guid SubscriptionPlanId) : IRequest<Result>;