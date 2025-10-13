using MediatR;
using Microsoft.Extensions.Logging;
using PaymentService.Domain.Entities;
using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Commons.Services;

namespace PaymentService.Application.Features.PaymentMethods.Commands.DeletePaymentMethod;

public class DeletePaymentMethodCommandHandler : IRequestHandler<DeletePaymentMethodCommand, Result>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeletePaymentMethodCommandHandler> _logger;

    public DeletePaymentMethodCommandHandler(
        IOutboxUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<DeletePaymentMethodCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeletePaymentMethodCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var repository = _unitOfWork.Repository<PaymentMethod>();

            // Find the payment method
            var existingMethod = await repository.GetFirstOrDefaultAsync(x => x.Id == request.Id);
            if (existingMethod == null)
            {
                _logger.LogWarning(
                    "Payment method with ID {Id} not found for deletion",
                    request.Id);

                return Result.Failure(
                    "Payment method not found",
                    ErrorCodeEnum.NotFound);
            }

            // Check if payment method has any transactions
            var transactionRepository = _unitOfWork.Repository<PaymentTransaction>();
            var hasTransactions = await transactionRepository.AnyAsync(
                x => x.PaymentMethodId == request.Id);

            if (hasTransactions)
            {
                _logger.LogWarning(
                    "Cannot delete payment method {Id} - has associated transactions",
                    request.Id);

                return Result.Failure(
                    "Cannot delete payment method. It has associated transactions.",
                    ErrorCodeEnum.ValidationFailed);
            }

            // Soft delete the payment method
            var userId = Guid.Parse(_currentUserService.UserId!);
            existingMethod.SoftDeleteEnitity(userId);
            repository.Update(existingMethod);

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Payment method {Id} soft deleted successfully by user {UserId}",
                request.Id,
                userId);

            return Result.Success("Payment method deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting payment method {Id}",
                request.Id);

            return Result.Failure(
                "An error occurred while deleting the payment method",
                ErrorCodeEnum.InternalError);
        }
    }
}

