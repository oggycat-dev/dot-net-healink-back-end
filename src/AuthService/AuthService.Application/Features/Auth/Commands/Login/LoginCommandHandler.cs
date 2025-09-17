using AuthService.Application.Commons.DTOs;
using AuthService.Application.Commons.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Cache;
using SharedLibrary.SharedLibrary.Contracts.Events;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Commons.Entities;


namespace AuthService.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IIdentityService _identityService;
    private readonly IAuthJwtService _jwtService;
    private readonly ILogger<LoginCommandHandler> _logger;
    private readonly IUserStateCache _userStateCache;
    private readonly IOutboxUnitOfWork _unitOfWork;

    public LoginCommandHandler(
        IIdentityService identityService, 
        IAuthJwtService jwtService, 
        ILogger<LoginCommandHandler> logger,
        IUserStateCache userStateCache,
        IOutboxUnitOfWork unitOfWork)
    {
        _identityService = identityService;
        _jwtService = jwtService;
        _logger = logger;
        _userStateCache = userStateCache;
        _unitOfWork = unitOfWork;
    }
    public async Task<Result<AuthResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _identityService.AuthenticateAsync(command.Request);
            if (!result.IsSuccess)
            {
                return Result<AuthResponse>.Failure(result.Message?? "Invalid credentials", ErrorCodeEnum.InvalidCredentials);
            }

            var user = result.Data!;
            
            //generate refresh token and update auth infor of user
            var (refreshToken, refreshTokenExpiryTime) = _jwtService.GenerateRefreshTokenWithExpiration();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = refreshTokenExpiryTime;
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdateEntity(user.Id);
            await _identityService.UpdateUserAsync(user);
            
            //generate jwt token
            var (token, roles, expiresInMinutes, expiresAt) = _jwtService.GenerateJwtTokenWithExpiration(user);
            
            // Cache user state for distributed auth
            var userState = new UserStateInfo
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                Roles = roles,
                Status = user.Status,
                RefreshToken = refreshToken,
                RefreshTokenExpiryTime = refreshTokenExpiryTime,
                LastLoginAt = user.LastLoginAt ?? DateTime.UtcNow
            };
            await _userStateCache.SetUserStateAsync(userState);
            
            // Publish login event
            var loginEvent = new UserLoggedInEvent
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                Roles = roles,
                RefreshToken = refreshToken,
                RefreshTokenExpiryTime = refreshTokenExpiryTime,
                LoginAt = user.LastLoginAt ?? DateTime.UtcNow,
                UserAgent = command.Request.UserAgent,
                IpAddress = command.Request.IpAddress
            };
            await _unitOfWork.AddOutboxEventAsync(loginEvent);
            await _unitOfWork.SaveChangesWithOutboxAsync(cancellationToken);
            
            var authResponse = new AuthResponse
            {
                AccessToken = token,
                Roles = roles,
                ExpiresAt = expiresAt
            };
            
            _logger.LogInformation("User {UserId} logged in successfully", user.Id);
            return Result<AuthResponse>.Success(authResponse, "Login successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in");
            return Result<AuthResponse>.Failure("An error occurred while logging in", ErrorCodeEnum.InternalError);
        }
    }
}