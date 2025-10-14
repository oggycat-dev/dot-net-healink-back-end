using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Commons.DTOs;
using PaymentService.Domain.Entities;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Repositories;
using System.Linq.Expressions;

namespace PaymentService.Application.Features.PaymentMethods.Queries.GetPaymentMethods;

public class GetPaymentMethodsQueryHandler : IRequestHandler<GetPaymentMethodsQuery, PaginationResult<PaymentMethodResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetPaymentMethodsQueryHandler> _logger;

    public GetPaymentMethodsQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetPaymentMethodsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PaginationResult<PaymentMethodResponse>> Handle(
        GetPaymentMethodsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting payment methods with filter: {@Filter}", request.Filter);

            var repository = _unitOfWork.Repository<PaymentMethod>();

            // Build predicate
            Expression<Func<PaymentMethod, bool>> predicate = x => !x.IsDeleted;

            if (!string.IsNullOrEmpty(request.Filter.Name))
            {
                var name = request.Filter.Name;
                predicate = predicate.And(x => x.Name.Contains(name));
            }

            if (request.Filter.Type.HasValue)
            {
                var type = request.Filter.Type.Value;
                predicate = predicate.And(x => x.Type == type);
            }

            if (!string.IsNullOrEmpty(request.Filter.ProviderName))
            {
                var provider = request.Filter.ProviderName;
                predicate = predicate.And(x => x.ProviderName.Contains(provider));
            }

            // Build order by
            Expression<Func<PaymentMethod, object>> orderBy = request.Filter.SortBy?.ToLower() switch
            {
                "name" => x => x.Name,
                "type" => x => x.Type,
                "provider" => x => x.ProviderName,
                "createdat" => x => x.CreatedAt!,
                _ => x => x.CreatedAt!
            };

            var isAscending = request.Filter.IsAscending ?? false;

            // Get paginated data
            var (items, totalCount) = await repository.GetPagedAsync(
                pageNumber: request.Filter.Page,
                pageSize: request.Filter.PageSize,
                predicate: predicate,
                orderBy: orderBy,
                isAscending: isAscending
            );

            // Map to response
            var response = _mapper.Map<List<PaymentMethodResponse>>(items);

            _logger.LogInformation(
                "Retrieved {Count} payment methods out of {Total}",
                response.Count,
                totalCount);

            return PaginationResult<PaymentMethodResponse>.Success(
                response,
                request.Filter.Page,
                request.Filter.PageSize,
                totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment methods");
            return PaginationResult<PaymentMethodResponse>.Failure(
                "Failed to retrieve payment methods",
                ErrorCodeEnum.InternalError);
        }
    }
}

// Extension for combining predicates
public static class PredicateExtensions
{
    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> first,
        Expression<Func<T, bool>> second)
    {
        var parameter = Expression.Parameter(typeof(T));

        var leftVisitor = new ReplaceExpressionVisitor(first.Parameters[0], parameter);
        var left = leftVisitor.Visit(first.Body);

        var rightVisitor = new ReplaceExpressionVisitor(second.Parameters[0], parameter);
        var right = rightVisitor.Visit(second.Body);

        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left!, right!), parameter);
    }

    private class ReplaceExpressionVisitor : ExpressionVisitor
    {
        private readonly Expression _oldValue;
        private readonly Expression _newValue;

        public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public override Expression? Visit(Expression? node)
        {
            return node == _oldValue ? _newValue : base.Visit(node);
        }
    }
}

