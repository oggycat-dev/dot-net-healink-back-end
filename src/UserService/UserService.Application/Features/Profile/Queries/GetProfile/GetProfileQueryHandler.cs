using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Repositories;
using SharedLibrary.Commons.Services;
using UserService.Application.Commons.DTOs;
using UserService.Domain.Entities;

namespace UserService.Application.Features.Profile.Queries.GetProfile;

public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, Result<ProfileResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetProfileQueryHandler> _logger;

    public GetProfileQueryHandler(ICurrentUserService currentUserService, IUnitOfWork unitOfWork, IMapper mapper, ILogger<GetProfileQueryHandler> logger)
    {
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<ProfileResponse>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (currentUserId == null)
            {
                return Result<ProfileResponse>.Failure("Not authorized", ErrorCodeEnum.Unauthorized);
            }

            var userProfile = await _unitOfWork.Repository<UserProfile>().GetFirstOrDefaultAsync(x => x.UserId == Guid.Parse(currentUserId));
            if (userProfile == null)
            {
                return Result<ProfileResponse>.Failure("User profile not found", ErrorCodeEnum.NotFound);
            }

            return Result<ProfileResponse>.Success(_mapper.Map<ProfileResponse>(userProfile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile");
            return Result<ProfileResponse>.Failure(ex.Message, ErrorCodeEnum.InternalError);
        }
    }
}