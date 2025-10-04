using Microsoft.AspNetCore.Mvc;
using MediatR;
using System.Security.Claims;
using SharedLibrary.Commons.Attributes;
using UserService.Application.Features.CreatorApplications.Commands;
using UserService.Application.Features.CreatorApplications.Queries;
using Swashbuckle.AspNetCore.Annotations;
using SharedLibrary.Commons.Outbox;
using UserService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace UserService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(GroupName = "v1")]
public class CreatorApplicationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CreatorApplicationsController> _logger;
    private readonly IOutboxUnitOfWork _unitOfWork;

    public CreatorApplicationsController(
        IMediator mediator,
        ILogger<CreatorApplicationsController> logger,
        IOutboxUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Nộp đơn đăng ký làm Content Creator
    /// </summary>
    /// <param name="request">Thông tin đơn đăng ký</param>
    [HttpPost]
    [DistributedAuthorize]
    [SwaggerOperation(
        Summary = "Nộp đơn đăng ký làm Content Creator",
        Description = "Người dùng nộp đơn xin làm content creator với thông tin kinh nghiệm, portfolio, etc",
        OperationId = "SubmitCreatorApplication",
        Tags = new[] { "Creator Applications" }
    )]
    [ProducesResponseType(typeof(SubmitCreatorApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SubmitCreatorApplicationResponse>> SubmitApplication(
        SubmitCreatorApplicationCommand request)
    {
        // Extract UserId from JWT token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Người dùng không được xác thực" });
        }

        // Set UserId in request
        request.UserId = userId;
        
        try
        {
            var result = await _mediator.Send(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation in SubmitApplication");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SubmitApplication");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý đơn đăng ký" });
        }
    }

    /// <summary>
    /// Lấy danh sách đơn đăng ký chờ duyệt (Admin only)
    /// </summary>
    [HttpGet("pending")]
    [DistributedAuthorizeRoles("Admin")]
    [SwaggerOperation(
        Summary = "Lấy danh sách đơn đăng ký chờ duyệt",
        Description = "Admin xem danh sách đơn đăng ký đang chờ duyệt",
        OperationId = "GetPendingApplications",
        Tags = new[] { "Creator Applications" }
    )]
    [ProducesResponseType(typeof(List<PendingApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<PendingApplicationDto>>> GetPendingApplications(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var query = new GetPendingApplicationsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPendingApplications");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy danh sách đơn đăng ký" });
        }
    }
    
    /// <summary>
    /// Lấy trạng thái đơn đăng ký Content Creator của user hiện tại
    /// </summary>
    [HttpGet("my-status")]
    [DistributedAuthorize]
    [SwaggerOperation(
        Summary = "Lấy trạng thái đơn đăng ký của user hiện tại",
        Description = "User xem trạng thái đơn đăng ký Content Creator của mình",
        OperationId = "GetMyApplicationStatus",
        Tags = new[] { "Creator Applications" }
    )]
    [ProducesResponseType(typeof(MyApplicationStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MyApplicationStatusDto>> GetMyApplicationStatus()
    {
        // Extract UserId from JWT token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Người dùng không được xác thực" });
        }

        try
        {
            var query = new GetMyApplicationStatusQuery { UserId = userId };
            var result = await _mediator.Send(query);
            
            if (result == null)
            {
                return NotFound(new { message = "Bạn chưa nộp đơn đăng ký Content Creator nào" });
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetMyApplicationStatus for User: {UserId}", userId);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy trạng thái đơn đăng ký" });
        }
    }
    
    /// <summary>
    /// Debug endpoint để kiểm tra User ID và đơn đăng ký
    /// </summary>
    [HttpGet("debug")]
    [DistributedAuthorize]
    [SwaggerOperation(
        Summary = "Debug endpoint để kiểm tra User ID và đơn đăng ký",
        Description = "Debug endpoint để kiểm tra User ID và đơn đăng ký",
        OperationId = "DebugApplicationStatus",
        Tags = new[] { "Creator Applications" }
    )]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> DebugApplicationStatus()
    {
        // Extract UserId from JWT token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Người dùng không được xác thực" });
        }

        try
        {
            // Get all applications for this user
            var applications = await _unitOfWork.Repository<CreatorApplication>()
                .GetQueryable()
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.SubmittedAt)
                .ToListAsync();

            return Ok(new
            {
                userId = userId,
                userIdString = userIdClaim,
                totalApplications = applications.Count,
                applications = applications.Select(a => new
                {
                    id = a.Id,
                    userId = a.UserId,
                    status = a.ApplicationStatus.ToString(),
                    submittedAt = a.SubmittedAt,
                    reviewedAt = a.ReviewedAt,
                    applicationData = a.ApplicationData
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DebugApplicationStatus for User: {UserId}", userId);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi debug", error = ex.Message });
        }
    }
    
    /// <summary>
    /// Debug endpoint để kiểm tra User ID từ JWT token
    /// </summary>
    [HttpGet("debug-token")]
    [DistributedAuthorize]
    [SwaggerOperation(
        Summary = "Debug endpoint để kiểm tra User ID từ JWT token",
        Description = "Debug endpoint để kiểm tra User ID từ JWT token",
        OperationId = "DebugToken",
        Tags = new[] { "Creator Applications" }
    )]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult DebugToken()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userEmailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            var userNameClaim = User.FindFirst(ClaimTypes.Name)?.Value;
            
            var allClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            
            return Ok(new
            {
                userIdFromToken = userIdClaim,
                userEmailFromToken = userEmailClaim,
                userNameFromToken = userNameClaim,
                allClaims = allClaims,
                message = "Token debug info"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DebugToken");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi debug token", error = ex.Message });
        }
    }
    
    /// <summary>
    /// Debug endpoint để kiểm tra tất cả đơn đăng ký trong database
    /// </summary>
    [HttpGet("debug-all")]
    [DistributedAuthorizeRoles("Admin")]
    [SwaggerOperation(
        Summary = "Debug endpoint để kiểm tra tất cả đơn đăng ký",
        Description = "Debug endpoint để kiểm tra tất cả đơn đăng ký trong database",
        OperationId = "DebugAllApplications",
        Tags = new[] { "Creator Applications" }
    )]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> DebugAllApplications()
    {
        try
        {
            // Get all applications
            var applications = await _unitOfWork.Repository<CreatorApplication>()
                .GetQueryable()
                .OrderByDescending(a => a.SubmittedAt)
                .Take(10)
                .ToListAsync();

            return Ok(new
            {
                totalApplications = applications.Count,
                applications = applications.Select(a => new
                {
                    id = a.Id,
                    userId = a.UserId,
                    status = a.ApplicationStatus.ToString(),
                    submittedAt = a.SubmittedAt,
                    reviewedAt = a.ReviewedAt,
                    applicationData = a.ApplicationData
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DebugAllApplications");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi debug", error = ex.Message });
        }
    }
    
    /// <summary>
    /// Lấy chi tiết đơn đăng ký theo ID
    /// </summary>
    [HttpGet("{id}")]
    [DistributedAuthorizeRoles("Admin")]
    [SwaggerOperation(
        Summary = "Lấy chi tiết đơn đăng ký",
        Description = "Xem thông tin chi tiết của một đơn đăng ký",
        OperationId = "GetApplicationById",
        Tags = new[] { "Creator Applications" }
    )]
    [ProducesResponseType(typeof(ApplicationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApplicationDetailDto>> GetApplicationById(Guid id)
    {
        try
        {
            var query = new GetApplicationByIdQuery { ApplicationId = id };
            var result = await _mediator.Send(query);
            
            if (result == null)
            {
                return NotFound(new { message = $"Không tìm thấy đơn đăng ký với ID: {id}" });
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetApplicationById for ID: {ApplicationId}", id);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy chi tiết đơn đăng ký" });
        }
    }
    
    /// <summary>
    /// Phê duyệt đơn đăng ký Content Creator (Admin only)
    /// </summary>
    [HttpPost("{id}/approve")]
    [DistributedAuthorizeRoles("Admin")]
    [SwaggerOperation(
        Summary = "Phê duyệt đơn đăng ký",
        Description = "Admin phê duyệt đơn đăng ký làm Content Creator",
        OperationId = "ApproveApplication",
        Tags = new[] { "Creator Applications" }
    )]
    [ProducesResponseType(typeof(ApproveCreatorApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApproveCreatorApplicationResponse>> ApproveApplication(
        Guid id, [FromBody] ApproveCreatorApplicationCommand request)
    {
        // Debug logging to see the actual values
        _logger.LogInformation("ApproveApplication - URL ID: {UrlId}, Body ApplicationId: {BodyApplicationId}", 
            id, request.ApplicationId);
        
        if (id != request.ApplicationId)
        {
            _logger.LogWarning("ID mismatch - URL ID: {UrlId}, Body ApplicationId: {BodyApplicationId}", 
                id, request.ApplicationId);
            return BadRequest(new { 
                message = "ID trong URL phải khớp với ID trong request body",
                urlId = id.ToString(),
                bodyApplicationId = request.ApplicationId.ToString()
            });
        }
        
        // Extract Admin UserId from JWT token
        var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminIdClaim) || !Guid.TryParse(adminIdClaim, out var adminId))
        {
            return Unauthorized(new { message = "Admin không được xác thực" });
        }

        // Set ReviewerId in request
        request.ReviewerId = adminId;
        
        try
        {
            var result = await _mediator.Send(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation in ApproveApplication for ID: {ApplicationId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ApproveApplication for ID: {ApplicationId}", id);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi phê duyệt đơn đăng ký" });
        }
    }
    
    /// <summary>
    /// Từ chối đơn đăng ký Content Creator (Admin only)
    /// </summary>
    [HttpPost("{id}/reject")]
    [DistributedAuthorizeRoles("Admin")]
    [SwaggerOperation(
        Summary = "Từ chối đơn đăng ký",
        Description = "Admin từ chối đơn đăng ký làm Content Creator",
        OperationId = "RejectApplication",
        Tags = new[] { "Creator Applications" }
    )]
    [ProducesResponseType(typeof(RejectCreatorApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RejectCreatorApplicationResponse>> RejectApplication(
        Guid id, [FromBody] RejectCreatorApplicationCommand request)
    {
        if (id != request.ApplicationId)
        {
            return BadRequest(new { message = "ID trong URL phải khớp với ID trong request body" });
        }
        
        // Extract Admin UserId from JWT token
        var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminIdClaim) || !Guid.TryParse(adminIdClaim, out var adminId))
        {
            return Unauthorized(new { message = "Admin không được xác thực" });
        }

        // Set ReviewerId in request
        request.ReviewerId = adminId;
        
        try
        {
            var result = await _mediator.Send(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation in RejectApplication for ID: {ApplicationId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RejectApplication for ID: {ApplicationId}", id);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi từ chối đơn đăng ký" });
        }
    }
}
