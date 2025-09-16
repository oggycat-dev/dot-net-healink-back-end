using ProductAuthMicroservice.AuthService.Domain.Entities;
using ProductAuthMicroservice.Commons.Services;

namespace AuthService.Application.Commons.Interfaces;

public interface IAuthJwtService : IJwtService
{
    /// <summary>
    /// Generate JWT token for a user
    /// </summary>
    /// <param name="user">User for which to generate the token</param>
    /// <param name="requestOrigin">Optional request origin for audience validation</param>
    /// <returns>Token and roles tuple</returns>
    (string token, List<string> roles) GenerateJwtToken(AppUser user, string? requestOrigin = null);
    
    /// <summary>
    /// Generate JWT token with expiration info for a user
    /// </summary>
    /// <param name="user">User for which to generate the token</param>
    /// <param name="requestOrigin">Optional request origin for audience validation</param>
    /// <returns>Token, roles, expiration time in minutes, and expiration datetime tuple</returns>
    (string token, List<string> roles, int expiresInMinutes, DateTime expiresAt) GenerateJwtTokenWithExpiration(AppUser user, string? requestOrigin = null);
    
    /// <summary>
    /// Get token expiration time in minutes
    /// </summary>
    /// <returns>Token expiration time in minutes</returns>
    
    /// <summary>
    /// Generate refresh token for a user
    /// </summary>
    /// <returns>Refresh token string and expiration time</returns>
    (string refreshToken, DateTime refreshTokenExpiryTime) GenerateRefreshTokenWithExpiration();
}