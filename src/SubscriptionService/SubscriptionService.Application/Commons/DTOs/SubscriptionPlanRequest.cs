using System.Text.Json.Serialization;
using SharedLibrary.Commons.Enums;
using SubscriptionService.Domain.Enums;

namespace SubscriptionService.Application.Commons.DTOs;

public class SubscriptionPlanRequest
{
    public string Name { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? FeatureConfig { get; set; }
    public string Currency { get; set; } = null!;  
    public int BillingPeriodCount { get; set; } 
    public BillingPeriodUnit BillingPeriodUnit { get; set; }  
    public decimal Amount { get; set; }
    public int TrialDays { get; set; } 
    public EntityStatusEnum Status { get; set; }
}
