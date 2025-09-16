namespace ProductAuthMicroservice.Commons.Services;

/// <summary>
/// JWT service interface for token operations
/// </summary>
public interface IJwtService
{
     /// <summary>
    /// Validate JWT token
    /// </summary>
    /// <param name="token">Token to validate</param>
    /// <returns>True if token is valid, false otherwise</returns>
    bool ValidateToken(string token);
    
    /// <summary>
    /// Get user id from token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>User ID or null if token is invalid</returns>
    string? GetUserIdFromToken(string token);
    
    /// <summary>
    /// Get principal claims from token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>ClaimsPrincipal or null if token is invalid</returns>
    System.Security.Claims.ClaimsPrincipal? GetPrincipalFromToken(string token);
}
