using NotificationService.Application.Commons.Enums;
using SharedLibrary.Commons.Enums;

namespace NotificationService.Application.Commons.Models;

public class NotificationRequest
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? HtmlContent { get; set; } // For email templates
    public NotificationTemplateEnums Template { get; set; } = NotificationTemplateEnums.Welcome;
    public Dictionary<string, object> TemplateData { get; set; } = new();
    public NotificationPriorityEnum Priority { get; set; } = NotificationPriorityEnum.Normal;
    public DateTime? ScheduledAt { get; set; }
    public List<string> AttachmentUrls { get; set; } = new();
    public Dictionary<string, string>? Metadata { get; set; }
}

public class NotificationSendResult
{
    public Guid UserId { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public List<NotificationChannelResult> ChannelResults { get; set; } = new();

}


public class NotificationChannelResult
{
    public NotificationChannelEnum Channel { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? ExternalMessageId { get; set; } // FCMMessageId, EmailId, etc.
    public string? DeviceToken { get; set; } // For Firebase
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string>? Metadata { get; set; }
}

public class RecipientInfo
{
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public List<string> DeviceTokens { get; set; } = new();
    public Dictionary<string, string>? CustomData { get; set; }
}