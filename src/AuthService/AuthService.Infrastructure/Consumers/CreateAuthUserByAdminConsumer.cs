using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Helpers;
using SharedLibrary.Contracts.User.Saga;
using AuthService.Domain.Entities;

namespace AuthService.Infrastructure.Consumers;

/// <summary>
/// Consumer to handle CreateAuthUserByAdmin command from AdminUserCreationSaga
/// Creates AppUser in AuthService database with specified role
/// </summary>
public class CreateAuthUserByAdminConsumer : IConsumer<CreateAuthUserByAdmin>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<CreateAuthUserByAdminConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly string _passwordEncryptionKey;

    public CreateAuthUserByAdminConsumer(
        UserManager<AppUser> userManager,
        ILogger<CreateAuthUserByAdminConsumer> logger,
        IPublishEndpoint _publishEndpoint,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _logger = logger;
        this._publishEndpoint = _publishEndpoint;
        _passwordEncryptionKey = configuration["PasswordEncryptionKey"] 
            ?? throw new ArgumentNullException("PasswordEncryptionKey is not configured");
    }

    public async Task Consume(ConsumeContext<CreateAuthUserByAdmin> context)
    {
        var request = context.Message;
        
        try
        {
            _logger.LogInformation("Processing CreateAuthUserByAdmin - Email: {Email}, Role: {Role}, CorrelationId: {CorrelationId}",
                request.Email, request.Role, request.CorrelationId);

            // ✅ IDEMPOTENCY CHECK: If user already exists with same role, return success
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                // Get user's roles
                var roles = await _userManager.GetRolesAsync(existingUser);
                var requestedRole = request.Role.ToString();

                if (roles.Contains(requestedRole))
                {
                    _logger.LogWarning("User already exists with role - Email: {Email}, Role: {Role} - Returning success (idempotent)",
                        request.Email, requestedRole);

                    await _publishEndpoint.Publish<AuthUserCreatedByAdmin>(new
                    {
                        CorrelationId = request.CorrelationId,
                        UserId = existingUser.Id,
                        Email = existingUser.Email!,
                        Success = true,
                        ErrorMessage = (string?)null,
                        CreatedAt = DateTime.UtcNow
                    });

                    return;
                }
                else
                {
                    _logger.LogError("User exists but with different role - Email: {Email}, ExistingRoles: {ExistingRoles}, RequestedRole: {RequestedRole}",
                        request.Email, string.Join(",", roles), requestedRole);

                    await _publishEndpoint.Publish<AuthUserCreatedByAdmin>(new
                    {
                        CorrelationId = request.CorrelationId,
                        UserId = Guid.Empty,
                        Email = request.Email,
                        Success = false,
                        ErrorMessage = $"User already exists with different role: {string.Join(",", roles)}",
                        CreatedAt = DateTime.UtcNow
                    });

                    return;
                }
            }

            // Decrypt password
            var decryptedPassword = PasswordCryptoHelper.Decrypt(request.EncryptedPassword, _passwordEncryptionKey);

            // Create AppUser
            var appUser = new AppUser
            {
                Email = request.Email,
                UserName = request.Email,
                PhoneNumber = request.PhoneNumber,
                EmailConfirmed = true, // Admin-created users are pre-verified
                PhoneNumberConfirmed = true,
                Status = EntityStatusEnum.Active,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(appUser, decryptedPassword);

            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create AuthUser - Email: {Email}, Errors: {Errors}", 
                    request.Email, errors);

                await _publishEndpoint.Publish<AuthUserCreatedByAdmin>(new
                {
                    CorrelationId = request.CorrelationId,
                    UserId = Guid.Empty,
                    Email = request.Email,
                    Success = false,
                    ErrorMessage = $"Failed to create user: {errors}",
                    CreatedAt = DateTime.UtcNow
                });

                return;
            }

            // Assign role
            var roleName = request.Role.ToString();
            var roleResult = await _userManager.AddToRoleAsync(appUser, roleName);

            if (!roleResult.Succeeded)
            {
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to assign role - Email: {Email}, Role: {Role}, Errors: {Errors}",
                    request.Email, roleName, errors);

                // Clean up: Delete the user since role assignment failed
                await _userManager.DeleteAsync(appUser);

                await _publishEndpoint.Publish<AuthUserCreatedByAdmin>(new
                {
                    CorrelationId = request.CorrelationId,
                    UserId = Guid.Empty,
                    Email = request.Email,
                    Success = false,
                    ErrorMessage = $"Failed to assign role: {errors}",
                    CreatedAt = DateTime.UtcNow
                });

                return;
            }

            _logger.LogInformation("AuthUser created successfully - UserId: {UserId}, Email: {Email}, Role: {Role}",
                appUser.Id, request.Email, roleName);

            // Publish success event
            await _publishEndpoint.Publish<AuthUserCreatedByAdmin>(new
            {
                CorrelationId = request.CorrelationId,
                UserId = appUser.Id,
                Email = appUser.Email!,
                Success = true,
                ErrorMessage = (string?)null,
                CreatedAt = DateTime.UtcNow
            });

            _logger.LogInformation("AuthUserCreatedByAdmin event published - UserId: {UserId}, CorrelationId: {CorrelationId}",
                appUser.Id, request.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating AuthUser - Email: {Email}, CorrelationId: {CorrelationId}",
                request.Email, request.CorrelationId);

            await _publishEndpoint.Publish<AuthUserCreatedByAdmin>(new
            {
                CorrelationId = request.CorrelationId,
                UserId = Guid.Empty,
                Email = request.Email,
                Success = false,
                ErrorMessage = ex.Message,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
