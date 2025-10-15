using SharedLibrary.Commons.Models;

namespace UserService.Application.Commons.DTOs;

/// <summary>
/// Filter for paginated user profiles with dynamic filtering
/// </summary>
public class UserProfileFilter : BasePaginationFilter
{
    /// <summary>
    /// Filter by specific user ID
    /// </summary>
    public Guid? UserId { get; set; }
    
    /// <summary>
    /// Filter by email (exact match or contains)
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// Filter by full name (contains)
    /// </summary>
    public string? FullName { get; set; }
    
    /// <summary>
    /// Filter by phone number (contains)
    /// </summary>
    public string? PhoneNumber { get; set; }
    
    /// <summary>
    /// Filter by last login date range start
    /// </summary>
    public DateTime? LastLoginFrom { get; set; }
    
    /// <summary>
    /// Filter by last login date range end
    /// </summary>
    public DateTime? LastLoginTo { get; set; }
    
    /// <summary>
    /// Filter by creation date range start
    /// </summary>
    public DateTime? CreatedFrom { get; set; }
    
    /// <summary>
    /// Filter by creation date range end
    /// </summary>
    public DateTime? CreatedTo { get; set; }
}
