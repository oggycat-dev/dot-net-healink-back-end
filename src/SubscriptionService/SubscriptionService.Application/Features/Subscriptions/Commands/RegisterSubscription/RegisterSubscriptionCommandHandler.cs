using AutoMapper;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Cache;
using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Commons.Services;
using SharedLibrary.Contracts.Subscription.Events;
using SharedLibrary.Contracts.Payment.Requests;
using SharedLibrary.Contracts.Payment.Responses;
using SubscriptionService.Application.Commons.Services;
using SubscriptionService.Domain.Entities;

namespace SubscriptionService.Application.Features.Subscriptions.Commands.RegisterSubscription;

/// <summary>
/// Command handler for registering a new subscription
/// Uses Request-Response pattern for payment initialization
/// Returns PayUrl/QrCodeUrl for immediate frontend redirect
/// 
/// IMPORTANT: 
/// - UserId from JWT token is for AUTHENTICATION only
/// - UserProfileId from Redis cache is for BUSINESS LOGIC
/// </summary>
public class RegisterSubscriptionCommandHandler : IRequestHandler<RegisterSubscriptionCommand, Result<object>>
{
    private readonly IOutboxUnitOfWork _outboxUnitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IRequestClient<CreatePaymentIntentRequest> _paymentClient;
    private readonly IMapper _mapper;
    private readonly ILogger<RegisterSubscriptionCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IQrCodeService _qrCodeService;
    private readonly IUserStateCache _userStateCache;
    
    public RegisterSubscriptionCommandHandler(
        IOutboxUnitOfWork outboxUnitOfWork,
        IPublishEndpoint publishEndpoint,
        IRequestClient<CreatePaymentIntentRequest> paymentClient,
        IMapper mapper,
        ILogger<RegisterSubscriptionCommandHandler> logger,
        ICurrentUserService currentUserService,
        IQrCodeService qrCodeService,
        IUserStateCache userStateCache)
    {
        _outboxUnitOfWork = outboxUnitOfWork;
        _publishEndpoint = publishEndpoint;
        _paymentClient = paymentClient;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
        _qrCodeService = qrCodeService;
        _userStateCache = userStateCache;
    }
    
    public async Task<Result<object>> Handle(RegisterSubscriptionCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // ✅ Step 1: Get UserId from JWT token (for AUTHENTICATION only)
            var userIdStr = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var authUserId))
            {
                return Result<object>.Failure("User not authenticated", ErrorCodeEnum.Unauthorized);
            }

                // ✅ Step 2: Get UserProfileId from Redis cache (for BUSINESS LOGIC)
                _logger.LogInformation("Querying cache for UserId={UserId}", authUserId);
                var userState = await _userStateCache.GetUserStateAsync(authUserId);
                if (userState == null)
                {
                    _logger.LogWarning("❌ User state not found in cache for UserId={UserId}", authUserId);
                    return Result<object>.Failure("User session not found. Please login again.", ErrorCodeEnum.Unauthorized);
                }
                
                _logger.LogInformation(
                    "✅ Cache retrieved: UserId={UserId}, UserProfileId={UserProfileId}, Email={Email}, Status={Status}",
                    userState.UserId, userState.UserProfileId, userState.Email, userState.Status);

            if (!userState.IsActive)
            {
                _logger.LogWarning("User {UserId} is inactive. Status={Status}", authUserId, userState.Status);
                return Result<object>.Failure("User account is inactive", ErrorCodeEnum.Forbidden);
            }

            // ✅ UserProfileId from cache - THIS is used for business logic
            var userProfileId = userState.UserProfileId;

            if (userProfileId == Guid.Empty)
            {
                _logger.LogError("UserProfileId is empty for UserId={UserId}. User may not have a profile.", authUserId);
                return Result<object>.Failure("User profile not found. Please contact support.", ErrorCodeEnum.NotFound);
            }

            _logger.LogInformation(
                "Processing subscription registration: AuthUserId={AuthUserId}, UserProfileId={UserProfileId}",
                authUserId, userProfileId);

            // 3. Validate subscription plan exists (eager load for mapping)
            var plan = await _outboxUnitOfWork.Repository<SubscriptionPlan>()
                .GetFirstOrDefaultAsync(p => p.Id == command.Request.SubscriptionPlanId && p.Status == EntityStatusEnum.Active);
            
            if (plan == null)
            {
                return Result<object>.Failure("Subscription plan not found or inactive", ErrorCodeEnum.NotFound);
            }

            // 4. Check if user already has an active subscription (use UserProfileId)
            var existingActiveSubscription = await _outboxUnitOfWork.Repository<Subscription>()
                .GetFirstOrDefaultAsync(
                    s => s.UserProfileId == userProfileId && 
                         s.SubscriptionStatus == Domain.Enums.SubscriptionStatus.Active,
                    s => s.Plan);
            
            if (existingActiveSubscription != null)
            {
                // If subscribing to the SAME plan → reject
                if (existingActiveSubscription.SubscriptionPlanId == command.Request.SubscriptionPlanId)
                {
                    return Result<object>.Failure(
                        $"You are already subscribed to the '{existingActiveSubscription.Plan.DisplayName}' plan", 
                        ErrorCodeEnum.DuplicateEntry);
                }
                
                // If subscribing to a DIFFERENT plan → this is an upgrade/downgrade scenario
                _logger.LogWarning(
                    "UserProfileId={UserProfileId} attempted to upgrade from plan {OldPlanId} to {NewPlanId}. Upgrade flow not yet implemented.",
                    userProfileId, existingActiveSubscription.SubscriptionPlanId, command.Request.SubscriptionPlanId);
                
                return Result<object>.Failure(
                    $"You already have an active subscription to '{existingActiveSubscription.Plan.DisplayName}'. " +
                    $"Please cancel your current subscription before subscribing to a new plan. " +
                    $"Upgrade/downgrade feature is coming soon.",
                    ErrorCodeEnum.BusinessRuleViolation);
            }

            // 5. Create subscription entity (use UserProfileId for business logic)
            var subscription = new Subscription
            {
                UserProfileId = userProfileId, // ✅ Business logic uses UserProfileId
                SubscriptionPlanId = plan.Id,
                SubscriptionStatus = Domain.Enums.SubscriptionStatus.Pending,
                RenewalBehavior = Domain.Enums.RenewalBehavior.Manual,
                CancelAtPeriodEnd = false
                // ❌ DO NOT set Plan navigation property here - causes EF to try INSERT plan!
                // Navigation property will be populated by EF when querying later
            };
            subscription.InitializeEntity(authUserId); // ✅ CreatedBy = authUserId (JWT UserId) for audit

            await _outboxUnitOfWork.Repository<Subscription>().AddAsync(subscription);

            // 6. Custom Outbox - For Immediate User Activity Logging
            // This event is stored in custom OutboxEvent table
            // and published IMMEDIATELY via legacy RabbitMQ EventBus after transaction commits
            // ✅ Create manually because subscription.Plan is null (to avoid EF duplicate key error)
            var activityEvent = new SubscriptionRegisteredActivityEvent
            {
                SubscriptionId = subscription.Id,
                UserProfileId = subscription.UserProfileId,
                SubscriptionPlanId = subscription.SubscriptionPlanId,
                SubscriptionPlanName = plan.Name,
                SubscriptionPlanDisplayName = plan.DisplayName,
                Amount = plan.Amount,
                Currency = plan.Currency,
                ActivityType = "SubscriptionRegistered",
                Description = $"User registered for subscription plan: {plan.DisplayName}",
                CreatedBy = subscription.CreatedBy,
                CreatedAt = subscription.CreatedAt,
                CorrelationId = subscription.Id,
                IpAddress = _currentUserService.IpAddress,
                UserAgent = _currentUserService.UserAgent
            };
            await _outboxUnitOfWork.AddOutboxEventAsync(activityEvent);

            // 7. ATOMIC: Save subscription + custom outbox, then publish immediately
            // ✅ Use SaveChangesWithOutboxAsync() to publish custom outbox events immediately
            await _outboxUnitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

            _logger.LogInformation(
                "Subscription created. Now requesting payment intent: SubscriptionId={SubscriptionId}, UserProfileId={UserProfileId}, Amount={Amount}",
                subscription.Id, userProfileId, plan.Amount);

            // 8. ✅ REQUEST PAYMENT via RPC (synchronous - wait for response)
            // Frontend needs PayUrl/QrCodeUrl immediately for redirect
            var paymentRequest = new CreatePaymentIntentRequest
            {
                SubscriptionId = subscription.Id,
                UserProfileId = userProfileId, // ✅ Use UserProfileId
                PaymentMethodId = command.Request.PaymentMethodId,
                Amount = plan.Amount,
                Currency = plan.Currency,
                Description = $"Subscription to {plan.DisplayName}",
                Metadata = new Dictionary<string, string>
                {
                    ["subscriptionPlanId"] = plan.Id.ToString(),
                    ["subscriptionPlanName"] = plan.Name,
                    ["billingPeriodCount"] = plan.BillingPeriodCount.ToString(),
                    ["billingPeriodUnit"] = plan.BillingPeriodUnit.ToString()
                },
                CreatedBy = authUserId, // ✅ Use authUserId (JWT UserId) for audit
                UserAgent = _currentUserService.UserAgent // ✅ Pass UserAgent for client detection
            };

            // ✅ Use Request-Response pattern (timeout 30s)
            var paymentResponse = await _paymentClient.GetResponse<PaymentIntentCreated>(
                paymentRequest, 
                cancellationToken,
                RequestTimeout.After(s: 30));

            var paymentResult = paymentResponse.Message;

            if (!paymentResult.Success)
            {
                _logger.LogError(
                    "Payment intent creation failed for SubscriptionId={SubscriptionId}: {Error}",
                    subscription.Id, paymentResult.ErrorMessage);

                // ❌ Payment failed - return error to frontend
                return Result<object>.Failure(
                    paymentResult.ErrorMessage ?? "Failed to initialize payment",
                    ErrorCodeEnum.InternalError);
            }

            _logger.LogInformation(
                "Payment intent created successfully. SubscriptionId={SubscriptionId}, PaymentTransactionId={PaymentTransactionId}",
                subscription.Id, paymentResult.PaymentTransactionId);

            // 6.5. ✅ Generate QR Code from MoMo qrCodeUrl (if available)
            // Per MoMo docs: qrCodeUrl is DATA string, not image URL
            // Generate QR code image here to avoid sending large data through message queue
            string? qrCodeBase64 = null;
            if (!string.IsNullOrWhiteSpace(paymentResult.QrCodeUrl))
            {
                try
                {
                    qrCodeBase64 = _qrCodeService.GenerateQrCodeBase64(paymentResult.QrCodeUrl, pixelsPerModule: 10);
                    _logger.LogInformation(
                        "QR code generated successfully for SubscriptionId={SubscriptionId}",
                        subscription.Id);
                }
                catch (Exception qrEx)
                {
                    _logger.LogWarning(qrEx,
                        "Failed to generate QR code for SubscriptionId={SubscriptionId}. QR code will not be available.",
                        subscription.Id);
                    // Non-critical error - continue without QR code
                }
            }

            // 9. ✅ Publish Saga event for state tracking (async - fire and forget)
            // Saga will track payment status via callbacks
            var sagaEvent = new SubscriptionRegistrationStarted
            {
                SubscriptionId = subscription.Id, // Used as CorrelationId
                UserProfileId = userProfileId, // ✅ Use UserProfileId for business logic
                SubscriptionPlanId = plan.Id,
                PaymentMethodId = command.Request.PaymentMethodId,
                SubscriptionPlanName = plan.Name,
                Amount = plan.Amount,
                Currency = plan.Currency,
                // Capture HTTP context for saga workflow
                IpAddress = _currentUserService.IpAddress,
                UserAgent = _currentUserService.UserAgent,
                CreatedBy = authUserId // ✅ Use authUserId (JWT UserId) for cache query
            };
            await _publishEndpoint.Publish(sagaEvent, cancellationToken);

            // 8. ✅ Return payment data to frontend for redirect
            // ✅ Detect user agent to determine redirect URL
            var userAgent = _currentUserService.UserAgent ?? "";
            var isFlutterApp = userAgent.Contains("Flutter") || userAgent.Contains("Dart");
            var isMobileApp = userAgent.Contains("Mobile") || userAgent.Contains("Android") || userAgent.Contains("iOS");
            
            // ✅ Set appropriate redirect URL based on client type
            string redirectUrl;
            if (isFlutterApp || isMobileApp)
            {
                // ✅ Flutter app redirect - use custom scheme or deep link
                redirectUrl = "healink://payment/result"; // Custom scheme for Flutter
            }
            else
            {
                // ✅ Web app redirect - use web URL
                redirectUrl = "https://healink-omega.vercel.app/payment/result";
            }
            
            _logger.LogInformation(
                "Payment redirect configured: UserAgent={UserAgent}, IsFlutter={IsFlutter}, RedirectUrl={RedirectUrl}",
                userAgent, isFlutterApp, redirectUrl);

            return Result<object>.Success(
                new
                {
                    SubscriptionId = subscription.Id,
                    SubscriptionPlanName = plan.DisplayName,
                    Amount = plan.Amount,
                    Currency = plan.Currency,
                    // ✅ Payment redirect URLs
                    PaymentUrl = paymentResult.PaymentUrl,
                    DeepLink = paymentResult.DeepLink,
                    AppLink = paymentResult.AppLink,  // ✅ For in-app browser
                    PaymentTransactionId = paymentResult.PaymentTransactionId,
                    // ✅ Custom redirect URL based on client type
                    RedirectUrl = redirectUrl,
                    // ✅ QR Code (Base64-encoded PNG)
                    // Frontend can use: <img src="data:image/png;base64,{QrCodeBase64}" />
                    QrCodeBase64 = qrCodeBase64,
                    // ✅ Original MoMo qrCodeUrl (raw data) for reference
                    QrCodeDataUrl = paymentResult.QrCodeUrl
                },
                "Subscription registered successfully. Please complete payment.");
        }
        catch (RequestTimeoutException timeoutEx)
        {
            _logger.LogError(timeoutEx, "Payment request timeout for SubscriptionId");
            return Result<object>.Failure("Payment service timeout. Please try again.", ErrorCodeEnum.InternalError);
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database error while registering subscription");
            return Result<object>.Failure("Failed to create subscription", ErrorCodeEnum.InternalError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while registering subscription");
            return Result<object>.Failure("An error occurred while registering subscription", ErrorCodeEnum.InternalError);
        }
    }
}
