using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ProductAuthMicroservice.Commons.Extensions;
using ProductAuthMicroservice.ProductService.Application.Features.Products.DTOs;
using ProductAuthMicroservice.ProductService.Domain.Entities;

namespace ProductAuthMicroservice.ProductService.Application.Features.Products.QueryBuilders;

/// <summary>
/// Extension methods cho ProductFilter để build query trực tiếp
/// Không cần inject QueryBuilder vào CQRS handlers
/// </summary>
public static class ProductQueryBuilder
{
    /// <summary>
    /// Build complete predicate từ ProductFilter
    /// </summary>
    public static Expression<Func<Product, bool>> BuildPredicate(this ProductFilter filter)
    {
        var predicate = PredicateBuilder.True<Product>();

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
            var searchPredicate = PredicateBuilder.False<Product>();
            
            // Search trong Name
            searchPredicate = searchPredicate.CombineOr(x => x.Name.Contains(filter.Search));
            
            // Search trong Description (null-safe)
            searchPredicate = searchPredicate.CombineOr(
                x => x.Description != null && x.Description.Contains(filter.Search));

            predicate = predicate.CombineAnd(searchPredicate);
        }

        // Category filter
        if (filter.CategoryId.HasValue)
        {
            predicate = predicate.CombineAnd(x => x.CategoryId == filter.CategoryId.Value);
        }

        // Price filters
        if (filter.MinPrice.HasValue)
        {
            predicate = predicate.CombineAnd(x => x.Price >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            predicate = predicate.CombineAnd(x => x.Price <= filter.MaxPrice.Value);
        }

        // PreOrder filter
        if (filter.IsPreOrder.HasValue)
        {
            predicate = predicate.CombineAnd(x => x.IsPreOrder == filter.IsPreOrder.Value);
        }

        // Stock filters
        if (filter.InStock.HasValue)
        {
            if (filter.InStock.Value)
                predicate = predicate.CombineAnd(x => x.StockQuantity > 0);
            else
                predicate = predicate.CombineAnd(x => x.StockQuantity == 0);
        }

        if (filter.MinStock.HasValue)
        {
            predicate = predicate.CombineAnd(x => x.StockQuantity >= filter.MinStock.Value);
        }

        if (filter.MaxStock.HasValue)
        {
            predicate = predicate.CombineAnd(x => x.StockQuantity <= filter.MaxStock.Value);
        }

        return predicate;
    }

    /// <summary>
    /// Build OrderBy expression từ ProductFilter
    /// Default: CreatedAt giảm dần (newest first)
    /// </summary>
    public static Expression<Func<Product, object>> BuildOrderBy(this ProductFilter filter)
    {
        if (string.IsNullOrWhiteSpace(filter.SortBy))
            return x => x.CreatedAt!; // Default: CreatedAt giảm dần

        return filter.SortBy.ToLowerInvariant() switch
        {
            "name" => x => x.Name,
            "price" => x => x.Price,
            "discountprice" => x => x.DiscountPrice ?? 0,
            "stockquantity" => x => x.StockQuantity,
            "categoryid" => x => x.CategoryId,
            "ispreorder" => x => x.IsPreOrder,
            "preorderreleasedate" => x => x.PreOrderReleaseDate ?? DateTime.MinValue,
            "createdat" => x => x.CreatedAt!,
            "updatedat" => x => x.UpdatedAt!,
            _ => x => x.CreatedAt! // Fallback to CreatedAt
        };
    }

    /// <summary>
    /// Build include expression cho Product queries
    /// </summary>
    public static Func<IQueryable<Product>, IQueryable<Product>> BuildInclude(bool includeCategory = true, bool includeImages = false, bool includeInventories = false)
    {
        return query =>
        {
            if (includeCategory)
                query = query.Include(p => p.Category);

            if (includeImages)
                query = query.Include(p => p.ProductImages.Where(pi => !pi.IsDeleted));

            if (includeInventories)
                query = query.Include(p => p.ProductInventories.Where(pi => !pi.IsDeleted));

            return query;
        };
    }
}
