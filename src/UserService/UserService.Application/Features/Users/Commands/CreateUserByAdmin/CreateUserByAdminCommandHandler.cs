using MassTransit;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Helpers;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Repositories;
using SharedLibrary.Commons.Services;
using SharedLibrary.Contracts.User.Saga;
using UserService.Application.Features.Users.Commands.CreateUserByAdmin;
using UserService.Domain.Entities;

namespace UserService.Application.Features.Users.Commands.CreateUserByAdmin;

/// <summary>
/// Handler for CreateUserByAdminCommand
/// Pattern: Pre-creates UserProfile (Pending), publishes event, waits for saga completion
/// Following RegistrationSaga pattern
/// </summary>
public class CreateUserByAdminCommandHandler : IRequestHandler<CreateUserByAdminCommand, Result<CreateUserByAdminResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private ICurrentUserService _currentUserService;
    private readonly ILogger<CreateUserByAdminCommandHandler> _logger;
    private readonly string _passwordEncryptionKey;

    // Configuration for polling saga completion
    private const int MaxPollAttempts = 30; // 30 attempts
    private const int PollIntervalMs = 1000; // 1 second between attempts
    private const int TotalTimeoutSeconds = 30; // Max 30 seconds total

    public CreateUserByAdminCommandHandler(
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<CreateUserByAdminCommandHandler> logger,
        ICurrentUserService currentUserService,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _currentUserService = currentUserService;
        _passwordEncryptionKey = configuration["PasswordEncryptionKey"]
            ?? throw new ArgumentNullException("PasswordEncryptionKey is not configured");
    }

    public async Task<Result<CreateUserByAdminResponse>> Handle(CreateUserByAdminCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Admin creating user - Email: {Email}, Role: {Role}", request.Email, request.Role);

            // ✅ STEP 1: Validation - Check if email already exists
            var existingProfile = await _unitOfWork.Repository<UserProfile>()
                .GetFirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingProfile != null)
            {
                _logger.LogWarning("Email already exists - Email: {Email}", request.Email);
                return Result<CreateUserByAdminResponse>.Failure(
                    "Email already exists",
                    ErrorCodeEnum.ValidationFailed);
            }

            // ✅ STEP 2: Pre-create UserProfile with Status = Pending
            // Pattern: Same as OTP pre-creation in RegistrationSaga
            var userProfile = new UserProfile
            {
                Email = request.Email,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                Status = EntityStatusEnum.Pending, // ⚠️ Pending until saga completes
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
                // UserId will be set by saga when AuthUser is created
            };

            userProfile.InitializeEntity(Guid.Parse(_currentUserService.UserId));

            await _unitOfWork.Repository<UserProfile>().AddAsync(userProfile);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Pre-created UserProfile (Pending) - ProfileId: {ProfileId}, Email: {Email}",
                userProfile.Id, request.Email);

            // ✅ STEP 3: Encrypt password
            var encryptedPassword = PasswordCryptoHelper.Encrypt(request.Password, _passwordEncryptionKey);

            // ✅ STEP 4: Create correlation ID and publish AdminUserCreationStarted event
            var correlationId = Guid.NewGuid();

            _logger.LogInformation("Publishing AdminUserCreationStarted - CorrelationId: {CorrelationId}, ProfileId: {ProfileId}, Role: {Role}",
                correlationId, userProfile.Id, request.Role);

            await _publishEndpoint.Publish<AdminUserCreationStarted>(new
            {
                CorrelationId = correlationId,
                Email = request.Email,
                EncryptedPassword = encryptedPassword,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                Role = request.Role, // ✅ RoleEnum, not string
                UserProfileId = userProfile.Id, // Pass pre-created profile ID
                StartedAt = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogInformation("Event published - CorrelationId: {CorrelationId}", correlationId);

            // ✅ STEP 5: Poll for saga completion (UserProfile.Status = Active)
            var pollAttempt = 0;
            var sagaCompleted = false;

            while (pollAttempt < MaxPollAttempts && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(PollIntervalMs, cancellationToken);
                pollAttempt++;

                // Refresh UserProfile from database
                var updatedProfile = await _unitOfWork.Repository<UserProfile>()
                    .GetFirstOrDefaultAsync(u => u.Id == userProfile.Id);

                if (updatedProfile == null)
                {
                    _logger.LogError("UserProfile not found during polling - ProfileId: {ProfileId}", userProfile.Id);
                    return Result<CreateUserByAdminResponse>.Failure(
                        "User creation failed - profile lost",
                        ErrorCodeEnum.InternalError);
                }

                // Check if saga completed successfully
                if (updatedProfile.Status == EntityStatusEnum.Active && updatedProfile.UserId != Guid.Empty)
                {
                    _logger.LogInformation("Saga completed successfully - ProfileId: {ProfileId}, UserId: {UserId}, Attempts: {Attempts}",
                        userProfile.Id, updatedProfile.UserId, pollAttempt);

                    sagaCompleted = true;

                    return Result<CreateUserByAdminResponse>.Success(new CreateUserByAdminResponse
                    {
                        UserProfileId = updatedProfile.Id,
                        Email = updatedProfile.Email,
                        FullName = updatedProfile.FullName,
                        Role = request.Role,
                        Message = "User created successfully"
                    });
                }

                // Check if saga failed
                if (updatedProfile.Status == EntityStatusEnum.Inactive)
                {
                    _logger.LogError("Saga failed - ProfileId: {ProfileId}, Status: Inactive", userProfile.Id);
                    return Result<CreateUserByAdminResponse>.Failure(
                        "User creation failed in saga",
                        ErrorCodeEnum.InternalError);
                }

                _logger.LogDebug("Polling saga completion - Attempt: {Attempt}/{MaxAttempts}, Status: {Status}",
                    pollAttempt, MaxPollAttempts, updatedProfile.Status);
            }

            // Timeout - Saga didn't complete in time
            if (!sagaCompleted)
            {
                _logger.LogWarning("Saga timeout - ProfileId: {ProfileId}, Attempts: {Attempts}",
                    userProfile.Id, pollAttempt);

                return Result<CreateUserByAdminResponse>.Failure(
                    $"User creation timeout after {TotalTimeoutSeconds} seconds. Please check saga status.",
                    ErrorCodeEnum.ExternalServiceError);
            }

            // Should not reach here
            return Result<CreateUserByAdminResponse>.Failure(
                "Unexpected error during user creation",
                ErrorCodeEnum.InternalError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user by admin - Email: {Email}", request.Email);
            return Result<CreateUserByAdminResponse>.Failure(
                $"An error occurred: {ex.Message}",
                ErrorCodeEnum.InternalError);
        }
    }
}
