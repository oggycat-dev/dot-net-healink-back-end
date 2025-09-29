using MediatR;
using SharedLibrary.Commons.Models;
using UserService.Application.Commons.DTOs;

namespace UserService.Application.Features.Profile.Queries.GetProfile;

public record GetProfileQuery : IRequest<Result<ProfileResponse>>;