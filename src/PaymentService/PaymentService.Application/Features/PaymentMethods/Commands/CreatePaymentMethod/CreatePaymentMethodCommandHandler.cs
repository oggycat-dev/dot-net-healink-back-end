using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using PaymentService.Domain.Entities;
using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Commons.Services;

namespace PaymentService.Application.Features.PaymentMethods.Commands.CreatePaymentMethod;

public class CreatePaymentMethodCommandHandler : IRequestHandler<CreatePaymentMethodCommand, Result>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreatePaymentMethodCommandHandler> _logger;

    public CreatePaymentMethodCommandHandler(
        IOutboxUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<CreatePaymentMethodCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(
        CreatePaymentMethodCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating payment method: {@Request}", request.Request);

            var repository = _unitOfWork.Repository<PaymentMethod>();

            // Check if payment method name already exists
            var nameExists = await repository.AnyAsync(x => x.Name == request.Request.Name);
            if (nameExists)
            {
                _logger.LogWarning("Payment method name already exists: {Name}", request.Request.Name);
                return Result.Failure(
                    "Payment method with this name already exists",
                    ErrorCodeEnum.ResourceConflict);
            }

            // Map to entity
            var paymentMethod = _mapper.Map<PaymentMethod>(request.Request);
            
            // Initialize entity
            var currentUserID = Guid.Parse(_currentUserService.UserId!);
            paymentMethod.InitializeEntity(currentUserID);

            // Add to repository
            await repository.AddAsync(paymentMethod);

            // Save changes (no outbox event for payment methods - internal entity)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Payment method created successfully: {Id}", paymentMethod.Id);

            return Result.Success("Payment method created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment method");
            return Result.Failure(
                "Failed to create payment method",
                ErrorCodeEnum.InternalError);
        }
    }
}

