using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Commons.Enums;
using NotificationService.Application.Commons.Interfaces;
using NotificationService.Application.Commons.Models;
using NotificationService.Infrastructure.Helpers;
using SharedLibrary.Commons.Enums;

namespace NotificationService.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly AppSettings _appSettings;
    private readonly OtpSettings _otpSettings;

    public EmailService(
        IOptions<EmailSettings> emailSettings,
        IOptions<AppSettings> appSettings,
        IOptions<OtpSettings> otpSettings,
        ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _appSettings = appSettings.Value;
        _otpSettings = otpSettings.Value;
        _logger = logger;
    }

    public Task<List<NotificationSendResult>> SendMultiCastAsync(NotificationRequest message, List<RecipientInfo> recipients)
    {
        throw new NotImplementedException();
    }

    public async Task<NotificationSendResult> SendNotificationAsync(NotificationRequest message, RecipientInfo recipient)
    {
        try
        {
            // Build content by template if not explicitly provided
            var (subject, htmlContent) = BuildEmailContent(message, recipient);

            // Send email using SMTP client
            using (var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort))
            {
                client.Credentials = new System.Net.NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword);
                client.EnableSsl = _emailSettings.EnableSsl;
                client.Timeout = 30000; // 30 seconds timeout

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = subject,
                    Body = htmlContent,
                    IsBodyHtml = true,
                };

                if (string.IsNullOrEmpty(recipient.Email))
                {
                    throw new ArgumentException("Recipient email cannot be null or empty", nameof(recipient.Email));
                }

                mailMessage.To.Add(new MailAddress(recipient.Email, recipient.FullName ?? string.Empty));

                await client.SendMailAsync(mailMessage);
                
                _logger.LogInformation("Email queued successfully to {Email} with subject {Subject}", recipient.Email, subject);
                
                return new NotificationSendResult
                {
                    UserId = recipient.UserId ?? Guid.Empty,
                    Email = recipient.Email,
                    FullName = recipient.FullName,
                    ChannelResults = new List<NotificationChannelResult>
                    {
                        new NotificationChannelResult
                        {
                            Channel = NotificationChannelEnum.Email,
                            Success = true,
                            Message = "Email queued for delivery. Note: Delivery confirmation requires bounce handling.",
                            Timestamp = DateTime.UtcNow
                        }
                    }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} with subject {Subject}", recipient.Email, message.Subject);
            return new NotificationSendResult
            {
                UserId = recipient.UserId ?? Guid.Empty,
                Email = recipient.Email,
                FullName = recipient.FullName,
                ChannelResults = new List<NotificationChannelResult>
                {
                    new NotificationChannelResult
                    {
                        Channel = NotificationChannelEnum.Email,
                        Success = false,
                        Message = "Failed to send email",
                        ErrorMessage = ex.Message,
                        Timestamp = DateTime.UtcNow
                    }
                }
            };
        }
    }

    private (string Subject, string HtmlContent) BuildEmailContent(NotificationRequest message, RecipientInfo recipient)
    {
        // If content already provided, use it directly
        var existingHtml = message.HtmlContent ?? message.Content;
        var existingSubject = message.Subject;
        if (!string.IsNullOrEmpty(existingHtml) && !string.IsNullOrEmpty(existingSubject))
        {
            return (existingSubject, existingHtml);
        }

        string subject = existingSubject ?? string.Empty;
        string htmlContent = existingHtml ?? string.Empty;

        if (!string.IsNullOrEmpty(htmlContent))
        {
            subject = string.IsNullOrEmpty(subject) ? ($"{_appSettings.AppName} Notification") : subject;
            return (subject, htmlContent);
        }

        switch (message.Template)
        {
            case NotificationTemplateEnums.Otp:
            {
                var contact = recipient.Email ?? message.To;
                var otpCode = message.TemplateData.TryGetValue("otpCode", out var oc) ? oc?.ToString() ?? string.Empty : string.Empty;
                var otpTypeStr = message.TemplateData.TryGetValue("otpType", out var ot) ? ot?.ToString() ?? nameof(OtpTypeEnum.Registration) : nameof(OtpTypeEnum.Registration);
                Enum.TryParse<OtpTypeEnum>(otpTypeStr, true, out var otpType);
                var channel = NotificationChannelEnum.Email;

                var data = NotificationTemplateHelper.GetOtpTemplateData(
                    contact,
                    otpCode,
                    otpType,
                    userData: null,
                    expirationMinutes: _otpSettings.ExpirationMinutes,
                    supportEmail: _appSettings.SupportEmail,
                    appName: _appSettings.AppName);

                // Prefer recipient full name when available
                if (!string.IsNullOrWhiteSpace(recipient.FullName))
                {
                    data["fullName"] = recipient.FullName;
                }

                subject = NotificationTemplateHelper.GetSubject(NotificationTemplateEnums.Otp, otpType, channel, _appSettings.AppName);
                htmlContent = NotificationTemplateHelper.ProcessOtpTemplate(data);
                break;
            }
            case NotificationTemplateEnums.Welcome:
            {
                var contact = recipient.Email ?? message.To;
                var fullName = recipient.FullName ?? (contact?.Contains("@") == true ? contact.Split('@')[0] : contact);
                var built = NotificationTemplateHelper.BuildWelcomeNotification(contact ?? string.Empty, fullName ?? string.Empty, NotificationChannelEnum.Email, _appSettings.SupportEmail, _appSettings.AppName);
                subject = built.Subject;
                htmlContent = built.HtmlContent ?? built.Content;
                break;
            }
            default:
            {
                subject = string.IsNullOrEmpty(subject) ? ($"{_appSettings.AppName} Notification") : subject;
                htmlContent = string.IsNullOrEmpty(htmlContent) ? (message.Content ?? string.Empty) : htmlContent;
                break;
            }
        }

        return (subject, htmlContent);
    }

} 