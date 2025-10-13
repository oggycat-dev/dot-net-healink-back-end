using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Features.Payments.Commands.ProcessPayment;
using PaymentService.Application.Commons.Helpers;
using SharedLibrary.Contracts.Payment.Requests;
using SharedLibrary.Contracts.Payment.Responses;
using SharedLibrary.Commons.Enums;

namespace PaymentService.Infrastructure.Consumers;

/// <summary>
/// Request-Response consumer for creating payment intent
/// Frontend needs immediate response with PayUrl/QrCodeUrl for redirect
/// This is synchronous - client waits for response
/// </summary>
public class CreatePaymentIntentConsumer : IConsumer<CreatePaymentIntentRequest>
{
    private readonly IMediator _mediator;
    private readonly ILogger<CreatePaymentIntentConsumer> _logger;

    public CreatePaymentIntentConsumer(
        IMediator mediator,
        ILogger<CreatePaymentIntentConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreatePaymentIntentRequest> context)
    {
        try
        {
            var request = context.Message;

            _logger.LogInformation(
                "Received CreatePaymentIntent request: SubscriptionId={SubscriptionId}, Amount={Amount}",
                request.SubscriptionId, request.Amount);

            // Delegate to CQRS command handler
            var command = new ProcessPaymentCommand
            {
                SubscriptionId = request.SubscriptionId,
                PaymentMethodId = request.PaymentMethodId,
                Amount = request.Amount,
                Currency = request.Currency,
                Description = request.Description,
                Metadata = request.Metadata,
                CreatedBy = request.CreatedBy
            };

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "Payment intent creation failed for SubscriptionId={SubscriptionId}: {Message}",
                    request.SubscriptionId, result.Message);

                // ✅ Respond with failure
                await context.RespondAsync(new PaymentIntentCreated
                {
                    Success = false,
                    Message = result.Message ?? "Failed to create payment intent",
                    SubscriptionId = request.SubscriptionId,
                    ErrorCode = "PAYMENT_INIT_FAILED",
                    ErrorMessage = result.Message
                });
                return;
            }

            // ✅ Extract gateway response from Result<object>.Data
            var gatewayResponse = result.Data;
            
            // Determine gateway type from payment method
            var gatewayType = GetGatewayTypeFromMetadata(request.Metadata);
            
            // Parse gateway-specific response using helper
            var paymentResponse = PaymentGatewayResponseParser.ParseToPaymentIntentCreated(
                gatewayType,
                gatewayResponse,
                request.SubscriptionId);

            _logger.LogInformation(
                "Payment intent created successfully for SubscriptionId={SubscriptionId}, Gateway={Gateway}",
                request.SubscriptionId, gatewayType);

            // ✅ Respond with success
            await context.RespondAsync(paymentResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error creating payment intent for SubscriptionId={SubscriptionId}",
                context.Message.SubscriptionId);
            
            // ✅ Respond with error
            await context.RespondAsync(new PaymentIntentCreated
            {
                Success = false,
                Message = "Internal error while creating payment intent",
                SubscriptionId = context.Message.SubscriptionId,
                ErrorCode = "INTERNAL_ERROR",
                ErrorMessage = ex.Message
            });
        }
    }

    /// <summary>
    /// Determine gateway type from metadata
    /// Default to MoMo for now
    /// </summary>
    private PaymentGatewayType GetGatewayTypeFromMetadata(Dictionary<string, string>? metadata)
    {
        // FUTURE: Get from payment method or metadata
        // For now, default to MoMo
        // Later: read from PaymentMethod.ProviderName
        return PaymentGatewayType.Momo;
    }
}

