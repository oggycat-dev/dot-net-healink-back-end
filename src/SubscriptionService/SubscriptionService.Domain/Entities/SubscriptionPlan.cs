using SharedLibrary.Commons.Entities;
using SubscriptionService.Domain.Enums;

namespace SubscriptionService.Domain.Entities;

public class SubscriptionPlan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string FeatureConfig { get; set; } = "{}"; // jsonb
    public string Currency { get; set; } = "VND";
    public int BillingPeriodCount { get; set; } = 1;
    public BillingPeriodUnit BillingPeriodUnit { get; set; } = BillingPeriodUnit.Month;
    public decimal Amount { get; set; }
    public int TrialDays { get; set; } = 0;
}
