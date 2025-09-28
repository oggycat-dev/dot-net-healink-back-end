using ContentService.Domain.Entities;
using SharedLibrary.Commons.Repositories;

namespace ContentService.Domain.Interfaces;

public interface IContentRepository : IGenericRepository<Content>
{
    Task<IEnumerable<Podcast>> GetPodcastsByUserAsync(Guid userId, int page = 1, int pageSize = 10);
    Task<IEnumerable<CommunityStory>> GetPendingStoriesAsync(int page = 1, int pageSize = 10);
    Task<IEnumerable<CommunityStory>> GetApprovedStoriesAsync(int page = 1, int pageSize = 10);
    Task<Podcast?> GetPodcastWithDetailsAsync(Guid id);
    Task<CommunityStory?> GetCommunityStoryWithDetailsAsync(Guid id);
    Task<bool> IsUserOwnerAsync(Guid contentId, Guid userId);
    
    // Podcast specific methods
    Task<Podcast?> GetPodcastByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Podcast> CreatePodcastAsync(Podcast podcast, CancellationToken cancellationToken = default);
    Task<Podcast> UpdatePodcastAsync(Podcast podcast, CancellationToken cancellationToken = default);
    Task<bool> DeletePodcastAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}

public interface ICommentRepository : IGenericRepository<Comment>
{
    Task<IEnumerable<Comment>> GetCommentsByContentAsync(Guid contentId, int page = 1, int pageSize = 10);
    Task<IEnumerable<Comment>> GetRepliesAsync(Guid parentCommentId);
    Task<Comment?> GetCommentWithRepliesAsync(Guid commentId);
}