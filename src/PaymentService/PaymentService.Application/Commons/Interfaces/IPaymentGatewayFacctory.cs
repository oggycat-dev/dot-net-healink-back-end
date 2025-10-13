using SharedLibrary.Commons.Enums;

namespace PaymentService.Application.Commons.Interfaces;

public interface IPaymentGatewayFactory
{
    IPaymentGatewayService GetPaymentGatewayService(PaymentGatewayType gatewayName);
}
