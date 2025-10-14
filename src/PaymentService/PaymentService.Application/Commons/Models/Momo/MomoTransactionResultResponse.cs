using System.Text.Json.Nodes;

namespace PaymentService.Application.Commons.Models.Momo;

public class MoMoTransactionResultResponse
{
    public string PartnerCode { get; set; }
    public string RequestId { get; set; }
    public string OrderId { get; set; }
    public string ExtraData { get; set; }
    public long Amount { get; set; }
    public long TransId { get; set; }
    public string PayType { get; set; }
    public int ResultCode { get; set; }
    public List<RefundTransResponse> RefundTrans { get; set; } = new();
    public string Message { get; set; }
    public long ResponseTime { get; set; }
}