using AutoMapper;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Repositories;
using SharedLibrary.Contracts.User.Rpc;
using UserService.Application.Commons.DTOs;
using UserService.Application.Commons.QueryBuilders;
using UserService.Domain.Entities;

namespace UserService.Application.Features.Users.Queries.GetUsers;

public class GetUsersQueryHandler
    : IRequestHandler<GetUsersQuery, PaginationResult<UserProfileResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IRequestClient<GetUserRolesRequest> _rolesClient;
    private readonly ILogger<GetUsersQueryHandler> _logger;
    
    // RPC timeout configuration
    private const int RpcTimeoutSeconds = 10;

    public GetUsersQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IRequestClient<GetUserRolesRequest> rolesClient,
        ILogger<GetUsersQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _rolesClient = rolesClient;
        _logger = logger;
    }

    public async Task<PaginationResult<UserProfileResponse>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var repository = _unitOfWork.Repository<UserProfile>();

            // Build predicate using query builder
            var predicate = request.Filter.BuildPredicate();
            var orderBy = request.Filter.BuildOrderBy();
            var includes = UserProfileQueryBuilder.GetIncludes();

            // Get paginated data
            var (items, totalCount) = await repository.GetPagedAsync(
                pageNumber: request.Filter.Page,
                pageSize: request.Filter.PageSize,
                predicate: predicate,
                orderBy: orderBy,
                isAscending: request.Filter.IsAscending ?? false,
                includes: includes
            );

            // Map to response DTOs (without roles yet)
            var response = _mapper.Map<List<UserProfileResponse>>(items);

            // If no users found, return empty result
            if (!response.Any())
            {
                return PaginationResult<UserProfileResponse>.Success(
                    response,
                    request.Filter.Page,
                    request.Filter.PageSize,
                    totalCount);
            }

            // ✅ Fetch roles for all users in ONE RPC call (batch request)
            // Filter out users with no UserId (pending state)
            var userIds = items
                .Where(x => x.UserId.HasValue)
                .Select(x => x.UserId!.Value)
                .ToList();
            
            _logger.LogInformation(
                "Fetching roles for {Count} users via RPC (Page: {Page}, PageSize: {PageSize})",
                userIds.Count, request.Filter.Page, request.Filter.PageSize);

            try
            {
                // RPC call with timeout (only if there are users with valid UserId)
                if (userIds.Any())
                {
                    var rolesResponse = await _rolesClient.GetResponse<GetUserRolesResponse>(
                        new GetUserRolesRequest { UserIds = userIds },
                        cancellationToken,
                        timeout: RequestTimeout.After(s: RpcTimeoutSeconds)
                    );

                    if (rolesResponse.Message.Success)
                    {
                        var userRolesDictionary = rolesResponse.Message.UserRoles;

                        // ✅ Map roles to response using dictionary O(1) lookup
                        foreach (var user in response)
                        {
                            if (user.UserId.HasValue && userRolesDictionary.TryGetValue(user.UserId.Value, out var roles))
                            {
                                user.Roles = roles;
                            }
                            else
                            {
                                if (user.UserId.HasValue)
                                {
                                    _logger.LogWarning("No roles found for UserId: {UserId}", user.UserId.Value);
                                }
                                user.Roles = new List<string>();
                            }
                        }

                        _logger.LogInformation(
                            "Successfully fetched roles for {Count} users",
                            userRolesDictionary.Count);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "RPC GetUserRoles failed: {Error}",
                            rolesResponse.Message.ErrorMessage);
                    }
                }
                else
                {
                    _logger.LogInformation("No users with valid UserId to fetch roles for");
                }
            }
            catch (RequestTimeoutException ex)
            {
                _logger.LogError(ex, "RPC timeout getting user roles after {Timeout}s", RpcTimeoutSeconds);
                // Continue without roles
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user roles via RPC");
                // Continue without roles
            }

            return PaginationResult<UserProfileResponse>.Success(
                response,
                request.Filter.Page,
                request.Filter.PageSize,
                totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users with filter");
            return PaginationResult<UserProfileResponse>.Failure(
                "An error occurred while retrieving users",
                ErrorCodeEnum.InternalError);
        }
    }
}
