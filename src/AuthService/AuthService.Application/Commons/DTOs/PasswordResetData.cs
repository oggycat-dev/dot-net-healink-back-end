namespace AuthService.Application.Commons.DTOs;

public class PasswordResetData
{
    public string? EncryptedPassword { get; set; }
    public string? ResetToken { get; set; }
}