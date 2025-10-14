using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Commons.DTOs;
using PaymentService.Domain.Entities;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Repositories;

namespace PaymentService.Application.Features.PaymentMethods.Queries.GetPaymentMethodById;

public class GetPaymentMethodByIdQueryHandler : IRequestHandler<GetPaymentMethodByIdQuery, Result<PaymentMethodResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetPaymentMethodByIdQueryHandler> _logger;

    public GetPaymentMethodByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetPaymentMethodByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<PaymentMethodResponse>> Handle(
        GetPaymentMethodByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting payment method by ID: {Id}", request.Id);

            var repository = _unitOfWork.Repository<PaymentMethod>();
            var method = await repository.GetFirstOrDefaultAsync(x => x.Id == request.Id);

            if (method == null)
            {
                _logger.LogWarning("Payment method not found: {Id}", request.Id);
                return Result<PaymentMethodResponse>.Failure(
                    "Payment method not found",
                    ErrorCodeEnum.NotFound);
            }

            var response = _mapper.Map<PaymentMethodResponse>(method);

            return Result<PaymentMethodResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment method by ID: {Id}", request.Id);
            return Result<PaymentMethodResponse>.Failure(
                "Failed to retrieve payment method",
                ErrorCodeEnum.InternalError);
        }
    }
}

