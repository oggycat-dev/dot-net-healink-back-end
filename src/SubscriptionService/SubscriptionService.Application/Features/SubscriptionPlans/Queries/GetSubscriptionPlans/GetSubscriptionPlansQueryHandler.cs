using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Repositories;
using SubscriptionService.Application.Commons.DTOs;
using SubscriptionService.Application.Commons.QueryBuilders;
using SubscriptionService.Domain.Entities;

namespace SubscriptionService.Application.Features.SubscriptionPlans.Queries.GetSubscriptionPlans;

public class GetSubscriptionPlansQueryHandler : IRequestHandler<GetSubscriptionPlansQuery, PaginationResult<SubscriptionPlanResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetSubscriptionPlansQueryHandler> _logger;

    public GetSubscriptionPlansQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetSubscriptionPlansQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PaginationResult<SubscriptionPlanResponse>> Handle(
        GetSubscriptionPlansQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting subscription plans with filter: {@Filter}", request.Filter);

            var repository = _unitOfWork.Repository<SubscriptionPlan>();

            // Build predicate using query builder
            var predicate = request.Filter.BuildPredicate();

            // Build order by expression
            var orderBy = request.Filter.BuildOrderBy();
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
            var response = _mapper.Map<List<SubscriptionPlanResponse>>(items);

            _logger.LogInformation(
                "Retrieved {Count} subscription plans out of {Total}",
                response.Count,
                totalCount);

            return PaginationResult<SubscriptionPlanResponse>.Success(
                response,
                request.Filter.Page,
                request.Filter.PageSize,
                totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription plans");
            return PaginationResult<SubscriptionPlanResponse>.Failure(
                "Failed to retrieve subscription plans",
                ErrorCodeEnum.InternalError);
        }
    }
}
