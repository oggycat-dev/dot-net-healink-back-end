namespace PaymentService.Application.Commons.Models.Momo;

public class RefundTransResponse
{
    public string OrderId { get; set; }
    public long Amount { get; set; }
    public int ResultCode { get; set; }
    public long TransId { get; set; }
    public long CreatedAt { get; set; }
}