using AuthService.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using ProductAuthMicroservice.Commons.Entities;
using ProductAuthMicroservice.Commons.Enums;

namespace ProductAuthMicroservice.AuthService.Domain.Entities;

public class AppUser : IdentityUser<Guid>, IEntityLike
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? LastLoginAt { get; set; }
    public DateTime JoiningAt { get; set; } = DateTime.UtcNow;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public DateTime? CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public EntityStatusEnum Status { get; set; }
    public virtual ICollection<UserAction> UserActions { get; set; } = new List<UserAction>();
}