using MediatR;
using Microsoft.Extensions.Logging;
using ProductAuthMicroservice.Commons.Enums;
using AuthService.Application.Commons.Interfaces;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.Commons.Services;
using ProductAuthMicroservice.Commons.Entities;
using ProductAuthMicroservice.Commons.Cache;
using ProductAuthMicroservice.Shared.Contracts.Events;
using ProductAuthMicroservice.Commons.EventBus;
using ProductAuthMicroservice.Commons.Outbox;

namespace AuthService.Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly ILogger<LogoutCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserStateCache _userStateCache;
    private readonly IOutboxUnitOfWork _unitOfWork;

    public LogoutCommandHandler(
        IIdentityService identityService, 
        ILogger<LogoutCommandHandler> logger, 
        ICurrentUserService currentUserService,
        IUserStateCache userStateCache,
        IOutboxUnitOfWork unitOfWork)
    {
        _identityService = identityService;
        _logger = logger;
        _currentUserService = currentUserService;
        _userStateCache = userStateCache;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                return Result.Failure("Not authorized", ErrorCodeEnum.Unauthorized);
            }
            var result = await _identityService.GetUserByIdAsync(userId);
            if (result == null)
            {
                return Result.Failure("User not found", ErrorCodeEnum.NotFound);
            }
            var user = result;
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            user.UpdateEntity(Guid.Parse(userId));
            await _identityService.UpdateUserAsync(user);
            
            // Remove user state from cache
            await _userStateCache.RemoveUserStateAsync(Guid.Parse(userId));
            
            // Publish logout event
            var logoutEvent = new UserLoggedOutEvent
            {
                UserId = Guid.Parse(userId),
                Email = user.Email ?? string.Empty,
                LogoutAt = DateTime.UtcNow,
                LogoutType = "Manual"
            };
            await _unitOfWork.AddOutboxEventAsync(logoutEvent);
            await _unitOfWork.SaveChangesWithOutboxAsync(cancellationToken);
            
            _logger.LogInformation("User {UserId} logged out successfully", userId);
            return Result.Success("Logout successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging out");
            return Result.Failure("An error occurred while logging out", ErrorCodeEnum.InternalError);
        }
    }
}