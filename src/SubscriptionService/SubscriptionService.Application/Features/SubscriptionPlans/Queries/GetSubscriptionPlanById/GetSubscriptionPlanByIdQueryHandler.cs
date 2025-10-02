using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Repositories;
using SubscriptionService.Application.Commons.DTOs;
using SubscriptionService.Domain.Entities;

namespace SubscriptionService.Application.Features.SubscriptionPlans.Queries.GetSubscriptionPlanById;

public class GetSubscriptionPlanByIdQueryHandler : IRequestHandler<GetSubscriptionPlanByIdQuery, Result<SubscriptionPlanResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetSubscriptionPlanByIdQueryHandler> _logger;

    public GetSubscriptionPlanByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetSubscriptionPlanByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<SubscriptionPlanResponse>> Handle(
        GetSubscriptionPlanByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting subscription plan by ID: {Id}", request.Id);

            var repository = _unitOfWork.Repository<SubscriptionPlan>();
            var plan = await repository.GetFirstOrDefaultAsync(x => x.Id == request.Id);

            if (plan == null)
            {
                _logger.LogWarning("Subscription plan not found: {Id}", request.Id);
                return Result<SubscriptionPlanResponse>.Failure(
                    "Subscription plan not found",
                    ErrorCodeEnum.NotFound);
            }

            var response = _mapper.Map<SubscriptionPlanResponse>(plan);

            return Result<SubscriptionPlanResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription plan by ID: {Id}", request.Id);
            return Result<SubscriptionPlanResponse>.Failure(
                "Failed to retrieve subscription plan",
                ErrorCodeEnum.InternalError);
        }
    }
}
