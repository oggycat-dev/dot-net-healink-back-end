namespace SharedLibrary.Commons.Models.Otp;

public class OtpResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? UserData { get; set; }
    public TimeSpan? ExpiresIn { get; set; }
    public int? RemainingAttempts { get; set; }
}