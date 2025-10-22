using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Commons.Interfaces;
using PaymentService.Application.Commons.Helpers;
using PaymentService.Application.Commons.Models;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Contracts.Payment.Events;

namespace PaymentService.Application.Features.Payments.Commands.ProcessPayment;

/// <summary>
/// Process payment for subscription
/// Creates payment transaction and initiates payment with gateway
/// Returns gateway-specific response (MomoResponse, VnPayResponse, etc.) for frontend
/// </summary>
public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, Result<PaymentIntentResult>>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly IPaymentGatewayFactory _paymentGatewayFactory;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;

    public ProcessPaymentCommandHandler(
        IOutboxUnitOfWork unitOfWork,
        IPaymentGatewayFactory paymentGatewayFactory,
        IPublishEndpoint publishEndpoint,
        ILogger<ProcessPaymentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _paymentGatewayFactory = paymentGatewayFactory;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<Result<PaymentIntentResult>> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Processing payment for SubscriptionId: {SubscriptionId}, Amount: {Amount}",
                request.SubscriptionId, request.Amount);

            // 1. Validate payment method
            var paymentMethod = await _unitOfWork.Repository<PaymentMethod>()
                .GetFirstOrDefaultAsync(pm => pm.Id == request.PaymentMethodId && pm.Status == EntityStatusEnum.Active);

            if (paymentMethod == null || !Enum.IsDefined(typeof(PaymentGatewayType), paymentMethod.ProviderName))
            {
                _logger.LogWarning("Payment method not available: {PaymentMethodId}", request.PaymentMethodId);
                // Publish PaymentFailed event
                await _publishEndpoint.Publish<PaymentFailed>(new
                {
                    PaymentIntentId = Guid.Empty,
                    SubscriptionId = request.SubscriptionId,
                    Reason = "No payment method available",
                    ErrorMessage = "System configuration error: No active payment method"
                }, cancellationToken);
            
                return Result<PaymentIntentResult>.Failure("Payment method not found", ErrorCodeEnum.NotFound);
            }

            // 2. Create payment transaction record (initial state: Pending)
            var transaction = new PaymentTransaction
            {
                PaymentMethodId = request.PaymentMethodId,
                TransactionType = TransactionType.Subscription,
                ReferenceId = request.SubscriptionId,
                Amount = request.Amount,
                Currency = request.Currency,
                Status = EntityStatusEnum.Active,
            };
            transaction.InitializeEntity(request.CreatedBy);
            transaction.PaymentStatus = PayementStatus.Pending; // Pending payment

            // 3. Get payment gateway service from factory
            var gatewayType = Enum.Parse<PaymentGatewayType>(paymentMethod.ProviderName);
            var gateway = _paymentGatewayFactory.GetPaymentGatewayService(gatewayType);

            // 4. Build gateway-specific request using helper
            object gatewayRequest = PaymentGatewayRequestBuilder.BuildPaymentRequest(
                gatewayType,
                request.SubscriptionId,
                request.CreatedBy ?? Guid.Empty,
                request.PaymentMethodId,
                request.Amount,
                request.Currency,
                request.Description,
                request.Metadata,
                transaction,
                request.UserAgent); // ✅ Pass UserAgent

            // 5. Create payment intent with gateway
            // ✅ Returns gateway-specific response: MomoResponse, VnPayResponse, etc.
            var gatewayResult = await gateway.CreatePaymentIntentAsync(gatewayRequest, cancellationToken);

            // 6. Validate gateway response using helper
            var validationResult = PaymentGatewayResponseValidator.ValidateResponse(
                gatewayType, 
                gatewayResult, 
                _logger);
            
            if (!validationResult.IsValid)
            {
                _logger.LogError(
                    "Payment gateway failed: {ErrorCode} - {ErrorMessage}",
                    validationResult.ErrorCode, validationResult.ErrorMessage);

                transaction.PaymentStatus = PayementStatus.Failed;
                transaction.ErrorCode = validationResult.ErrorCode;
                transaction.ErrorMessage = validationResult.ErrorMessage;
                
                await _unitOfWork.Repository<PaymentTransaction>().AddAsync(transaction);
                
                await _publishEndpoint.Publish(new PaymentFailed
                {
                    PaymentIntentId = transaction.Id,
                    SubscriptionId = request.SubscriptionId,
                    Reason = "Gateway initialization failed",
                    ErrorCode = validationResult.ErrorCode,
                    ErrorMessage = validationResult.ErrorMessage
                }, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<PaymentIntentResult>.Failure("Failed to initialize payment", ErrorCodeEnum.InternalError);
            }

            // 7. Save transaction (TransactionId will be updated after payment callback)
            // ✅ CRITICAL: Don't set TransactionId here - it's the provider's transId, 
            // which we only get after payment is completed
            await _unitOfWork.Repository<PaymentTransaction>().AddAsync(transaction);

            // 8. Save all changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Payment transaction created: InternalTransactionId={TransactionId}, Gateway={Gateway}",
                transaction.Id, gatewayType);

            // ✅ Wrap response with internal transaction ID
            var result = new PaymentIntentResult
            {
                PaymentTransactionId = transaction.Id,  // ✅ Internal DB ID
                GatewayResponse = gatewayResult!         // Gateway-specific response (MomoResponse, etc.)
            };

            return Result<PaymentIntentResult>.Success(result, "Payment initiated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for SubscriptionId: {SubscriptionId}", request.SubscriptionId);
            return Result<PaymentIntentResult>.Failure("Failed to process payment", ErrorCodeEnum.InternalError);
        }
    }

}

