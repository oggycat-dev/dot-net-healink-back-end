using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Repositories;
using SubscriptionService.Application.Commons.DTOs;
using SubscriptionService.Domain.Entities;

namespace SubscriptionService.Application.Features.Subscriptions.Queries.GetSubscriptionById;

public class GetSubscriptionByIdQueryHandler : IRequestHandler<GetSubscriptionByIdQuery, Result<SubscriptionResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetSubscriptionByIdQueryHandler> _logger;

    public GetSubscriptionByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetSubscriptionByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<SubscriptionResponse>> Handle(
        GetSubscriptionByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var repository = _unitOfWork.Repository<Subscription>();

            // Find subscription with Plan included
            var subscription = await repository.FindAsync(
                x => x.Id == request.Id,
                includes: x => x.Plan);

            var existingSubscription = subscription.FirstOrDefault();

            if (existingSubscription == null)
            {
                _logger.LogWarning("Subscription with ID {SubscriptionId} not found", request.Id);
                return Result<SubscriptionResponse>.Failure(
                    "Subscription not found",
                    ErrorCodeEnum.NotFound);
            }

            var response = _mapper.Map<SubscriptionResponse>(existingSubscription);

            _logger.LogInformation(
                "Retrieved subscription {SubscriptionId} for user {UserId}",
                request.Id,
                existingSubscription.UserProfileId);

            return Result<SubscriptionResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription by ID {SubscriptionId}", request.Id);
            return Result<SubscriptionResponse>.Failure(
                "An error occurred while retrieving the subscription",
                ErrorCodeEnum.InternalError);
        }
    }
}
