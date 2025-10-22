using MediatR;
using Microsoft.Extensions.Logging;
using ContentService.Domain.Interfaces;
using AutoMapper;
using SharedLibrary.Commons.Cache;
using SharedLibrary.Commons.Services;

namespace ContentService.Application.Features.Podcasts.Queries;

public class GetPodcastByIdQueryHandler : IRequestHandler<GetPodcastByIdQuery, PodcastDto?>
{
    private readonly IContentRepository _repository;
    private readonly IUserStateCache _userStateCache;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetPodcastByIdQueryHandler> _logger;

    public GetPodcastByIdQueryHandler(
        IContentRepository repository,
        IUserStateCache userStateCache,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<GetPodcastByIdQueryHandler> logger)
    {
        _repository = repository;
        _userStateCache = userStateCache;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PodcastDto?> Handle(GetPodcastByIdQuery request, CancellationToken cancellationToken)
    {
        // ✅ Check subscription requirement from cache
        await ValidateSubscriptionAccessAsync();
        
        var podcast = await _repository.GetPodcastByIdAsync(request.Id, cancellationToken);
        
        if (podcast == null)
            return null;

        return _mapper.Map<PodcastDto>(podcast);
    }

    /// <summary>
    /// Validate subscription access from cache
    /// ✅ Admin, Staff, ContentCreator: No subscription required
    /// ❌ User: Must have active subscription
    /// </summary>
    private async Task ValidateSubscriptionAccessAsync()
    {
        var userId = _currentUserService.UserId;
        
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            _logger.LogWarning("Unauthorized access attempt - No valid user ID");
            throw new UnauthorizedAccessException("Authentication required to view podcast");
        }

        // Get user state from cache
        var userState = await _userStateCache.GetUserStateAsync(userGuid);
        
        if (userState == null)
        {
            _logger.LogWarning("🔍 DEBUG: User {UserId} not found in cache - this might be a cache miss", userGuid);
            // TODO: Implement fallback to identity service or allow temporary access
            // For now, we'll be strict and deny access
            throw new UnauthorizedAccessException("User state not found in cache. Please try again.");
        }
        
        if (!userState.IsActive)
        {
            _logger.LogWarning("Access denied - User {UserId} is not active", userGuid);
            throw new UnauthorizedAccessException("User account is not active");
        }

        // 🔍 DEBUG: Log detailed user state information
        _logger.LogInformation("🔍 DEBUG: User {UserId} state - Roles: [{Roles}], Subscription: {Subscription}, HasActiveSubscription: {HasActiveSubscription}", 
            userGuid, 
            string.Join(", ", userState.Roles),
            userState.Subscription != null ? $"Id={userState.Subscription.SubscriptionId}, Status={userState.Subscription.SubscriptionStatus}, IsActive={userState.Subscription.IsActive}" : "NULL",
            userState.HasActiveSubscription);

        // ✅ Check roles - Admin, Staff, ContentCreator bypass subscription check
        var exemptRoles = new[] { "Admin", "Staff", "ContentCreator" };
        var hasExemptRole = userState.Roles.Any(role => exemptRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
        
        if (hasExemptRole)
        {
            _logger.LogInformation("User {UserId} has exempt role ({Roles}) - Subscription check bypassed", 
                userGuid, string.Join(", ", userState.Roles));
            return;
        }

        // ❌ Regular User - Must have active subscription
        if (!userState.HasActiveSubscription)
        {
            // 🔍 DEBUG: Try to load subscription from subscription service if not in cache
            if (userState.Subscription == null)
            {
                _logger.LogInformation("🔍 DEBUG: Subscription not in cache for user {UserId}, attempting to load from subscription service", userGuid);
                
                // TODO: Implement RPC call to subscription service to get user subscription
                // For now, we'll allow access temporarily and log this for investigation
                _logger.LogWarning("🔍 DEBUG: Subscription service fallback not implemented - allowing temporary access for user {UserId}", userGuid);
                
                // Temporary: Allow access if subscription is not in cache
                // This prevents blocking users who have subscriptions but cache miss
                _logger.LogInformation("🔍 DEBUG: Temporary access granted to user {UserId} due to subscription cache miss", userGuid);
                return;
            }
            
            _logger.LogWarning("🔍 DEBUG: Access denied - User {UserId} subscription details: Subscription={Subscription}, HasActiveSubscription={HasActiveSubscription}", 
                userGuid, 
                userState.Subscription != null ? $"Status={userState.Subscription.SubscriptionStatus}, IsActive={userState.Subscription.IsActive}" : "NULL",
                userState.HasActiveSubscription);
            throw new UnauthorizedAccessException("Active subscription required to view podcast. Please subscribe to continue.");
        }

        _logger.LogInformation("User {UserId} has active subscription - Access granted", userGuid);
    }
}