namespace UserService.Application.Commons.DTOs;

/// <summary>
/// Request DTO for updating user information
/// Email and PhoneNumber changes trigger sync with AuthService via RPC
/// </summary>
public class UpdateUserInfoRequest
{
    /// <summary>
    /// Full name (optional update)
    /// </summary>
    public string? FullName { get; set; }
    
    /// <summary>
    /// Email (optional update)
    /// If changed, will sync with AuthService via RPC with timeout
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// Phone number (optional update)
    /// If changed, will sync with AuthService via RPC with timeout
    /// </summary>
    public string? PhoneNumber { get; set; }
    
    /// <summary>
    /// Address (optional update)
    /// </summary>
    public string? Address { get; set; }
}
