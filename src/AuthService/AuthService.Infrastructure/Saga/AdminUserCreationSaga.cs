using MassTransit;
using Microsoft.Extensions.Logging;
using SharedLibrary.Contracts.User.Saga;

namespace AuthService.Infrastructure.Saga;

/// <summary>
/// Admin User Creation Saga - Orchestrates admin-initiated user creation workflow
/// Owned by AuthService (like RegistrationSaga) - manages distributed transaction with compensating actions
/// Flow: AdminUserCreationStarted → CreateAuthUser → UpdateUserProfile → Completed
/// Pattern: Follows RegistrationSaga pattern for consistency
/// </summary>
public class AdminUserCreationSaga : MassTransitStateMachine<AdminUserCreationSagaState>
{
    private readonly ILogger<AdminUserCreationSaga> _logger;

    public AdminUserCreationSaga(ILogger<AdminUserCreationSaga> logger)
    {
        _logger = logger;
        
        InstanceState(x => x.CurrentState);
        
        // Configure Events with proper correlation strategy (same as RegistrationSaga)
        Event(() => AdminUserCreationStartedEvent, x => 
        {
            x.CorrelateById(context => context.Message.CorrelationId);
            // Let MassTransit handle saga creation automatically
        });
        
        Event(() => AuthUserCreatedByAdminEvent, x => 
        {
            x.CorrelateById(context => context.Message.CorrelationId);
            x.OnMissingInstance(m => m.Discard()); // Don't create new saga
        });
        
        Event(() => UserProfileUpdatedByAdminEvent, x => 
        {
            x.CorrelateById(context => context.Message.CorrelationId);
            x.OnMissingInstance(m => m.Discard()); // Don't create new saga
        });
        
        // Compensating Event
        Event(() => AuthUserDeletedByAdminEvent, x => 
        {
            x.CorrelateById(context => context.Message.CorrelationId);
            x.OnMissingInstance(m => m.Discard()); // Don't create new saga
        });
        
        // Define workflow with idempotency check (same pattern as RegistrationSaga)
        Initially(
            When(AdminUserCreationStartedEvent)
                .IfElse(context => string.IsNullOrEmpty(context.Saga.Email),
                    // ✅ First time - email is empty, initialize saga
                    x => x.Then(context =>
                    {
                        var timestamp = DateTime.UtcNow;
                        
                        _logger.LogInformation("NEW AdminUserCreationSaga instance created - Email: {Email}, Role: {Role}, CorrelationId: {CorrelationId}", 
                            context.Message.Email, context.Message.Role, context.Message.CorrelationId);
                        
                        // COMPREHENSIVE STATE INITIALIZATION
                        context.Saga.CorrelationId = context.Message.CorrelationId;
                        context.Saga.Email = context.Message.Email;
                        context.Saga.EncryptedPassword = context.Message.EncryptedPassword;
                        context.Saga.FullName = context.Message.FullName;
                        context.Saga.PhoneNumber = context.Message.PhoneNumber;
                        context.Saga.Address = context.Message.Address;
                        context.Saga.Role = context.Message.Role;
                        context.Saga.CreatedBy = context.Message.CreatedBy;
                        
                        // Store pre-created UserProfile info
                        context.Saga.UserProfileId = context.Message.UserProfileId;
                        
                        // Timestamps
                        context.Saga.CreatedAt = timestamp;
                        context.Saga.StartedAt = timestamp;
                        
                        // Status flags
                        context.Saga.IsCompleted = false;
                        context.Saga.IsFailed = false;
                        context.Saga.RetryCount = 0;
                        
                        // Clear residual data
                        context.Saga.AuthUserCreatedAt = null;
                        context.Saga.UserProfileUpdatedAt = null;
                        context.Saga.CompletedAt = null;
                        context.Saga.AuthUserId = null;
                        context.Saga.ErrorMessage = null;
                        
                        _logger.LogInformation("Saga {CorrelationId} initialized - Email: {Email}, Role: {Role}, UserProfileId: {ProfileId}", 
                            context.Saga.CorrelationId, context.Saga.Email, context.Saga.Role, context.Saga.UserProfileId);
                    })
                    // Publish CreateAuthUserByAdmin command
                    .PublishAsync(context => context.Init<CreateAuthUserByAdmin>(new
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        Email = context.Saga.Email,
                        EncryptedPassword = context.Saga.EncryptedPassword,
                        FullName = context.Saga.FullName,
                        PhoneNumber = context.Saga.PhoneNumber,
                        Role = context.Saga.Role,
                        CreatedBy = context.Saga.CreatedBy
                    }))
                    .TransitionTo(Started),
                    
                    // ❌ Duplicate - saga already initialized, ignore
                    x => x.Then(context =>
                    {
                        _logger.LogWarning("DUPLICATE AdminUserCreationStarted ignored - Email: {Email}, CorrelationId: {CorrelationId}", 
                            context.Saga.Email, context.Message.CorrelationId);
                    })
                )
        );
        
        // Wait for AuthUser creation
        During(Started,
            When(AuthUserCreatedByAdminEvent)
                .If(context => context.Message.Success, x => x // Success case
                    .Then(context =>
                    {
                        context.Saga.AuthUserId = context.Message.UserId;
                        context.Saga.AuthUserCreatedAt = DateTime.UtcNow;
                        _logger.LogInformation("Auth user created for {Email} with ID {UserId}, Role: {Role}", 
                            context.Saga.Email, context.Message.UserId, context.Saga.Role);
                    })
                    // Publish UpdateUserProfileUserId command to UserService
                    .PublishAsync(context => context.Init<UpdateUserProfileUserId>(new
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        UserProfileId = context.Saga.UserProfileId,
                        UserId = context.Saga.AuthUserId
                    }))
                    .TransitionTo(AuthUserCreated))
                .If(context => !context.Message.Success, x => x // Failure case
                    .Then(context =>
                    {
                        context.Saga.ErrorMessage = $"Auth user creation failed: {context.Message.ErrorMessage}";
                        context.Saga.IsFailed = true;
                        context.Saga.CompletedAt = DateTime.UtcNow;
                        _logger.LogError("Auth user creation failed for {Email}: {Error}", 
                            context.Saga.Email, context.Message.ErrorMessage);
                    })
                    .TransitionTo(Failed)),
                
            // Ignore duplicate events
            Ignore(AdminUserCreationStartedEvent)
        );
        
        // Wait for UserProfile update
        During(AuthUserCreated,
            When(UserProfileUpdatedByAdminEvent)
                .If(context => context.Message.Success, x => x // Success case
                    .Then(context =>
                    {
                        context.Saga.UserProfileUpdatedAt = DateTime.UtcNow;
                        context.Saga.CompletedAt = DateTime.UtcNow;
                        context.Saga.IsCompleted = true;
                        context.Saga.IsFailed = false;
                        
                        _logger.LogInformation("Admin user creation COMPLETED - Email: {Email}, CorrelationId: {CorrelationId}, AuthUserId: {AuthUserId}, ProfileId: {ProfileId}", 
                            context.Saga.Email, context.Saga.CorrelationId, context.Saga.AuthUserId, context.Saga.UserProfileId);
                    })
                    .Finalize())
                .If(context => !context.Message.Success, x => x // Failure case - rollback AuthUser
                    .Then(context =>
                    {
                        context.Saga.ErrorMessage = $"UserProfile update failed: {context.Message.ErrorMessage}";
                        _logger.LogError("UserProfile update failed for {Email}: {Error}. Rolling back AuthUser {UserId}", 
                            context.Saga.Email, context.Message.ErrorMessage, context.Saga.AuthUserId);
                    })
                    .PublishAsync(context => context.Init<DeleteAuthUserByAdmin>(new
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        UserId = context.Saga.AuthUserId,
                        Reason = "UserProfile update failed"
                    }))
                    .TransitionTo(RollingBack)),
                
            // Ignore duplicate events
            Ignore(AdminUserCreationStartedEvent),
            Ignore(AuthUserCreatedByAdminEvent)
        );
        
        // Handle rollback process
        During(RollingBack,
            When(AuthUserDeletedByAdminEvent)
                .If(context => context.Message.Success, x => x
                    .Then(context =>
                    {
                        context.Saga.IsFailed = true;
                        context.Saga.CompletedAt = DateTime.UtcNow;
                        _logger.LogInformation("AuthUser {UserId} deleted during rollback for {Email}", 
                            context.Message.UserId, context.Saga.Email);
                    })
                    .TransitionTo(RolledBack))
                .If(context => !context.Message.Success, x => x
                    .Then(context =>
                    {
                        context.Saga.ErrorMessage += $" | AuthUser deletion failed: {context.Message.ErrorMessage}";
                        context.Saga.IsFailed = true;
                        context.Saga.CompletedAt = DateTime.UtcNow;
                        _logger.LogError("Failed to delete AuthUser {UserId} during rollback for {Email}: {Error}", 
                            context.Message.UserId, context.Saga.Email, context.Message.ErrorMessage);
                    })
                    .TransitionTo(Failed)),
                
            // Ignore duplicate events
            Ignore(AdminUserCreationStartedEvent),
            Ignore(AuthUserCreatedByAdminEvent),
            Ignore(UserProfileUpdatedByAdminEvent)
        );
        
        During(Failed,
            // Ignore all events in Failed state
            Ignore(AdminUserCreationStartedEvent),
            Ignore(AuthUserCreatedByAdminEvent),
            Ignore(UserProfileUpdatedByAdminEvent),
            Ignore(AuthUserDeletedByAdminEvent)
        );
        
        During(RolledBack,
            // Ignore all events in RolledBack state
            Ignore(AdminUserCreationStartedEvent),
            Ignore(AuthUserCreatedByAdminEvent),
            Ignore(UserProfileUpdatedByAdminEvent),
            Ignore(AuthUserDeletedByAdminEvent)
        );
        
        // Auto-delete completed sagas
        SetCompletedWhenFinalized();
    }

    // States
    public State Started { get; private set; } = null!;
    public State AuthUserCreated { get; private set; } = null!;
    public State Failed { get; private set; } = null!;
    public State RollingBack { get; private set; } = null!;
    public State RolledBack { get; private set; } = null!;
    
    // Events
    public Event<AdminUserCreationStarted> AdminUserCreationStartedEvent { get; private set; } = null!;
    public Event<AuthUserCreatedByAdmin> AuthUserCreatedByAdminEvent { get; private set; } = null!;
    public Event<UserProfileUpdatedByAdmin> UserProfileUpdatedByAdminEvent { get; private set; } = null!;
    
    // Compensating Event
    public Event<AuthUserDeletedByAdmin> AuthUserDeletedByAdminEvent { get; private set; } = null!;
}
