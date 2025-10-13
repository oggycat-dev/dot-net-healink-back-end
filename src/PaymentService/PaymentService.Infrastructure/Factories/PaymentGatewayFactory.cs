using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application.Commons.Interfaces;
using PaymentService.Infrastructure.Services;
using SharedLibrary.Commons.Enums;

namespace PaymentService.Infrastructure.Factories;

public class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly IServiceProvider _serviceProvider;
    public PaymentGatewayFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    public IPaymentGatewayService GetPaymentGatewayService(PaymentGatewayType gatewayName)
    {
        return gatewayName switch
        {
            PaymentGatewayType.Momo => _serviceProvider.GetRequiredService<MomoService>(),
            // FUTURE: Add other payment gateways here
            // PaymentGatewayType.VnPay => new VnPayService(),
            // PaymentGatewayType.PayPal => new PayPalService(),
            _ => throw new ArgumentException("Invalid payment gateway type"),
        };
    }
}
