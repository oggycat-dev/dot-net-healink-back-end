using System.Linq.Expressions;
using SharedLibrary.Commons.Extensions;
using SubscriptionService.Application.Commons.DTOs;
using SubscriptionService.Domain.Entities;

namespace SubscriptionService.Application.Commons.QueryBuilders;

/// <summary>
/// Static query builder for SubscriptionPlan entity
/// Builds dynamic LINQ expressions for filtering and sorting
/// </summary>
public static class SubscriptionPlanQueryBuilder
{
    /// <summary>
    /// Build complete predicate from SubscriptionPlanFilter
    /// </summary>
    public static Expression<Func<SubscriptionPlan, bool>> BuildPredicate(this SubscriptionPlanFilter filter)
    {
        var predicate = PredicateBuilder.True<SubscriptionPlan>();

        // Base Status filter from BasePaginationFilter
        if (filter.Status.HasValue)
        {
            predicate = predicate.CombineAnd(x => x.Status == filter.Status.Value);
        }

        // Search predicate
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchPredicate = PredicateBuilder.False<SubscriptionPlan>();
            
            // Search in Name
            searchPredicate = searchPredicate.CombineOr<SubscriptionPlan>(
                x => x.Name.Contains(filter.Search));
            
            // Search in DisplayName
            searchPredicate = searchPredicate.CombineOr<SubscriptionPlan>(
                x => x.DisplayName.Contains(filter.Search));
            
            // Search in Description
            searchPredicate = searchPredicate.CombineOr<SubscriptionPlan>(
                x => x.Description.Contains(filter.Search));

            predicate = predicate.CombineAnd(searchPredicate);
        }

        // Custom filters

        if (filter.BillingPeriodUnit.HasValue)
        {
            predicate = predicate.CombineAnd(x => (int)x.BillingPeriodUnit == filter.BillingPeriodUnit.Value);
        }

        if (filter.MinAmount.HasValue)
        {
            predicate = predicate.CombineAnd(x => x.Amount >= filter.MinAmount.Value);
        }

        if (filter.MaxAmount.HasValue)
        {
            predicate = predicate.CombineAnd(x => x.Amount <= filter.MaxAmount.Value);
        }

        if (filter.HasTrialPeriod.HasValue)
        {
            if (filter.HasTrialPeriod.Value)
            {
                predicate = predicate.CombineAnd(x => x.TrialDays > 0);
            }
            else
            {
                predicate = predicate.CombineAnd(x => x.TrialDays == 0);
            }
        }

        if (filter.MinTrialDays.HasValue)
        {
            predicate = predicate.CombineAnd(x => x.TrialDays >= filter.MinTrialDays.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Currency))
        {
            predicate = predicate.CombineAnd(x => x.Currency == filter.Currency);
        }

        return predicate;
    }

    /// <summary>
    /// Build OrderBy expression from SubscriptionPlanFilter
    /// Default: CreatedAt descending (newest first)
    /// </summary>
    public static Expression<Func<SubscriptionPlan, object>> BuildOrderBy(this SubscriptionPlanFilter filter)
    {
        if (string.IsNullOrWhiteSpace(filter.SortBy))
            return x => x.CreatedAt;

        return filter.SortBy.ToLower() switch
        {
            "createdat" => x => x.CreatedAt,
            "updatedat" => x => x.UpdatedAt ?? x.CreatedAt,
            "name" => x => x.Name,
            "displayname" => x => x.DisplayName,
            "amount" => x => x.Amount,
            "billingperiodunit" => x => x.BillingPeriodUnit,
            "billingperiodcount" => x => x.BillingPeriodCount,
            "trialdays" => x => x.TrialDays,
            _ => x => x.CreatedAt
        };
    }
}
