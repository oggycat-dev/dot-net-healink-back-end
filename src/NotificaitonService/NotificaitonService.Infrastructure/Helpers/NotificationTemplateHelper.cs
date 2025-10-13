using System.Text.RegularExpressions;
using NotificationService.Application.Commons.Enums;
using NotificationService.Application.Commons.Models;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Contracts.User;
using SharedLibrary.Contracts.User.Events;

namespace NotificationService.Infrastructure.Helpers;

public static class NotificationTemplateHelper
{
    public static NotificationRequest BuildOtpNotification(string contact, string otpCode, OtpTypeEnum otpType, NotificationChannelEnum channel, object? userData = null, int expirationMinutes = 5, string? supportEmail = null, string? appName = null)
    {
        var templateData = GetOtpTemplateData(contact, otpCode, otpType, userData, expirationMinutes, supportEmail, appName);
        var subject = GetSubject(NotificationTemplateEnums.Otp, otpType, channel, appName);
        var htmlContent = ProcessOtpTemplate(templateData);

        return new NotificationRequest
        {
            To = contact,
            Subject = subject,
            Content = htmlContent,
            HtmlContent = htmlContent,
            Template = NotificationTemplateEnums.Otp,
            TemplateData = templateData,
            Priority = NotificationPriorityEnum.High,
            Metadata = new Dictionary<string, string>
            {
                ["otpType"] = otpType.ToString(),
                ["channel"] = channel.ToString(),
                ["contact"] = contact
            }
        };
    }

    public static NotificationRequest BuildWelcomeNotification(string contact, string fullName, NotificationChannelEnum channel, string? supportEmail = null, string? appName = null)
    {
        var templateData = new Dictionary<string, object>
        {
            ["fullName"] = fullName,
            ["contact"] = contact,
            ["appName"] = appName?? "NotificationService",
            ["supportEmail"] = supportEmail ?? "Unknown"
        };

        var htmlContent = ProcessWelcomeTemplate(templateData);

        return new NotificationRequest
        {
            To = contact,
            Subject = GetSubject(NotificationTemplateEnums.Welcome, channel: channel, appName: appName),
            Content = htmlContent,
            HtmlContent = htmlContent,
            Template = NotificationTemplateEnums.Welcome,
            TemplateData = templateData,
            Priority = NotificationPriorityEnum.Normal
        };
    }

    public static NotificationRequest BuildSubscriptionActivatedNotification(
        string contact, 
        string fullName,
        string planName,
        decimal amount,
        string currency,
        string transactionId,
        NotificationChannelEnum channel,
        string? supportEmail = null,
        string? appName = null)
    {
        var templateData = new Dictionary<string, object>
        {
            ["fullName"] = fullName,
            ["planName"] = planName,
            ["amount"] = amount.ToString("N2"),
            ["currency"] = currency,
            ["transactionId"] = transactionId,
            ["activatedDate"] = DateTime.UtcNow.ToString("MMMM dd, yyyy"),
            ["appName"] = appName ?? "Healink",
            ["supportEmail"] = supportEmail ?? "support@healink.com"
        };

        var htmlContent = ProcessSubscriptionActivatedTemplate(templateData);

        return new NotificationRequest
        {
            To = contact,
            Subject = GetSubject(NotificationTemplateEnums.SubscriptionActivated, channel: channel, appName: appName),
            Content = htmlContent,
            HtmlContent = htmlContent,
            Template = NotificationTemplateEnums.SubscriptionActivated,
            TemplateData = templateData,
            Priority = NotificationPriorityEnum.Normal
        };
    }

    public static Dictionary<string, object> GetOtpTemplateData(string contact, string otpCode, OtpTypeEnum otpType, object? userData = null, int expirationMinutes = 5, string? supportEmail = null, string? appName = null)
    {
        var templateData = new Dictionary<string, object>
        {
            ["otpCode"] = otpCode,
            ["contact"] = contact,
            ["otpType"] = otpType.ToString(),
            ["appName"] = appName ?? "NotificationService",
            ["expirationMinutes"] = expirationMinutes.ToString(),
            ["supportEmail"] = supportEmail ?? "Unknown"
        };

        // Set default fullName from contact (email or phone)
        var defaultName = contact.Contains("@") ? contact.Split('@')[0] : contact;
        templateData["fullName"] = defaultName;

        // Add specific data based on OTP type
        switch (otpType)
        {
            case OtpTypeEnum.Registration:
                templateData["action"] = "complete your registration";
                templateData["actionDescription"] = $"Welcome to {templateData["appName"]}! Please verify your account to get started.";
                
                // Add registration specific data from userData
                if (userData is RegistrationEvent registerData)
                {
                    templateData["fullName"] = registerData.FullName ?? registerData.Email;
                }
                break;

            case OtpTypeEnum.PasswordReset:
                templateData["action"] = "reset your password";
                templateData["actionDescription"] = "Someone requested a password reset for your account.";
                break;

            default:
                templateData["action"] = "verify your account";
                templateData["actionDescription"] = "Please complete the verification process.";
                break;
        }

        return templateData;
    }

    public static string GetSubject(NotificationTemplateEnums template, OtpTypeEnum? otpType = null, NotificationChannelEnum channel = NotificationChannelEnum.Email, string? appName = null)
    {
        var appTitle = appName ?? "Healink";
        return template switch
        {
            NotificationTemplateEnums.Otp => otpType switch
            {
                OtpTypeEnum.Registration => $"{appTitle} - Complete Your Registration",
                OtpTypeEnum.PasswordReset => $"{appTitle} - Password Reset Request",
                _ => $"{appTitle} - Verification Code"
            },
            NotificationTemplateEnums.Welcome => $"Welcome to {appTitle}!",
            NotificationTemplateEnums.SubscriptionActivated => $"{appTitle} - Subscription Activated Successfully!",
            _ => $"{appTitle} Notification"
        };
    }

    public static string ProcessOtpTemplate(Dictionary<string, object> data)
    {
        var template = GetOtpTemplate();
        return ProcessTemplate(template, data);
    }

    public static string ProcessWelcomeTemplate(Dictionary<string, object> data)
    {
        var template = GetWelcomeTemplate();
        return ProcessTemplate(template, data);
    }

    public static string ProcessSubscriptionActivatedTemplate(Dictionary<string, object> data)
    {
        var template = GetSubscriptionActivatedTemplate();
        return ProcessTemplate(template, data);
    }

    private static string ProcessTemplate(string template, Dictionary<string, object> data)
    {
        if (string.IsNullOrEmpty(template) || data == null || !data.Any())
            return template;

        var result = template;

        // Replace placeholders like {{variableName}}
        var regex = new Regex(@"\{\{(\w+)\}\}", RegexOptions.IgnoreCase);
        result = regex.Replace(result, match =>
        {
            var key = match.Groups[1].Value;
            if (data.TryGetValue(key, out var value))
            {
                return value?.ToString() ?? string.Empty;
            }
            
            // Return empty string instead of keeping placeholder to avoid displaying {{placeholder}} to users
            return string.Empty;
        });

        return result;
    }

    private static string GetOtpTemplate()
    {
        return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{{appName}} - Verification Code</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #2c3e50; color: white; padding: 20px; text-align: center; }
        .content { padding: 30px; background-color: #f9f9f9; }
        .otp-code { font-size: 32px; font-weight: bold; text-align: center; color: #e74c3c; padding: 20px; background-color: #ecf0f1; border-radius: 5px; margin: 20px 0; letter-spacing: 5px; }
        .footer { text-align: center; padding: 20px; color: #7f8c8d; font-size: 12px; }
        .warning { background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{{appName}}</h1>
        </div>
        <div class='content'>
            <h2>Verification Code</h2>
            <p>Hello {{fullName}},</p>
            <p>{{actionDescription}}</p>
            <p>Please use the following verification code to {{action}}:</p>
            
            <div class='otp-code'>{{otpCode}}</div>
            
            <div class='warning'>
                <strong>⚠️ Important:</strong>
                <ul>
                    <li>This code will expire in {{expirationMinutes}} minutes</li>
                    <li>Never share this code with anyone</li>
                    <li>If you didn't request this code, please ignore this email</li>
                </ul>
            </div>
            
            <p>If you have any questions, please contact our support team at {{supportEmail}}.</p>
        </div>
        <div class='footer'>
            <p>&copy; {{appName}}. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private static string GetWelcomeTemplate()
    {
        return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Welcome to {{appName}}</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #27ae60; color: white; padding: 20px; text-align: center; }
        .content { padding: 30px; background-color: #f9f9f9; }
        .footer { text-align: center; padding: 20px; color: #7f8c8d; font-size: 12px; }
        .welcome-message { background-color: #d5f4e6; border: 1px solid #27ae60; padding: 20px; border-radius: 5px; margin: 20px 0; text-align: center; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to {{appName}}!</h1>
        </div>
        <div class='content'>
            <div class='welcome-message'>
                <h2>🎉 Welcome {{fullName}}!</h2>
                <p>Your account has been successfully created and verified.</p>
            </div>
            
            <p>Thank you for joining {{appName}}! We're excited to have you on board.</p>
            
            <p>You can now enjoy all the features and benefits of our platform.</p>
            
            <p>If you have any questions or need assistance, don't hesitate to contact our support team at {{supportEmail}}.</p>
        </div>
        <div class='footer'>
            <p>&copy; {{appName}}. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private static string GetSubscriptionActivatedTemplate()
    {
        return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Subscription Activated - {{appName}}</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #3498db; color: white; padding: 20px; text-align: center; }
        .content { padding: 30px; background-color: #f9f9f9; }
        .footer { text-align: center; padding: 20px; color: #7f8c8d; font-size: 12px; }
        .success-message { background-color: #d5f4e6; border: 1px solid #27ae60; padding: 20px; border-radius: 5px; margin: 20px 0; text-align: center; }
        .details { background-color: white; border: 1px solid #ddd; padding: 15px; border-radius: 5px; margin: 20px 0; }
        .details-row { padding: 10px 0; border-bottom: 1px solid #eee; }
        .details-row:last-child { border-bottom: none; }
        .label { font-weight: bold; color: #555; }
        .value { color: #333; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✅ Subscription Activated!</h1>
        </div>
        <div class='content'>
            <div class='success-message'>
                <h2>🎉 Congratulations {{fullName}}!</h2>
                <p>Your subscription has been successfully activated.</p>
            </div>
            
            <p>Thank you for subscribing to {{appName}}!</p>
            
            <div class='details'>
                <div class='details-row'>
                    <span class='label'>Plan:</span> 
                    <span class='value'>{{planName}}</span>
                </div>
                <div class='details-row'>
                    <span class='label'>Amount:</span> 
                    <span class='value'>{{amount}} {{currency}}</span>
                </div>
                <div class='details-row'>
                    <span class='label'>Activated Date:</span> 
                    <span class='value'>{{activatedDate}}</span>
                </div>
                <div class='details-row'>
                    <span class='label'>Transaction ID:</span> 
                    <span class='value'>{{transactionId}}</span>
                </div>
            </div>
            
            <p>You now have access to all the premium features included in your subscription plan.</p>
            
            <p>If you have any questions or need assistance, please contact our support team at {{supportEmail}}.</p>
        </div>
        <div class='footer'>
            <p>&copy; {{appName}}. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }
}
