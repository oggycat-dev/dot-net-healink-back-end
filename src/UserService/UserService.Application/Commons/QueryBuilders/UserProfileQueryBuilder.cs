using System.Linq.Expressions;
using SharedLibrary.Commons.Extensions;
using UserService.Application.Commons.DTOs;
using UserService.Domain.Entities;

namespace UserService.Application.Commons.QueryBuilders;

/// <summary>
/// Static query builder for UserProfile entity
/// Builds dynamic LINQ expressions for filtering and sorting
/// </summary>
public static class UserProfileQueryBuilder
{
    /// <summary>
    /// Build complete predicate from UserProfileFilter
    /// </summary>
    public static Expression<Func<UserProfile, bool>> BuildPredicate(this UserProfileFilter filter)
    {
        var predicate = PredicateBuilder.True<UserProfile>();

        // Base Status filter from BasePaginationFilter
        if (filter.Status.HasValue)
        {
            predicate = predicate.CombineAnd(x => x.Status == filter.Status.Value);
        }

        // Search in FullName, Email, PhoneNumber
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchPredicate = PredicateBuilder.False<UserProfile>();
            
            // Search in FullName
            searchPredicate = searchPredicate.CombineOr<UserProfile>(
                x => x.FullName.Contains(filter.Search));
            
            // Search in Email
            searchPredicate = searchPredicate.CombineOr<UserProfile>(
                x => x.Email.Contains(filter.Search));
            
            // Search in PhoneNumber
            searchPredicate = searchPredicate.CombineOr<UserProfile>(
                x => x.PhoneNumber.Contains(filter.Search));

            predicate = predicate.CombineAnd(searchPredicate);
        }

        // Custom filters
        if (filter.UserId.HasValue)
        {
            predicate = predicate.CombineAnd(x => x.UserId == filter.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Email))
        {
            predicate = predicate.CombineAnd(x => x.Email.Contains(filter.Email));
        }

        if (!string.IsNullOrWhiteSpace(filter.FullName))
        {
            predicate = predicate.CombineAnd(x => x.FullName.Contains(filter.FullName));
        }

        if (!string.IsNullOrWhiteSpace(filter.PhoneNumber))
        {
            predicate = predicate.CombineAnd(x => x.PhoneNumber.Contains(filter.PhoneNumber));
        }

        // Date range filters - Last Login
        if (filter.LastLoginFrom.HasValue)
        {
            predicate = predicate.CombineAnd(x => 
                x.LastLoginAt != null &&
                x.LastLoginAt >= filter.LastLoginFrom.Value);
        }

        if (filter.LastLoginTo.HasValue)
        {
            predicate = predicate.CombineAnd(x => 
                x.LastLoginAt != null &&
                x.LastLoginAt <= filter.LastLoginTo.Value);
        }

        // Date range filters - Created At
        if (filter.CreatedFrom.HasValue)
        {
            predicate = predicate.CombineAnd(x => 
                x.CreatedAt != null &&
                x.CreatedAt >= filter.CreatedFrom.Value);
        }

        if (filter.CreatedTo.HasValue)
        {
            predicate = predicate.CombineAnd(x => 
                x.CreatedAt != null &&
                x.CreatedAt <= filter.CreatedTo.Value);
        }

        return predicate;
    }

    /// <summary>
    /// Build OrderBy expression from UserProfileFilter
    /// Default: CreatedAt descending (newest first)
    /// </summary>
    public static Expression<Func<UserProfile, object>> BuildOrderBy(this UserProfileFilter filter)
    {
        if (string.IsNullOrWhiteSpace(filter.SortBy))
            return x => x.CreatedAt!;

        return filter.SortBy.ToLower() switch
        {
            "createdat" => x => x.CreatedAt!,
            "updatedat" => x => x.UpdatedAt ?? x.CreatedAt!,
            "fullname" => x => x.FullName,
            "email" => x => x.Email,
            "phonenumber" => x => x.PhoneNumber,
            "lastloginat" => x => x.LastLoginAt ?? DateTime.MinValue,
            "status" => x => x.Status,
            _ => x => x.CreatedAt!
        };
    }

    /// <summary>
    /// Get includes for eager loading (UserProfile has no navigation properties to load)
    /// </summary>
    public static Expression<Func<UserProfile, object>>[] GetIncludes()
    {
        return Array.Empty<Expression<Func<UserProfile, object>>>();
    }
}
