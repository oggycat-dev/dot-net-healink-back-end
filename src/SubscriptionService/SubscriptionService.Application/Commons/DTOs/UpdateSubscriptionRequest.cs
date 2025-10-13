using System.Text.Json.Serialization;
using SubscriptionService.Domain.Enums;

namespace SubscriptionService.Application.Commons.DTOs;

public class UpdateSubscriptionRequest
{
    [JsonPropertyName("subscriptionStatus")]
    public SubscriptionStatus? SubscriptionStatus { get; set; }
    
    [JsonPropertyName("renewalBehavior")]
    public RenewalBehavior? RenewalBehavior { get; set; }
    
    [JsonPropertyName("cancelAtPeriodEnd")]
    public bool? CancelAtPeriodEnd { get; set; }
    
    [JsonPropertyName("currentPeriodStart")]
    public DateTime? CurrentPeriodStart { get; set; }
    
    [JsonPropertyName("currentPeriodEnd")]
    public DateTime? CurrentPeriodEnd { get; set; }
}
