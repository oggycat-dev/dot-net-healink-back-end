using MediatR;
using PaymentService.Application.Commons.DTOs;
using SharedLibrary.Commons.Models;

namespace PaymentService.Application.Features.PaymentMethods.Queries.GetPaymentMethodById;

public record GetPaymentMethodByIdQuery(Guid Id) : IRequest<Result<PaymentMethodResponse>>;

