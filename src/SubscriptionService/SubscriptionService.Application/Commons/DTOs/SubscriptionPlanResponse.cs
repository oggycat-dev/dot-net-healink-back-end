namespace SubscriptionService.Application.Commons.DTOs;

public class SubscriptionPlanResponse
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string DisplayName { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
    
    
    
    public string FeatureConfig { get; set; } = "{}";
    
    public string Currency { get; set; } = "VND";
    
    public int BillingPeriodCount { get; set; }
    
    public int BillingPeriodUnit { get; set; }
    
    public string BillingPeriodUnitName { get; set; } = string.Empty;
    
    public decimal Amount { get; set; }
    
    public int TrialDays { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
}
