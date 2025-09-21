using MassTransit;
using Microsoft.Extensions.Logging;
using SharedLibrary.Contracts.User.Events;
using SharedLibrary.Commons.Enums;

namespace SharedLibrary.Contracts.User.Saga;

/// <summary>
/// Saga quản lý workflow đăng ký user với OTP verification
/// Quản lý distributed transaction từ AuthService qua UserService
/// </summary>
public class RegistrationSaga : MassTransitStateMachine<RegistrationSagaState>
{
    private readonly ILogger<RegistrationSaga> _logger;

    public RegistrationSaga(ILogger<RegistrationSaga> logger)
    {
        _logger = logger;
        
        InstanceState(x => x.CurrentState);
        
        // Cấu hình Events
        Event(() => RegistrationStartedEvent, x => x.CorrelateById(context => context.Message.CorrelationId));
        Event(() => OtpSentEvent, x => x.CorrelateById(context => context.Message.CorrelationId));
        Event(() => OtpVerifiedEvent, x => x.CorrelateById(context => context.Message.CorrelationId));
        Event(() => AuthUserCreatedEvent, x => x.CorrelateById(context => context.Message.CorrelationId));
        Event(() => UserProfileCreatedEvent, x => x.CorrelateById(context => context.Message.CorrelationId));
        
        // Cấu hình Timeout Schedule
        Schedule(() => OtpTimeoutSchedule, instance => instance.OtpTimeoutTokenId, s =>
        {
            s.Delay = TimeSpan.FromMinutes(5); // OTP hết hạn sau 5 phút
            s.Received = e => e.CorrelateById(context => context.Message.CorrelationId);
        });
        
        // Định nghĩa workflow
        Initially(
            When(RegistrationStartedEvent)
                .Then(context =>
                {
                    context.Saga.CorrelationId = context.Message.CorrelationId;
                    context.Saga.Email = context.Message.Email;
                    context.Saga.FullName = context.Message.FullName;
                    context.Saga.Channel = context.Message.Channel;
                    context.Saga.StartedAt = DateTime.UtcNow;
                    
                    _logger.LogInformation("Registration saga started for {Email}", context.Message.Email);
                })
                .PublishAsync(context => context.Init<GenerateOtp>(new
                {
                    CorrelationId = context.Saga.CorrelationId,
                    UserId = context.Saga.CorrelationId, // Dùng CorrelationId làm UserId tạm
                    Contact = context.Message.Channel == NotificationChannelEnum.Email 
                        ? context.Message.Email 
                        : context.Message.PhoneNumber,
                    Channel = context.Message.Channel,
                    Purpose = "Registration Verification",
                    OtpType = OtpTypeEnum.Registration,
                    ExpiryMinutes = 5
                }))
                .Schedule(OtpTimeoutSchedule, context => context.Init<OtpTimeout>(new
                {
                    CorrelationId = context.Saga.CorrelationId
                }))
                .TransitionTo(Started)
        );
        
        During(Started,
            When(OtpSentEvent)
                .Then(context =>
                {
                    context.Saga.OtpSentAt = DateTime.UtcNow;
                    _logger.LogInformation("OTP sent for registration {Email}", context.Saga.Email);
                })
                .TransitionTo(OtpSent),
                
            When(OtpTimeoutSchedule.Received)
                .Then(context =>
                {
                    context.Saga.ErrorMessage = "OTP generation timeout";
                    _logger.LogWarning("OTP generation timeout for {Email}", context.Saga.Email);
                })
                .TransitionTo(Failed)
        );
        
        During(OtpSent,
            When(OtpVerifiedEvent)
                .Then(context =>
                {
                    context.Saga.OtpVerifiedAt = DateTime.UtcNow;
                    _logger.LogInformation("OTP verified for registration {Email}", context.Saga.Email);
                })
                .Unschedule(OtpTimeoutSchedule)
                .PublishAsync(context => context.Init<CreateAuthUser>(new
                {
                    CorrelationId = context.Saga.CorrelationId,
                    Email = context.Saga.Email,
                    FullName = context.Saga.FullName
                }))
                .TransitionTo(OtpVerified),
                
            When(OtpTimeoutSchedule.Received)
                .Then(context =>
                {
                    context.Saga.ErrorMessage = "OTP verification timeout";
                    _logger.LogWarning("OTP verification timeout for {Email}", context.Saga.Email);
                })
                .TransitionTo(Failed)
        );
        
        During(OtpVerified,
            When(AuthUserCreatedEvent)
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
                    FullName = context.Saga.FullName
                }))
                .TransitionTo(AuthUserCreated),
                
            When(OtpTimeoutSchedule.Received)
                .Then(context =>
                {
                    context.Saga.ErrorMessage = "Auth user creation timeout";
                    _logger.LogWarning("Auth user creation timeout for {Email}", context.Saga.Email);
                })
                .TransitionTo(Failed)
        );
        
        During(AuthUserCreated,
            When(UserProfileCreatedEvent)
                .Then(context =>
                {
                    context.Saga.UserProfileId = context.Message.UserProfileId;
                    context.Saga.UserProfileCreatedAt = DateTime.UtcNow;
                    context.Saga.CompletedAt = DateTime.UtcNow;
                    _logger.LogInformation("User profile created for {Email} with ProfileID {ProfileId}", 
                        context.Saga.Email, context.Message.UserProfileId);
                })
                .TransitionTo(UserProfileCreated),
                
            When(OtpTimeoutSchedule.Received)
                .Then(context =>
                {
                    context.Saga.ErrorMessage = "User profile creation timeout";
                    _logger.LogWarning("User profile creation timeout for {Email}", context.Saga.Email);
                })
                .TransitionTo(Failed)
        );
        
        During(UserProfileCreated,
            Ignore(RegistrationStartedEvent),
            Ignore(OtpSentEvent),
            Ignore(OtpVerifiedEvent),
            Ignore(AuthUserCreatedEvent),
            Ignore(UserProfileCreatedEvent)
        );
        
        During(Failed,
            Ignore(RegistrationStartedEvent),
            Ignore(OtpSentEvent),
            Ignore(OtpVerifiedEvent),
            Ignore(AuthUserCreatedEvent),
            Ignore(UserProfileCreatedEvent),
            Ignore(OtpTimeoutSchedule.Received)
        );
        
        DuringAny(
            When(RegistrationStartedEvent)
                .Then(context =>
                {
                    _logger.LogWarning("Duplicate registration attempt for {Email} in state {State}",
                        context.Message.Email, context.Saga.CurrentState);
                })
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
    
    // Events - Đổi tên để tránh conflict với States
    public Event<RegistrationStarted> RegistrationStartedEvent { get; private set; } = null!;
    public Event<OtpSent> OtpSentEvent { get; private set; } = null!;
    public Event<OtpVerified> OtpVerifiedEvent { get; private set; } = null!;
    public Event<AuthUserCreated> AuthUserCreatedEvent { get; private set; } = null!;
    public Event<UserProfileCreated> UserProfileCreatedEvent { get; private set; } = null!;
    
    // Schedules
    public Schedule<RegistrationSagaState, OtpTimeout> OtpTimeoutSchedule { get; private set; } = null!;
}

/// <summary>
/// Timeout event cho OTP verification
/// </summary>
public record OtpTimeout
{
    public Guid CorrelationId { get; init; }
}