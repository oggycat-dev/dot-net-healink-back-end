using MediatR;
using SharedLibrary.Commons.Models;
using SubscriptionService.Application.Commons.DTOs;

namespace SubscriptionService.Application.Features.Subscriptions.Queries.GetSubscriptionById;

/// <summary>
/// Query to get a single subscription by ID
/// </summary>
public record GetSubscriptionByIdQuery(Guid Id) : IRequest<Result<SubscriptionResponse>>;
