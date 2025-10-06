using SharedLibrary.Commons.Models;

namespace SubscriptionService.Application.Commons.DTOs;

/// <summary>
/// Filter for subscription list with pagination
/// </summary>
public class SubscriptionFilter : BasePaginationFilter
{
    public Guid? UserProfileId { get; set; }
    
    public Guid? SubscriptionPlanId { get; set; }
    
    public int? SubscriptionStatus { get; set; }
    
    public int? RenewalBehavior { get; set; }
    
    public bool? IsActive { get; set; }
    
    public bool? HasCancelScheduled { get; set; }
    
    public DateTime? StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
}
