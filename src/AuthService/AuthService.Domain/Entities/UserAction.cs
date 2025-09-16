using ProductAuthMicroservice.AuthService.Domain.Entities;
using ProductAuthMicroservice.Commons.Entities;
using ProductAuthMicroservice.Commons.Enums;

namespace AuthService.Domain.Entities;

public class UserAction : BaseEntity
    {
        public Guid UserId { get; set; }
        public UserActionEnum Action { get; set; }
        public Guid? EntityId { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? IPAddress { get; set; }
        public string? ActionDetail { get; set; }
        // navigation property
        public virtual AppUser? User { get; set; }
    }