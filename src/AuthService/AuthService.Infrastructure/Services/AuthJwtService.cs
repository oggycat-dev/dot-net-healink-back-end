using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthService.Application.Commons.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProductAuthMicroservice.AuthService.Domain.Entities;
using ProductAuthMicroservice.Commons.Configs;
using ProductAuthMicroservice.Commons.Services;

namespace AuthService.Infrastructure.Services;

public class AuthJwtService : JwtService, IAuthJwtService
{
     private readonly JwtConfig _jwtConfig;
    private readonly UserManager<AppUser> _userManager;

    public AuthJwtService(IOptions<JwtConfig> jwtConfig, UserManager<AppUser> userManager) : base(jwtConfig)
    {
        _jwtConfig = jwtConfig.Value;
        _userManager = userManager;
    }

    /// <summary>
    /// Generate JWT token for a user
    /// </summary>
    public (string token, List<string> roles) GenerateJwtToken(AppUser user, string? requestOrigin = null)
    {
        // Get user claims and roles
        var userRoles = _userManager.GetRolesAsync(user).Result.ToList();

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        // Add role claims
        claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        // Create signing credentials
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Create token
        var token = new JwtSecurityToken(
            issuer: _jwtConfig.Issuer,
            audience: _jwtConfig.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtConfig.ExpireInMinutes),
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), userRoles ?? new List<string>());
    }

    /// <summary>
    /// Generate JWT token with expiration info for a user
    /// </summary>
    public (string token, List<string> roles, int expiresInMinutes, DateTime expiresAt) GenerateJwtTokenWithExpiration(AppUser user, string? requestOrigin = null)
    {
        var (token, roles) = GenerateJwtToken(user, requestOrigin);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtConfig.ExpireInMinutes);
        return (token, roles, _jwtConfig.ExpireInMinutes, expiresAt);
    }


    /// <summary>
    /// Generate refresh token
    /// </summary>
    public (string refreshToken, DateTime refreshTokenExpiryTime) GenerateRefreshTokenWithExpiration()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return (Convert.ToBase64String(randomNumber), DateTime.UtcNow.AddDays(_jwtConfig.RefreshTokenExpireInDays));
    }

}