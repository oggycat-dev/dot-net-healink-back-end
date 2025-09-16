using Microsoft.AspNetCore.Identity;
using ProductAuthMicroservice.Commons.Entities;
using ProductAuthMicroservice.Commons.Enums;

namespace ProductAuthMicroservice.AuthService.Domain.Entities;

public class AppRole : IdentityRole<Guid>, IEntityLike
{
    public DateTime? CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public EntityStatusEnum Status { get; set; }
}