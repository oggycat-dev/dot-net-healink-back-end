using SharedLibrary.Commons.Entities;
using SubscriptionService.Domain.Enums;

namespace SubscriptionService.Domain.Entities;

public class Subscription : BaseEntity
{
    public Guid UserProfileId { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.InTrial;
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime? CancelAt { get; set; }
    public DateTime? CanceledAt { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public RenewalBehavior RenewalBehavior { get; set; } = RenewalBehavior.AutoRenew;
    public virtual SubscriptionPlan Plan { get; set; } = null!;
}
