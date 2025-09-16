using ProductAuthMicroservice.Commons.Enums;

namespace ProductAuthMicroservice.Commons.Entities;

public class BaseEntity : IEntityLike
{
    public Guid Id { get; set; }
    public DateTime? CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
    public Guid? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public EntityStatusEnum Status { get; set; } = EntityStatusEnum.Active;
}