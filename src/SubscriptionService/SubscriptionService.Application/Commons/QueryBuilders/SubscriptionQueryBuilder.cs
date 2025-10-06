using System.Linq.Expressions;
using SharedLibrary.Commons.Extensions;
using SubscriptionService.Application.Commons.DTOs;
using SubscriptionService.Domain.Entities;
using SubscriptionService.Domain.Enums;

namespace SubscriptionService.Application.Commons.QueryBuilders;

/// <summary>
/// Static query builder for Subscription entity
/// Builds dynamic LINQ expressions for filtering and sorting
/// </summary>
public static class SubscriptionQueryBuilder
{
    /// <summary>
    /// Build complete predicate from SubscriptionFilter
    /// </summary>
    public static Expression<Func<Subscription, bool>> BuildPredicate(this SubscriptionFilter filter)
    {
        var predicate = PredicateBuilder.True<Subscription>();

        // Base Status filter from BasePaginationFilter
        if (filter.Status.HasValue)
        {
            predicate = predicate.CombineAnd(x => x.Status == filter.Status.Value);
        }

        // Search in Plan Name (via navigation property)
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchPredicate = PredicateBuilder.False<Subscription>();
            
            // Search in Plan Name
            searchPredicate = searchPredicate.CombineOr<Subscription>(
                x => x.Plan.Name.Contains(filter.Search));
            
            // Search in Plan DisplayName
            searchPredicate = searchPredicate.CombineOr<Subscription>(
                x => x.Plan.DisplayName.Contains(filter.Search));
            
            // Search in Plan Description
            searchPredicate = searchPredicate.CombineOr<Subscription>(
                x => x.Plan.Description.Contains(filter.Search));

            predicate = predicate.CombineAnd(searchPredicate);
        }

        // Custom filters
        if (filter.UserProfileId.HasValue)
        {
            predicate = predicate.CombineAnd(x => x.UserProfileId == filter.UserProfileId.Value);
        }

        if (filter.SubscriptionPlanId.HasValue)
        {
            predicate = predicate.CombineAnd(x => x.SubscriptionPlanId == filter.SubscriptionPlanId.Value);
        }

        if (filter.SubscriptionStatus.HasValue)
        {
            predicate = predicate.CombineAnd(x => (int)x.SubscriptionStatus == filter.SubscriptionStatus.Value);
        }

        if (filter.RenewalBehavior.HasValue)
        {
            predicate = predicate.CombineAnd(x => (int)x.RenewalBehavior == filter.RenewalBehavior.Value);
        }

        if (filter.IsActive.HasValue)
        {
            if (filter.IsActive.Value)
            {
                // Active: Status = Active and CurrentPeriodEnd > Now
                predicate = predicate.CombineAnd(x => 
                    x.SubscriptionStatus == SubscriptionStatus.Active &&
                    x.CurrentPeriodEnd != null &&
                    x.CurrentPeriodEnd > DateTime.UtcNow);
            }
            else
            {
                // Inactive: Status != Active or Expired
                predicate = predicate.CombineAnd(x => 
                    x.SubscriptionStatus != SubscriptionStatus.Active ||
                    x.CurrentPeriodEnd == null ||
                    x.CurrentPeriodEnd <= DateTime.UtcNow);
            }
        }

        if (filter.HasCancelScheduled.HasValue)
        {
            if (filter.HasCancelScheduled.Value)
            {
                predicate = predicate.CombineAnd(x => x.CancelAt != null || x.CancelAtPeriodEnd);
            }
            else
            {
                predicate = predicate.CombineAnd(x => x.CancelAt == null && !x.CancelAtPeriodEnd);
            }
        }

        // Date range filters
        if (filter.StartDate.HasValue)
        {
            predicate = predicate.CombineAnd(x => 
                x.CurrentPeriodStart != null &&
                x.CurrentPeriodStart >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            predicate = predicate.CombineAnd(x => 
                x.CurrentPeriodEnd != null &&
                x.CurrentPeriodEnd <= filter.EndDate.Value);
        }

        return predicate;
    }

    /// <summary>
    /// Build OrderBy expression from SubscriptionFilter
    /// Default: CreatedAt descending (newest first)
    /// </summary>
    public static Expression<Func<Subscription, object>> BuildOrderBy(this SubscriptionFilter filter)
    {
        if (string.IsNullOrWhiteSpace(filter.SortBy))
            return x => x.CreatedAt;

        return filter.SortBy.ToLower() switch
        {
            "createdat" => x => x.CreatedAt,
            "updatedat" => x => x.UpdatedAt ?? x.CreatedAt,
            "subscriptionstatus" => x => x.SubscriptionStatus,
            "currentperiodstart" => x => x.CurrentPeriodStart ?? DateTime.MinValue,
            "currentperiodend" => x => x.CurrentPeriodEnd ?? DateTime.MinValue,
            "planname" => x => x.Plan.Name,
            "plandisplayname" => x => x.Plan.DisplayName,
            "amount" => x => x.Plan.Amount,
            _ => x => x.CreatedAt
        };
    }

    /// <summary>
    /// Get includes for eager loading
    /// </summary>
    public static Expression<Func<Subscription, object>>[] GetIncludes()
    {
        return new Expression<Func<Subscription, object>>[]
        {
            x => x.Plan
        };
    }
}
