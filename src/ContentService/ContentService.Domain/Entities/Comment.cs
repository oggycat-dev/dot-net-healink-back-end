using SharedLibrary.Commons.Entities;
using ContentService.Domain.Enums;

namespace ContentService.Domain.Entities;

public class Comment : BaseEntity
{
    public string Content { get; set; } = string.Empty;
    public Guid ContentId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public bool IsApproved { get; set; } = false;
    public int LikeCount { get; set; } = 0;
    public int ReplyCount { get; set; } = 0;
    
    // Navigation properties
    public virtual Content ContentItem { get; set; } = null!;
    public virtual Comment? ParentComment { get; set; }
    public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
    public virtual ICollection<CommentLike> Likes { get; set; } = new List<CommentLike>();
}

public class CommentLike : BaseEntity
{
    public Guid CommentId { get; set; }
    public Guid UserId { get; set; }
    
    // Navigation properties
    public virtual Comment Comment { get; set; } = null!;
}