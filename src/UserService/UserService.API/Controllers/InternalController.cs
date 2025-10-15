using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Repositories;
using UserService.Domain.Entities;

namespace UserService.API.Controllers;

/// <summary>
/// Internal API Controller - For inter-service communication only
/// Not exposed through API Gateway
/// </summary>
[ApiController]
[Route("api/internal/users")]
[ApiExplorerSettings(IgnoreApi = true)] // Hide from Swagger
public class InternalController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InternalController> _logger;

    public InternalController(
        IUnitOfWork unitOfWork,
        ILogger<InternalController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get all users for recommendation service data sync
    /// Internal use only - not authenticated
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            _logger.LogInformation("Internal request: Getting all users for recommendation service");

            var users = await _unitOfWork.Repository<UserProfile>()
                .GetQueryable()
                .Where(u => !u.IsDeleted)
                .ToListAsync();

            var userDtos = users.Select(u => new
            {
                Id = u.UserId.ToString(),
                Email = u.Email,
                FirstName = u.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty,
                LastName = u.FullName.Contains(' ') ? u.FullName.Substring(u.FullName.IndexOf(' ') + 1) : string.Empty,
                CreatedAt = u.CreatedAt,
                Preferences = (string?)null
            }).ToList();

            _logger.LogInformation("Returning {UserCount} users to recommendation service", userDtos.Count);

            return Ok(new
            {
                IsSuccess = true,
                Data = userDtos,
                Message = "Users retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users for recommendation service");
            return StatusCode(500, new
            {
                IsSuccess = false,
                Message = "Failed to fetch users",
                ErrorCode = "InternalError"
            });
        }
    }

    /// <summary>
    /// Get specific user by ID for recommendation service
    /// Internal use only - not authenticated
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserById(string userId)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return BadRequest(new
                {
                    IsSuccess = false,
                    Message = "Invalid user ID format",
                    ErrorCode = "ValidationFailed"
                });
            }

            _logger.LogInformation("Internal request: Getting user {UserId} for recommendation service", userId);

            var userProfile = await _unitOfWork.Repository<UserProfile>()
                .GetQueryable()
                .Where(u => u.UserId == userGuid && !u.IsDeleted)
                .FirstOrDefaultAsync();

            if (userProfile == null)
            {
                return NotFound(new
                {
                    IsSuccess = false,
                    Message = "User not found",
                    ErrorCode = "NotFound"
                });
            }

            var user = new
            {
                Id = userProfile.UserId?.ToString() ?? string.Empty,
                Email = userProfile.Email,
                FirstName = userProfile.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty,
                LastName = userProfile.FullName.Contains(' ') ? userProfile.FullName.Substring(userProfile.FullName.IndexOf(' ') + 1) : string.Empty,
                CreatedAt = userProfile.CreatedAt,
                Preferences = (string?)null
            };

            return Ok(new
            {
                IsSuccess = true,
                Data = user,
                Message = "User retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user {UserId} for recommendation service", userId);
            return StatusCode(500, new
            {
                IsSuccess = false,
                Message = "Failed to fetch user",
                ErrorCode = "InternalError"
            });
        }
    }

    /// <summary>
    /// Health check for internal API
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "UserService Internal API",
            Timestamp = DateTime.UtcNow
        });
    }
}
