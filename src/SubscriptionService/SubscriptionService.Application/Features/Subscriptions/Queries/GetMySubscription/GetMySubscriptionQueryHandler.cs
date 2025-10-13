using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Cache;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Repositories;
using SharedLibrary.Commons.Services;
using SubscriptionService.Application.Commons.DTOs;
using SubscriptionService.Application.Commons.QueryBuilders;
using SubscriptionService.Domain.Entities;
using SubscriptionService.Domain.Enums;

namespace SubscriptionService.Application.Features.Subscriptions.Queries.GetMySubscription;

public class GetMySubscriptionQueryHandler : IRequestHandler<GetMySubscriptionQuery, Result<SubscriptionResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserStateCache _userStateCache;
    private readonly ILogger<GetMySubscriptionQueryHandler> _logger;

    public GetMySubscriptionQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        IUserStateCache userStateCache,
        ILogger<GetMySubscriptionQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _userStateCache = userStateCache;
        _logger = logger;
    }

    public async Task<Result<SubscriptionResponse>> Handle(
        GetMySubscriptionQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // ✅ Step 1: Get UserId from JWT (authentication)
            var userIdStr = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var authUserId))
            {
                return Result<SubscriptionResponse>.Failure(
                    "User not authenticated",
                    ErrorCodeEnum.Unauthorized);
            }

            // ✅ Step 2: Get UserProfileId from cache (business logic)
            var userState = await _userStateCache.GetUserStateAsync(authUserId);
            if (userState == null)
            {
                _logger.LogWarning("User state not found in cache for UserId={UserId}", authUserId);
                return Result<SubscriptionResponse>.Failure(
                    "User session not found. Please login again.",
                    ErrorCodeEnum.Unauthorized);
            }

            if (!userState.IsActive)
            {
                _logger.LogWarning("User {UserId} is inactive. Status={Status}", authUserId, userState.Status);
                return Result<SubscriptionResponse>.Failure(
                    "User account is inactive",
                    ErrorCodeEnum.Forbidden);
            }

            var userProfileId = userState.UserProfileId;
            if (userProfileId == Guid.Empty)
            {
                _logger.LogError("UserProfileId is empty for UserId={UserId}", authUserId);
                return Result<SubscriptionResponse>.Failure(
                    "User profile not found. Please contact support.",
                    ErrorCodeEnum.NotFound);
            }

            _logger.LogInformation(
                "Getting subscription for AuthUserId={AuthUserId}, UserProfileId={UserProfileId}",
                authUserId, userProfileId);

            // ✅ Step 3: Get active subscription by UserProfileId
            var repository = _unitOfWork.Repository<Subscription>();
            var subscription = await repository.GetFirstOrDefaultAsync(
                predicate: s => s.UserProfileId == userProfileId &&
                               s.SubscriptionStatus == SubscriptionStatus.Active,
                includes: x => x.Plan);

            if (subscription == null)
            {
                _logger.LogInformation(
                    "No active subscription found for UserProfileId={UserProfileId}",
                    userProfileId);
                return Result<SubscriptionResponse>.Failure(
                    "You don't have an active subscription",
                    ErrorCodeEnum.NotFound);
            }

            var response = _mapper.Map<SubscriptionResponse>(subscription);

            _logger.LogInformation(
                "Successfully retrieved subscription {SubscriptionId} for UserProfileId={UserProfileId}",
                subscription.Id, userProfileId);

            return Result<SubscriptionResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user subscription");
            return Result<SubscriptionResponse>.Failure(
                "An error occurred while retrieving your subscription",
                ErrorCodeEnum.InternalError);
        }
    }
}

    