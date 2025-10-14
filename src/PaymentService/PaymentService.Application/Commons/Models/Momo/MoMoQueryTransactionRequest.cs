namespace PaymentService.Application.Commons.Models.Momo;

public class MoMoQueryTransactionRequest
{
    public string PartnerCode { get; set; }
    public string RequestId { get; set; }
    public string OrderId { get; set; }
    public string Signature { get; set; }
    public string Lang { get; set; } = "vi";
}