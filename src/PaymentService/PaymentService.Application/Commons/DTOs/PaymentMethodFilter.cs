using PaymentService.Domain.Enums;
using SharedLibrary.Commons.Models;

namespace PaymentService.Application.Commons.DTOs;

public class PaymentMethodFilter : BasePaginationFilter
{
    public string? Name { get; set; }
    public PaymentType? Type { get; set; }
    public string? ProviderName { get; set; }
}

