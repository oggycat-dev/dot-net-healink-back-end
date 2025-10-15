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
        
        if (userState == null || !userState.IsActive)
        {
            _logger.LogWarning("Access denied - User {UserId} is not active or not found in cache", userGuid);
            throw new UnauthorizedAccessException("User account is not active");
        }

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
            _logger.LogWarning("Access denied - User {UserId} does not have active subscription", userGuid);
            throw new UnauthorizedAccessException("Active subscription required to view podcast. Please subscribe to continue.");
        }

        _logger.LogInformation("User {UserId} has active subscription - Access granted", userGuid);
    }
}