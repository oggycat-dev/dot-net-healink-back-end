using AuthService.Application.Commons.Interfaces;
using AuthService.Application.Helpers;
using AuthService.Domain.Entities;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Contracts.User.Saga;

namespace AuthService.Infrastructure.Consumers;

/// <summary>
/// Consumer để xử lý CreateAuthUser command từ Registration Saga
/// </summary>
public class CreateAuthUserConsumer : IConsumer<CreateAuthUser>
{
    private readonly IIdentityService _identityService;
    private readonly ILogger<CreateAuthUserConsumer> _logger;
    private readonly string _passwordEncryptionKey;

    public CreateAuthUserConsumer(
        IIdentityService identityService,
        ILogger<CreateAuthUserConsumer> logger,
        IConfiguration configuration)
    {
        _identityService = identityService;
        _logger = logger;
        _passwordEncryptionKey = configuration["PasswordEncryptionKey"] ?? throw new ArgumentNullException("PasswordEncryptionKey is not configured");
    }

    public async Task Consume(ConsumeContext<CreateAuthUser> context)
    {
        var message = context.Message;
        
        try
        {
            _logger.LogInformation("Processing CreateAuthUser for email: {Email}, CorrelationId: {CorrelationId}", 
                message.Email, message.CorrelationId);

            // Check if user already exists
            var existingUser = await _identityService.GetUserByFirstOrDefaultAsync(
                u => u.Email.ToLower() == message.Email.ToLower());

            if (existingUser != null)
            {
                _logger.LogWarning("Auth user already exists for email: {Email}, CorrelationId: {CorrelationId}", 
                    message.Email, message.CorrelationId);

                // If user already exists, consider it a success to avoid duplicate creation
                await context.Publish<AuthUserCreated>(new
                {
                    CorrelationId = message.CorrelationId,
                    UserId = existingUser.Id,
                    Success = true,
                    ErrorMessage = (string?)null,
                    CreatedAt = DateTime.UtcNow
                });
                return;
            }

            // Create new AppUser
            var newUser = new AppUser
            {
                UserName = message.Email,
                Email = message.Email,
                EmailConfirmed = true, // Email is verified through OTP
                PhoneNumber = message.PhoneNumber,
                PhoneNumberConfirmed = !string.IsNullOrEmpty(message.PhoneNumber),
                Status = EntityStatusEnum.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Decrypt password
            var decryptedPassword = PasswordCryptoHelper.Decrypt(message.EncryptedPassword, _passwordEncryptionKey);
            var result = await _identityService.CreateUserAsync(newUser, "");
            
            if (result.Succeeded)
            {
                // Add user to default role
                await _identityService.AddUserToRoleAsync(newUser, RoleEnum.User.ToString());

                _logger.LogInformation("Auth user created successfully for email: {Email}, UserId: {UserId}, CorrelationId: {CorrelationId}", 
                    message.Email, newUser.Id, message.CorrelationId);

                // Publish success response
                await context.Publish<AuthUserCreated>(new
                {
                    CorrelationId = message.CorrelationId,
                    UserId = newUser.Id,
                    Success = true,
                    ErrorMessage = (string?)null,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create auth user for email: {Email}, CorrelationId: {CorrelationId}, Errors: {Errors}", 
                    message.Email, message.CorrelationId, errors);

                // Publish failure response
                await context.Publish<AuthUserCreated>(new
                {
                    CorrelationId = message.CorrelationId,
                    UserId = Guid.Empty,
                    Success = false,
                    ErrorMessage = errors,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating auth user for email: {Email}, CorrelationId: {CorrelationId}", 
                message.Email, message.CorrelationId);

            // Publish failure response
            await context.Publish<AuthUserCreated>(new
            {
                CorrelationId = message.CorrelationId,
                UserId = Guid.Empty,
                Success = false,
                ErrorMessage = ex.Message,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}