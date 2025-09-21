using System.Transactions;
using AuthService.Application.Commons.Interfaces;
using AuthService.Application.Helpers;
using AuthService.Domain.Entities;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Entities;
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
            // Validate message data
            if (string.IsNullOrEmpty(message.Email))
            {
                _logger.LogError("CreateAuthUser message has empty email. CorrelationId: {CorrelationId}, FullName: {FullName}", 
                    message.CorrelationId, message.FullName);
                
                await context.Publish<AuthUserCreated>(new
                {
                    CorrelationId = message.CorrelationId,
                    UserId = Guid.Empty,
                    Success = false,
                    ErrorMessage = "Invalid message: Email is required",
                    CreatedAt = DateTime.UtcNow
                });
                return;
            }

            if (string.IsNullOrEmpty(message.EncryptedPassword))
            {
                _logger.LogError("CreateAuthUser message has empty encrypted password for email: {Email}, CorrelationId: {CorrelationId}", 
                    message.Email, message.CorrelationId);
                
                await context.Publish<AuthUserCreated>(new
                {
                    CorrelationId = message.CorrelationId,
                    UserId = Guid.Empty,
                    Success = false,
                    ErrorMessage = "Invalid message: Encrypted password is required",
                    CreatedAt = DateTime.UtcNow
                });
                return;
            }

            _logger.LogInformation("Processing CreateAuthUser for email: {Email}, CorrelationId: {CorrelationId}, PasswordLength: {PasswordLength}", 
                message.Email, message.CorrelationId, message.EncryptedPassword?.Length ?? 0);

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
            };

            newUser.InitializeEntity(newUser.Id);

            // Decrypt password with error handling
            string decryptedPassword;
            try
            {
                _logger.LogDebug("Attempting to decrypt password for email: {Email}, EncryptionKeyLength: {KeyLength}", 
                    message.Email, _passwordEncryptionKey?.Length ?? 0);
                
                decryptedPassword = PasswordCryptoHelper.Decrypt(message.EncryptedPassword, _passwordEncryptionKey);
                
                _logger.LogDebug("Password decrypted successfully for email: {Email}", message.Email);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Failed to decrypt password for email: {Email}, CorrelationId: {CorrelationId}. Error: {Error}", 
                    message.Email, message.CorrelationId, ex.Message);
                
                await context.Publish<AuthUserCreated>(new
                {
                    CorrelationId = message.CorrelationId,
                    UserId = Guid.Empty,
                    Success = false,
                    ErrorMessage = $"Password decryption failed: {ex.Message}",
                    CreatedAt = DateTime.UtcNow
                });
                return;
            }

            // Using transaction scope to ensure atomicity without DB retry conflicts
            using (var transaction = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted,
                    Timeout = TimeSpan.FromMinutes(1) // Reduced timeout since no retries
                },
                TransactionScopeAsyncFlowOption.Enabled))
            {
                var result = await _identityService.CreateUserAsync(newUser, decryptedPassword);

                if (result.Succeeded)
                {
                    // Add user to default role
                    var roleResult = await _identityService.AddUserToRoleAsync(newUser, RoleEnum.User.ToString());
                    
                    if (!roleResult.Succeeded)
                    {
                        var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                        _logger.LogError("Failed to add role for user {Email}: {Errors}", message.Email, roleErrors);
                        
                        // Don't complete transaction - will rollback automatically
                        await context.Publish<AuthUserCreated>(new
                        {
                            CorrelationId = message.CorrelationId,
                            UserId = Guid.Empty,
                            Success = false,
                            ErrorMessage = $"User created but role assignment failed: {roleErrors}",
                            CreatedAt = DateTime.UtcNow
                        });
                        return;
                    }

                    // Complete transaction only if everything succeeded
                    transaction.Complete();

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

                    // Don't complete transaction - will rollback automatically
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