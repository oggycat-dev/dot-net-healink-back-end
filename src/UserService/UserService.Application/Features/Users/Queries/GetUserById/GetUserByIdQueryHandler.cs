using AutoMapper;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Repositories;
using SharedLibrary.Contracts.User.Rpc;
using UserService.Application.Commons.DTOs;
using UserService.Domain.Entities;

namespace UserService.Application.Features.Users.Queries.GetUserById;

public class GetUserByIdQueryHandler
    : IRequestHandler<GetUserByIdQuery, Result<UserProfileResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IRequestClient<GetUserRolesRequest> _rolesClient;
    private readonly ILogger<GetUserByIdQueryHandler> _logger;
    
    // RPC timeout configuration
    private const int RpcTimeoutSeconds = 10;

    public GetUserByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IRequestClient<GetUserRolesRequest> rolesClient,
        ILogger<GetUserByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _rolesClient = rolesClient;
        _logger = logger;
    }

    public async Task<Result<UserProfileResponse>> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var repository = _unitOfWork.Repository<UserProfile>();

            // Get user profile by ID
            var userProfile = await repository.GetFirstOrDefaultAsync(u => u.Id == request.Id);

            if (userProfile == null)
            {
                _logger.LogWarning("UserProfile not found: {Id}", request.Id);
                return Result<UserProfileResponse>.Failure(
                    "User profile not found",
                    ErrorCodeEnum.NotFound);
            }

            // Map to response DTO (without roles yet)
            var response = _mapper.Map<UserProfileResponse>(userProfile);

            // ✅ Fetch roles from AuthService via RPC
            _logger.LogInformation("Fetching roles for UserId: {UserId}", userProfile.UserId);

            try
            {
                // RPC call with timeout
                var rolesResponse = await _rolesClient.GetResponse<GetUserRolesResponse>(
                    new GetUserRolesRequest { UserIds = new List<Guid> { userProfile.UserId } },
                    cancellationToken,
                    timeout: RequestTimeout.After(s: RpcTimeoutSeconds)
                );

                if (rolesResponse.Message.Success &&
                    rolesResponse.Message.UserRoles.TryGetValue(userProfile.UserId, out var roles))
                {
                    response.Roles = roles;
                    _logger.LogInformation(
                        "Successfully fetched {Count} roles for UserId: {UserId}",
                        roles.Count, userProfile.UserId);
                }
                else
                {
                    _logger.LogWarning("No roles found for UserId: {UserId}", userProfile.UserId);
                    response.Roles = new List<string>();
                }
            }
            catch (RequestTimeoutException ex)
            {
                _logger.LogError(ex, "RPC timeout getting user roles after {Timeout}s", RpcTimeoutSeconds);
                // Continue without roles
                response.Roles = new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user roles via RPC for UserId: {UserId}", userProfile.UserId);
                // Continue without roles
                response.Roles = new List<string>();
            }

            return Result<UserProfileResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {Id}", request.Id);
            return Result<UserProfileResponse>.Failure(
                "An error occurred while retrieving user",
                ErrorCodeEnum.InternalError);
        }
    }
}
