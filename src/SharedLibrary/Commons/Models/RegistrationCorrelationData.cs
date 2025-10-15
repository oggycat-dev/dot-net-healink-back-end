using SharedLibrary.Commons.Enums;

namespace SharedLibrary.Commons.Models;

/// <summary>
/// Strongly-typed object for OTP cache correlation data
/// Thay thế anonymous object để tránh JSON deserialize phức tạp
/// </summary>
public class RegistrationCorrelationData
{
    public Guid CorrelationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string EncryptedPassword { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public NotificationChannelEnum Channel { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Constructor for easy creation from RegisterCommand
    /// </summary>
    public RegistrationCorrelationData(Guid correlationId, object originalRequest, string encryptedPassword, NotificationChannelEnum channel)
    {
        CorrelationId = correlationId;
        Channel = channel;
        CreatedAt = DateTime.UtcNow;
        
        // Safely extract properties from original request
        if (originalRequest != null)
        {
            var props = originalRequest.GetType().GetProperties();
            
            Email = props.FirstOrDefault(p => p.Name == "Email")?.GetValue(originalRequest)?.ToString() ?? string.Empty;
            FullName = props.FirstOrDefault(p => p.Name == "FullName")?.GetValue(originalRequest)?.ToString() ?? string.Empty;
            PhoneNumber = props.FirstOrDefault(p => p.Name == "PhoneNumber")?.GetValue(originalRequest)?.ToString() ?? string.Empty;
        }
        
        EncryptedPassword = encryptedPassword;
    }
    
    /// <summary>
    /// Parameterless constructor for JSON deserialization
    /// </summary>
    public RegistrationCorrelationData() { }
}