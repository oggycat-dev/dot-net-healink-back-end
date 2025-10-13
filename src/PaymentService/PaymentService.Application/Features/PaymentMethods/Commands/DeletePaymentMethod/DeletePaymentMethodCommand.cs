using MediatR;
using SharedLibrary.Commons.Models;

namespace PaymentService.Application.Features.PaymentMethods.Commands.DeletePaymentMethod;

public record DeletePaymentMethodCommand(Guid Id) : IRequest<Result>;

