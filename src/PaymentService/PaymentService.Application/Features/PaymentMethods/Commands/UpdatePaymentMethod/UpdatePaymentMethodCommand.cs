using MediatR;
using PaymentService.Application.Commons.DTOs;
using SharedLibrary.Commons.Models;

namespace PaymentService.Application.Features.PaymentMethods.Commands.UpdatePaymentMethod;

public record UpdatePaymentMethodCommand(Guid Id, PaymentMethodRequest Request) : IRequest<Result>;

