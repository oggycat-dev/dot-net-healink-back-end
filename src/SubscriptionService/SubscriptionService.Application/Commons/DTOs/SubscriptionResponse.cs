namespace SubscriptionService.Application.Commons.DTOs;

public class SubscriptionResponse
{
    public Guid Id { get; set; }
    
    public Guid UserProfileId { get; set; }
    
    public Guid SubscriptionPlanId { get; set; }
    
    public string PlanName { get; set; } = string.Empty;
    
    public string PlanDisplayName { get; set; } = string.Empty;
    
    public int SubscriptionStatus { get; set; }
    
    public string SubscriptionStatusName { get; set; } = string.Empty;
    
    public DateTime? CurrentPeriodStart { get; set; }
    
    public DateTime? CurrentPeriodEnd { get; set; }
    
    public DateTime? CancelAt { get; set; }
    
    public DateTime? CanceledAt { get; set; }
    
    public bool CancelAtPeriodEnd { get; set; }
    
    public int RenewalBehavior { get; set; }

    public string RenewalBehaviorName { get; set; } = string.Empty;
    
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;

    public string BillingPeriodUnit { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
}
