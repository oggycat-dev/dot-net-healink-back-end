using SharedLibrary.Commons.Enums;

namespace AuthService.Application.Features.Auth.Models;

/// <summary>
/// Request model for user registration
/// </summary>
public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public NotificationChannelEnum Channel { get; set; } = NotificationChannelEnum.Email;
}

/// <summary>
/// Response model for registration
/// </summary>
public class RegisterResponse
{
    public Guid CorrelationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; }
}