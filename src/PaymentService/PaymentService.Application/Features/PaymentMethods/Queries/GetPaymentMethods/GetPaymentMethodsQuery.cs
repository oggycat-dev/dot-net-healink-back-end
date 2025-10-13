using MediatR;
using PaymentService.Application.Commons.DTOs;
using SharedLibrary.Commons.Models;

namespace PaymentService.Application.Features.PaymentMethods.Queries.GetPaymentMethods;

public record GetPaymentMethodsQuery(PaymentMethodFilter Filter) : IRequest<PaginationResult<PaymentMethodResponse>>;

