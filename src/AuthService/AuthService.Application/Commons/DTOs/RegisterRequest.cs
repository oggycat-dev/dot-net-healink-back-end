using System.Text.Json.Serialization;
using SharedLibrary.Commons.Enums;

namespace AuthService.Application.Commons.DTOs;

public class RegisterRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
    [JsonPropertyName("confirm_password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    // [JsonPropertyName("grantType")]
    // public GrantTypeEnum GrantType { get; set; } = GrantTypeEnum.Password;

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;
    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;
    // [JsonPropertyName("gender")]
    // public GenderEnum? Gender { get; set; }
    // [JsonPropertyName("dateOfBirth")]
    // public DateTime? DateOfBirth { get; set; }

    [JsonPropertyName("otp_sent_channel")]
    public NotificationChannelEnum? OtpSentChannel { get; set; } = NotificationChannelEnum.Email;
}