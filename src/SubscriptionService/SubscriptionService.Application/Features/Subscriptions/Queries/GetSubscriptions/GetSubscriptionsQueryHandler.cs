using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Repositories;
using SubscriptionService.Application.Commons.DTOs;
using SubscriptionService.Application.Commons.QueryBuilders;
using SubscriptionService.Domain.Entities;

namespace SubscriptionService.Application.Features.Subscriptions.Queries.GetSubscriptions;

public class GetSubscriptionsQueryHandler
    : IRequestHandler<GetSubscriptionsQuery, PaginationResult<SubscriptionResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetSubscriptionsQueryHandler> _logger;

    public GetSubscriptionsQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetSubscriptionsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PaginationResult<SubscriptionResponse>> Handle(
        GetSubscriptionsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var repository = _unitOfWork.Repository<Subscription>();

            // Build predicate using query builder
            var predicate = request.Filter.BuildPredicate();
            var orderBy = request.Filter.BuildOrderBy();
            var includes = SubscriptionQueryBuilder.GetIncludes();

            // Get paginated data with includes
            var (items, totalCount) = await repository.GetPagedAsync(
                pageNumber: request.Filter.Page,
                pageSize: request.Filter.PageSize,
                predicate: predicate,
                orderBy: orderBy,
                isAscending: request.Filter.IsAscending ?? false,
                includes: includes
            );

            var response = _mapper.Map<List<SubscriptionResponse>>(items);

            return PaginationResult<SubscriptionResponse>.Success(
                response,
                request.Filter.Page,
                request.Filter.PageSize,
                totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscriptions with filter");
            return PaginationResult<SubscriptionResponse>.Failure(
                "An error occurred while retrieving subscriptions",
                ErrorCodeEnum.InternalError);
        }
    }
}
