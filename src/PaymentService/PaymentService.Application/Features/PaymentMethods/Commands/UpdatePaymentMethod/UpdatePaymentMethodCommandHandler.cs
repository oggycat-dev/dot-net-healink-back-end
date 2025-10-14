using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using PaymentService.Domain.Entities;
using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Commons.Services;

namespace PaymentService.Application.Features.PaymentMethods.Commands.UpdatePaymentMethod;

public class UpdatePaymentMethodCommandHandler : IRequestHandler<UpdatePaymentMethodCommand, Result>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdatePaymentMethodCommandHandler> _logger;

    public UpdatePaymentMethodCommandHandler(
        IOutboxUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<UpdatePaymentMethodCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(
        UpdatePaymentMethodCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating payment method: {Id}", request.Id);

            var repository = _unitOfWork.Repository<PaymentMethod>();

            // Get existing payment method
            var existingMethod = await repository.GetFirstOrDefaultAsync(x => x.Id == request.Id);
            if (existingMethod == null)
            {
                _logger.LogWarning("Payment method not found: {Id}", request.Id);
                return Result.Failure(
                    "Payment method not found",
                    ErrorCodeEnum.NotFound);
            }

            // Check if new name conflicts with another payment method
            if (existingMethod.Name != request.Request.Name)
            {
                var nameExists = await repository.AnyAsync(x => x.Name == request.Request.Name && x.Id != request.Id);
                if (nameExists)
                {
                    _logger.LogWarning("Payment method name already exists: {Name}", request.Request.Name);
                    return Result.Failure(
                        "Payment method with this name already exists",
                        ErrorCodeEnum.ResourceConflict);
                }
            }

            // Update fields from request using AutoMapper
            _mapper.Map(request.Request, existingMethod);
           
            // Update entity metadata
            var userId = Guid.Parse(_currentUserService.UserId!);
            existingMethod.UpdateEntity(userId);

            // Update in repository
            repository.Update(existingMethod);

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Payment method updated successfully: {Id}", existingMethod.Id);

            return Result.Success("Payment method updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment method: {Id}", request.Id);
            return Result.Failure(
                "Failed to update payment method",
                ErrorCodeEnum.InternalError);
        }
    }
}

