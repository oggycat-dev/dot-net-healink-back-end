using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ProductAuthMicroservice.Commons.Extensions;
using ProductAuthMicroservice.ProductService.Application.Features.Categories.DTOs;
using ProductAuthMicroservice.ProductService.Domain.Entities;

namespace ProductAuthMicroservice.ProductService.Application.Features.Categories.QueryBuilders;

/// <summary>
/// Extension methods cho CategoryFilter để build query trực tiếp
/// Không cần inject QueryBuilder vào CQRS handlers
/// </summary>
public static class CategoryQueryBuilder
{
    /// <summary>
    /// Build complete predicate từ CategoryFilter
    /// </summary>
    public static Expression<Func<Category, bool>> BuildPredicate(this CategoryFilter filter)
    {
        var predicate = PredicateBuilder.True<Category>();

        // Base filters (từ BaseEntity)
        predicate = predicate.CombineAnd(x => !x.IsDeleted);

        // Status filter từ BasePaginationFilter
        if (filter.Status.HasValue)
        {
            predicate = predicate.CombineAnd(x => x.Status == filter.Status.Value);
        }

        // Search predicate
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchPredicate = PredicateBuilder.False<Category>();
            
            // Search trong Name
            searchPredicate = searchPredicate.CombineOr(x => x.Name.Contains(filter.Search));
            
            // Search trong Description
            searchPredicate = searchPredicate.CombineOr(x => x.Description.Contains(filter.Search));

            predicate = predicate.CombineAnd(searchPredicate);
        }

        // Parent category filter
        if (filter.ParentCategoryId.HasValue)
        {
            predicate = predicate.CombineAnd(x => x.ParentCategoryId == filter.ParentCategoryId.Value);
        }
        else if (filter.RootCategoriesOnly)
        {
            predicate = predicate.CombineAnd(x => x.ParentCategoryId == null);
        }

        return predicate;
    }

    /// <summary>
    /// Build OrderBy expression từ CategoryFilter
    /// Default: Name tăng dần (alphabetical)
    /// </summary>
    public static Expression<Func<Category, object>> BuildOrderBy(this CategoryFilter filter)
    {
        if (string.IsNullOrWhiteSpace(filter.SortBy))
            return x => x.Name; // Default: Name tăng dần

        return filter.SortBy.ToLowerInvariant() switch
        {
            "name" => x => x.Name,
            "description" => x => x.Description,
            "parentcategoryid" => x => x.ParentCategoryId ?? Guid.Empty,
            "createdat" => x => x.CreatedAt!,
            "updatedat" => x => x.UpdatedAt!,
            _ => x => x.Name // Fallback to Name
        };
    }

    /// <summary>
    /// Build include expression cho Category queries
    /// </summary>
    public static Func<IQueryable<Category>, IQueryable<Category>> BuildInclude(bool includeParent = true, bool includeSubCategories = false, bool includeProducts = false)
    {
        return query =>
        {
            if (includeParent)
                query = query.Include(c => c.ParentCategory);

            if (includeSubCategories)
                query = query.Include(c => c.SubCategories.Where(sc => !sc.IsDeleted));

            if (includeProducts)
                query = query.Include(c => c.Products.Where(p => !p.IsDeleted));

            return query;
        };
    }
}
