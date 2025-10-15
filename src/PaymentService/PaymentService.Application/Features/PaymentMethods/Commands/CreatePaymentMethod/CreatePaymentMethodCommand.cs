using MediatR;
using PaymentService.Application.Commons.DTOs;
using SharedLibrary.Commons.Models;

namespace PaymentService.Application.Features.PaymentMethods.Commands.CreatePaymentMethod;

public record CreatePaymentMethodCommand(PaymentMethodRequest Request) : IRequest<Result>;

