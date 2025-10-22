using MassTransit;
using Microsoft.Extensions.Logging;
using SharedLibrary.Contracts.Subscription.Events;
using SharedLibrary.Contracts.Subscription.Commands;
using SharedLibrary.Contracts.Payment.Events;
using MassTransit.RabbitMqTransport;
using SharedLibrary.Commons.Enums;

namespace SubscriptionService.Infrastructure.Saga;

/// <summary>
/// Subscription Registration Saga State Machine
/// Orchestrates the subscription workflow: Registration → Payment → Activation
/// Similar to RegistrationSaga but with MassTransit Entity Framework Outbox
/// </summary>
public class RegisterSubscriptionSaga : MassTransitStateMachine<RegisterSubscriptionSagaState>
{
    private readonly ILogger<RegisterSubscriptionSaga> _logger;

    public RegisterSubscriptionSaga(ILogger<RegisterSubscriptionSaga> logger)
    {
        _logger = logger;

        // CRITICAL: Use SubscriptionId as CorrelationId for unified tracking
        InstanceState(x => x.CurrentState);

        // Define states
        Event(() => SubscriptionRegistrationStarted, x => x.CorrelateById(m => m.Message.SubscriptionId));
        Event(() => PaymentSucceeded, x => x.CorrelateById(m => m.Message.SubscriptionId));
        Event(() => PaymentFailed, x => x.CorrelateById(m => m.Message.SubscriptionId));

        Initially(
            When(SubscriptionRegistrationStarted)
                .Then(context =>
                {
                    // Initialize saga state
                    context.Saga.CorrelationId = context.Message.SubscriptionId;
                    context.Saga.UserProfileId = context.Message.UserProfileId;
                    context.Saga.SubscriptionPlanId = context.Message.SubscriptionPlanId;
                    context.Saga.PaymentMethodId = context.Message.PaymentMethodId;
                    context.Saga.SubscriptionPlanName = context.Message.SubscriptionPlanName;
                    context.Saga.Amount = context.Message.Amount;
                    context.Saga.Currency = context.Message.Currency;
                    context.Saga.StartedAt = DateTime.UtcNow;
                    context.Saga.PaymentRequestedAt = DateTime.UtcNow;
                    context.Saga.CreatedBy = context.Message.CreatedBy;
                    _logger.LogInformation(
                        "Subscription saga started for SubscriptionId: {SubscriptionId}, User: {UserId}, Plan: {PlanName}",
                        context.Saga.CorrelationId, context.Saga.UserProfileId, context.Saga.SubscriptionPlanName);
                })
                // Send payment request to PaymentService
                .PublishAsync(context => context.Init<RequestPayment>(new
                {
                    SubscriptionId = context.Saga.CorrelationId,
                    UserProfileId = context.Saga.UserProfileId,
                    PaymentMethodId = context.Saga.PaymentMethodId,
                    Amount = context.Saga.Amount,
                    Currency = context.Saga.Currency,
                    Description = $"Subscription to {context.Saga.SubscriptionPlanName}",
                    Metadata = new Dictionary<string, string>
                    {
                        ["SubscriptionPlanId"] = context.Saga.SubscriptionPlanId.ToString(),
                        ["SubscriptionPlanName"] = context.Saga.SubscriptionPlanName ?? ""
                    },
                    UserAgent = context.Message.UserAgent, // ✅ Pass UserAgent for client detection
                    CreatedBy = context.Saga.CreatedBy
                }))
                .TransitionTo(AwaitingPayment)
        );

        During(AwaitingPayment,
            When(PaymentSucceeded)
                .Then(context =>
                {
                    context.Saga.PaymentIntentId = context.Message.PaymentIntentId;
                    context.Saga.PaymentStatus = PayementStatus.Succeeded.ToString();
                    context.Saga.PaymentProvider = context.Message.PaymentProvider;
                    context.Saga.TransactionId = context.Message.TransactionId; 
                    context.Saga.PaymentCompletedAt = DateTime.UtcNow;
                    context.Saga.IsPaymentCompleted = true;

                    _logger.LogInformation(
                        "Payment succeeded for SubscriptionId: {SubscriptionId}, PaymentIntentId: {PaymentIntentId}",
                        context.Saga.CorrelationId, context.Saga.PaymentIntentId);
                })
                // ✅ Activate subscription after successful payment
                .PublishAsync(context => context.Init<ActivateSubscription>(new
                {
                    SubscriptionId = context.Saga.CorrelationId,
                    PaymentIntentId = context.Saga.PaymentIntentId,
                    PaymentProvider = context.Saga.PaymentProvider,
                    TransactionId = context.Saga.TransactionId,
                    UpdatedBy = context.Saga.CreatedBy
                }))
                .TransitionTo(PaymentCompleted)
                .Finalize(),

            When(PaymentFailed)
                .Then(context =>
                {
                    context.Saga.PaymentIntentId = context.Message.PaymentIntentId;
                    context.Saga.PaymentStatus = PayementStatus.Failed.ToString();
                    context.Saga.ErrorMessage = context.Message.ErrorMessage ?? context.Message.Reason;
                    context.Saga.FailedAt = DateTime.UtcNow;
                    context.Saga.IsFailed = true;

                    _logger.LogError(
                        "Payment failed for SubscriptionId: {SubscriptionId}, Reason: {Reason}",
                        context.Saga.CorrelationId, context.Message.Reason);
                })
                // ✅ COMPENSATION: Rollback subscription when payment fails
                .PublishAsync(context => context.Init<CancelSubscription>(new
                {
                    SubscriptionId = context.Saga.CorrelationId,
                    Reason = $"Payment failed: {context.Message.Reason}",
                    IsCompensation = true, // Mark as compensation/rollback
                    UpdatedBy = context.Saga.CreatedBy
                }))
                .TransitionTo(Failed)
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }

    // States
    public State AwaitingPayment { get; private set; } = null!;
    public State PaymentCompleted { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    // Events
    public Event<SubscriptionRegistrationStarted> SubscriptionRegistrationStarted { get; private set; } = null!;
    public Event<PaymentSucceeded> PaymentSucceeded { get; private set; } = null!;
    public Event<PaymentFailed> PaymentFailed { get; private set; } = null!;
}