using SharedLibrary.Commons.Models;

namespace SubscriptionService.Application.Commons.DTOs;

/// <summary>
/// Filter for subscription plan list with pagination
/// </summary>
public class SubscriptionPlanFilter : BasePaginationFilter
{
    public int? BillingPeriodUnit { get; set; }
    
    public decimal? MinAmount { get; set; }
    
    public decimal? MaxAmount { get; set; }
    
    public bool? HasTrialPeriod { get; set; }
    
    public int? MinTrialDays { get; set; }
    
    public string? Currency { get; set; }
}
