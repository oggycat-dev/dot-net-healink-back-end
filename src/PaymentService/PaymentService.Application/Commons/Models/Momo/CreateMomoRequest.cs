namespace PaymentService.Application.Commons.Models.Momo;

public class CreateMomoRequest
{
    public string PartnerCode { get; set; }
    public string PartnerName { get; set; }
    public string StoreId { get; set; }
    public string RequestType { get; set; }
    public string IpnUrl { get; set; }
    public string RedirectUrl { get; set; }
    public string OrderId { get; set; }
    public long Amount { get; set; }
    public string Lang { get; set; } = "vi";
    public string OrderInfo { get; set; }
    public string RequestId { get; set; }
    public string ExtraData { get; set; }
    public string Signature { get; set; }
}