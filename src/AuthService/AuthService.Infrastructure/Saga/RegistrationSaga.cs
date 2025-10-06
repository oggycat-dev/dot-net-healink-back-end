using MassTransit;
using Microsoft.Extensions.Logging;
using SharedLibrary.Contracts.User.Events;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Contracts.User.Saga;

namespace AuthService.Infrastructure.Saga;

/// <summary>
/// Registration Saga - Orchestrates user registration workflow across AuthService and UserService
/// Owned by AuthService - manages distributed transaction with compensating actions
/// </summary>
public class RegistrationSaga : MassTransitStateMachine<RegistrationSagaState>
{
    private readonly ILogger<RegistrationSaga> _logger;

    public RegistrationSaga(ILogger<RegistrationSaga> logger)
    {
        _logger = logger;
        
        InstanceState(x => x.CurrentState);
        
        // Configure Events with proper correlation strategy
        // CRITICAL: Each event correlates by unique CorrelationId - allows multiple sagas per email
        Event(() => RegistrationStartedEvent, x => 
        {
            x.CorrelateById(context => context.Message.CorrelationId);
            // OPTIMIZED: Let MassTransit handle saga creation automatically
            // This prevents race conditions and duplicate key violations
        });
        
        Event(() => OtpSentEvent, x => 
        {
            x.CorrelateById(context => context.Message.CorrelationId);
            x.OnMissingInstance(m => m.Discard()); // Don't create new saga for OtpSent
        });
        Event(() => OtpVerifiedEvent, x => 
        {
            x.CorrelateById(context => context.Message.CorrelationId);
            x.OnMissingInstance(m => m.Discard()); // Don't create new saga for OtpVerified
        });
        Event(() => AuthUserCreatedEvent, x => 
        {
            x.CorrelateById(context => context.Message.CorrelationId);
            x.OnMissingInstance(m => m.Discard()); // Don't create new saga for AuthUserCreated
        });
        Event(() => UserProfileCreatedEvent, x => 
        {
            x.CorrelateById(context => context.Message.CorrelationId);
            x.OnMissingInstance(m => m.Discard()); // Don't create new saga for UserProfileCreated
        });
        
        // Compensating Events
        Event(() => AuthUserDeletedEvent, x => 
        {
            x.CorrelateById(context => context.Message.CorrelationId);
            x.OnMissingInstance(m => m.Discard()); // Don't create new saga for compensating events
        });
        Event(() => UserProfileDeletedEvent, x => 
        {
            x.CorrelateById(context => context.Message.CorrelationId);
            x.OnMissingInstance(m => m.Discard()); // Don't create new saga for compensating events
        });
        
        // TODO: Re-enable timeout scheduling when RabbitMQ delayed message plugin is available
        // Schedule(() => OtpTimeoutSchedule, instance => instance.OtpTimeoutTokenId, s =>
        // {
        //     s.Delay = TimeSpan.FromMinutes(5); // OTP hết hạn sau 5 phút
        //     s.Received = e => e.CorrelateById(context => context.Message.CorrelationId);
        // });
        
        // Định nghĩa workflow với proper idempotency check
        Initially(
            When(RegistrationStartedEvent)
                .IfElse(context => string.IsNullOrEmpty(context.Saga.Email),
                    // ✅ First time - email is empty, so initialize saga
                    x => x.Then(context =>
                    {
                        var timestamp = DateTime.UtcNow;
                        
                        // Log incoming message for debugging
                        _logger.LogInformation("NEW RegistrationSaga instance created - Email: {Email}, CorrelationId: {CorrelationId}, Timestamp: {Timestamp}", 
                            context.Message.Email, context.Message.CorrelationId, timestamp);
                        
                        // COMPREHENSIVE STATE INITIALIZATION - Each saga instance is completely independent
                        context.Saga.CorrelationId = context.Message.CorrelationId;
                        context.Saga.Email = context.Message.Email;
                        context.Saga.EncryptedPassword = context.Message.EncryptedPassword;
                        context.Saga.FullName = context.Message.FullName;
                        context.Saga.PhoneNumber = context.Message.PhoneNumber;
                        context.Saga.Channel = context.Message.Channel;
                        context.Saga.OtpCode = context.Message.OtpCode;
                        context.Saga.ExpiresInMinutes = context.Message.ExpiresInMinutes;
                        
                        // Timestamps
                        context.Saga.CreatedAt = timestamp;
                        context.Saga.StartedAt = timestamp;
                        
                        // Status flags
                        context.Saga.IsCompleted = false;
                        context.Saga.IsFailed = false;
                        context.Saga.RetryCount = 0;
                        
                        // Clear any potential residual data
                        context.Saga.OtpSentAt = null;
                        context.Saga.OtpVerifiedAt = null;
                        context.Saga.AuthUserCreatedAt = null;
                        context.Saga.UserProfileCreatedAt = null;
                        context.Saga.CompletedAt = null;
                        context.Saga.AuthUserId = null;
                        context.Saga.UserProfileId = null;
                        context.Saga.ErrorMessage = null;
                        context.Saga.OtpTimeoutTokenId = null;
                        
                        _logger.LogInformation("Saga {CorrelationId} initialized successfully - Email: {Email}, FullName: {FullName}, Channel: {Channel}", 
                            context.Saga.CorrelationId, context.Saga.Email, context.Saga.FullName, context.Saga.Channel);
                    })

                    /* publish SendOtpNotification event to NotificationService 
                     after saga initialized (otp cache redis successffly) */
                    .PublishAsync(context => context.Init<SendOtpNotification>(new
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        Contact = context.Message.Channel == NotificationChannelEnum.Email 
                            ? context.Message.Email 
                            : context.Message.PhoneNumber,
                        OtpCode = context.Message.OtpCode,
                        Channel = context.Message.Channel,
                        OtpType = OtpTypeEnum.Registration,
                        FullName = context.Message.FullName,
                        ExpiresInMinutes = context.Message.ExpiresInMinutes
                    }))
                    .TransitionTo(Started),
                    
                    // ❌ Duplicate - saga already initialized, ignore safely
                    x => x.Then(context =>
                    {
                        _logger.LogWarning("DUPLICATE RegistrationStarted ignored - Saga already initialized. Email: {Email}, CorrelationId: {CorrelationId}, Current State: {State}", 
                            context.Saga.Email, context.Message.CorrelationId, context.Saga.CurrentState);
                    })
                )
        );
        
        /*
        while state is Started, saga will wait for OtpSentEvent from NotificationService
        if OtpSentEvent is received, saga will transition to OtpSent state
        */
        During(Started,
            When(OtpSentEvent)
                .Then(context =>
                {
                    context.Saga.OtpSentAt = DateTime.UtcNow;
                    _logger.LogInformation("✅ SAGA RECEIVED OtpSent event! Email: {Email}, CorrelationId: {CorrelationId}, Current State: {State} → Transitioning to OtpSent", 
                        context.Saga.Email, context.Saga.CorrelationId, context.Saga.CurrentState);
                })
                .TransitionTo(OtpSent),
                
            // Ignore duplicate events
            Ignore(RegistrationStartedEvent)
        );

        /*
        while state is OtpSent,saga will wait for OtpVerifiedEvent from AuthService.
        if OtpVerifiedEvent is received, then publish CreateAuthUser event to AuthService
        and saga will transition to OtpVerified state
        */
        During(OtpSent,
            When(OtpVerifiedEvent)
                .IfElse(context => !string.IsNullOrEmpty(context.Saga.Email), 
                    // ✅ Success path - saga state is valid
                    x => x.Then(context =>
                    {
                        context.Saga.OtpVerifiedAt = DateTime.UtcNow;
                        _logger.LogInformation("✅ SAGA RECEIVED OtpVerified event! Email: {Email}, CorrelationId: {CorrelationId}, Current State: {State}", 
                            context.Saga.Email, context.Saga.CorrelationId, context.Saga.CurrentState);
                    })
                    .PublishAsync(context => 
                    {
                        _logger.LogInformation("Publishing CreateAuthUser for email: {Email}, CorrelationId: {CorrelationId}", 
                            context.Saga.Email, context.Saga.CorrelationId);
                        
                        return context.Init<CreateAuthUser>(new
                        {
                            CorrelationId = context.Saga.CorrelationId,
                            Email = context.Saga.Email,
                            EncryptedPassword = context.Saga.EncryptedPassword,
                            FullName = context.Saga.FullName,
                            PhoneNumber = context.Saga.PhoneNumber
                        });
                    }).TransitionTo(OtpVerified),
                    
                    // ❌ Failure path - saga state is corrupted  
                    x => x.Then(context =>
                    {
                        _logger.LogError("CRITICAL: Saga state corrupted on OtpVerified - Email is empty! CorrelationId: {CorrelationId}. Transitioning to Failed state.", 
                            context.Saga.CorrelationId);
                        
                        context.Saga.ErrorMessage = "Saga state corrupted: Email is empty during OTP verification";
                        context.Saga.IsFailed = true;
                        context.Saga.CompletedAt = DateTime.UtcNow;
                    })
                    .PublishAsync(context => context.Init<RegistrationFailed>(new
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        Email = context.Saga.Email ?? "unknown",
                        ErrorMessage = context.Saga.ErrorMessage ?? "Saga state corrupted",
                        FailureReason = "Email empty during OTP verification",
                        FailedAt = DateTime.UtcNow
                    }))
                    .TransitionTo(Failed)),
                
            // Ignore duplicate events
            Ignore(RegistrationStartedEvent),
            Ignore(OtpSentEvent)
        );

        /*
        while state is OtpVerified, saga will wait for AuthUserCreatedEvent from AuthService
        if successfully AuthUserCreatedEvent is received, then publish CreateUserProfile event to UserService
        and saga will transition to AuthUserCreated state
        */
        During(OtpVerified,
            When(AuthUserCreatedEvent)
                .If(context => context.Message.Success, x => x // Success case
                    .Then(context =>
                    {
                        context.Saga.AuthUserId = context.Message.UserId;
                        context.Saga.AuthUserCreatedAt = DateTime.UtcNow;
                        _logger.LogInformation("Auth user created for {Email} with ID {UserId}", 
                            context.Saga.Email, context.Message.UserId);
                    })
                    .PublishAsync(context => context.Init<CreateUserProfile>(new
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        UserId = context.Saga.AuthUserId, // UserId từ AuthService
                        Email = context.Saga.Email,
                        FullName = context.Saga.FullName,
                        PhoneNumber = context.Saga.PhoneNumber
                    }))
                    .TransitionTo(AuthUserCreated))
                .If(context => !context.Message.Success, x => x // Failure case
                    .Then(context =>
                    {
                        context.Saga.ErrorMessage = $"Auth user creation failed: {context.Message.ErrorMessage}";
                        _logger.LogError("Auth user creation failed for {Email}: {Error}", 
                            context.Saga.Email, context.Message.ErrorMessage);
                    })
                    .TransitionTo(Failed)),
                
            // Ignore duplicate events
            Ignore(RegistrationStartedEvent),
            Ignore(OtpSentEvent),
            Ignore(OtpVerifiedEvent)
        );

        /*
        while state is AuthUserCreated, saga will wait for UserProfileCreatedEvent from UserService
        if successfully UserProfileCreatedEvent is received, then publish SendWelcomeNotification event to NotificationService
        and saga will transition to UserProfileCreated state
        */
        During(AuthUserCreated,
            When(UserProfileCreatedEvent)
                .If(context => context.Message.Success, x => x // Success case
                    .Then(context =>
                    {
                        context.Saga.UserProfileId = context.Message.UserProfileId;
                        context.Saga.UserProfileCreatedAt = DateTime.UtcNow;
                        context.Saga.CompletedAt = DateTime.UtcNow;
                        context.Saga.IsCompleted = true;
                        context.Saga.IsFailed = false;
                        
                        _logger.LogInformation("Registration COMPLETED successfully for {Email} - CorrelationId: {CorrelationId}, AuthUserId: {AuthUserId}, ProfileId: {ProfileId}", 
                            context.Saga.Email, context.Saga.CorrelationId, context.Saga.AuthUserId, context.Message.UserProfileId);
                    })
                    .PublishAsync(context => context.Init<SendWelcomeNotification>(new
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        Email = context.Saga.Email,
                        FullName = context.Saga.FullName
                    }))
                    .Finalize())
                .If(context => !context.Message.Success, x => x // Failure case - need to rollback AuthUser
                    .Then(context =>
                    {
                        context.Saga.ErrorMessage = $"User profile creation failed: {context.Message.ErrorMessage}";
                        _logger.LogError("User profile creation failed for {Email}: {Error}. Rolling back AuthUser {UserId}", 
                            context.Saga.Email, context.Message.ErrorMessage, context.Saga.AuthUserId);
                    })
                    .PublishAsync(context => context.Init<DeleteAuthUser>(new
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        UserId = context.Saga.AuthUserId,
                        Reason = "UserProfile creation failed"
                    }))
                    .TransitionTo(RollingBack)),
                
            // Ignore duplicate events
            Ignore(RegistrationStartedEvent),
            Ignore(OtpSentEvent),
            Ignore(OtpVerifiedEvent),
            Ignore(AuthUserCreatedEvent)
        );
        
        During(UserProfileCreated,
            // Ignore all events since workflow is complete - saga will auto-finalize
            Ignore(RegistrationStartedEvent),
            Ignore(OtpSentEvent),
            Ignore(OtpVerifiedEvent),
            Ignore(AuthUserCreatedEvent),
            Ignore(UserProfileCreatedEvent)
        );
        
        /*
        while state is RollingBack, saga will wait for AuthUserDeletedEvent from AuthService
        if successfully AuthUserDeletedEvent is received, then saga will transition to RolledBack state
        if failed AuthUserDeletedEvent is received, then saga will transition to Failed state
        */
        // Handle rollback process
        During(RollingBack,
            When(AuthUserDeletedEvent)
                .If(context => context.Message.Success, x => x
                    .Then(context =>
                    {
                        _logger.LogInformation("AuthUser {UserId} successfully deleted during rollback for {Email}", 
                            context.Message.UserId, context.Saga.Email);
                    })
                    .TransitionTo(RolledBack))
                .If(context => !context.Message.Success, x => x
                    .Then(context =>
                    {
                        context.Saga.ErrorMessage += $" | AuthUser deletion failed: {context.Message.ErrorMessage}";
                        _logger.LogError("Failed to delete AuthUser {UserId} during rollback for {Email}: {Error}", 
                            context.Message.UserId, context.Saga.Email, context.Message.ErrorMessage);

                        //Future: trigger background job to delete AppUser in AuthService
                    })
                    .TransitionTo(Failed)),
                
            // Ignore duplicate events during rollback - saga is focused on cleanup
            Ignore(RegistrationStartedEvent),
            Ignore(OtpSentEvent),
            Ignore(OtpVerifiedEvent),
            Ignore(AuthUserCreatedEvent),
            Ignore(UserProfileCreatedEvent)
        );
        
        During(Failed,
            // Ignore all events in Failed state - saga will auto-finalize
            Ignore(RegistrationStartedEvent),
            Ignore(OtpSentEvent),
            Ignore(OtpVerifiedEvent),
            Ignore(AuthUserCreatedEvent),
            Ignore(UserProfileCreatedEvent),
            Ignore(AuthUserDeletedEvent),
            Ignore(UserProfileDeletedEvent)
        );
        
        During(RolledBack,
            // Ignore all events in RolledBack state - saga will auto-finalize
            Ignore(RegistrationStartedEvent),
            Ignore(OtpSentEvent),
            Ignore(OtpVerifiedEvent),
            Ignore(AuthUserCreatedEvent),
            Ignore(UserProfileCreatedEvent),
            Ignore(AuthUserDeletedEvent),
            Ignore(UserProfileDeletedEvent)
        );
        
        // Cấu hình xóa completed sagas
        SetCompletedWhenFinalized();
    }

    // States
    public State Started { get; private set; } = null!;
    public State OtpSent { get; private set; } = null!;
    public State OtpVerified { get; private set; } = null!;
    public State AuthUserCreated { get; private set; } = null!;
    public State UserProfileCreated { get; private set; } = null!;
    public State Failed { get; private set; } = null!;
    public State RollingBack { get; private set; } = null!;
    public State RolledBack { get; private set; } = null!;
    
    // Events - Đổi tên để tránh conflict với States
    public Event<RegistrationStarted> RegistrationStartedEvent { get; private set; } = null!;
    public Event<OtpSent> OtpSentEvent { get; private set; } = null!;
    public Event<OtpVerified> OtpVerifiedEvent { get; private set; } = null!;
    public Event<AuthUserCreated> AuthUserCreatedEvent { get; private set; } = null!;
    public Event<UserProfileCreated> UserProfileCreatedEvent { get; private set; } = null!;
    
    // Compensating Events
    public Event<AuthUserDeleted> AuthUserDeletedEvent { get; private set; } = null!;
    public Event<UserProfileDeleted> UserProfileDeletedEvent { get; private set; } = null!;
    
    // TODO: Re-enable when RabbitMQ delayed message plugin is available
    // public Schedule<RegistrationSagaState, OtpTimeout> OtpTimeoutSchedule { get; private set; } = null!;
}

// TODO: Re-enable when RabbitMQ delayed message plugin is available
// /// <summary>
// /// Timeout event cho OTP verification
// /// </summary>
// public record OtpTimeout
// {
//     public Guid CorrelationId { get; init; }
// }
