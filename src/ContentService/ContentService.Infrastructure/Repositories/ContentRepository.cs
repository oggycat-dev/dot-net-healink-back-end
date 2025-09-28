using ContentService.Domain.Entities;
using ContentService.Domain.Interfaces;
using ContentService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Commons.Repositories;

namespace ContentService.Infrastructure.Repositories;

public class ContentRepository : GenericRepository<Content>, IContentRepository
{
    private readonly ContentDbContext _context;

    public ContentRepository(ContentDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Podcast>> GetPodcastsByUserAsync(Guid userId, int page = 1, int pageSize = 10)
    {
        return await _context.Set<Podcast>()
            .Where(p => p.CreatedBy == userId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<CommunityStory>> GetPendingStoriesAsync(int page = 1, int pageSize = 10)
    {
        return await _context.Set<CommunityStory>()
            .Where(s => s.ContentStatus == Domain.Enums.ContentStatus.PendingModeration && !s.IsDeleted)
            .OrderBy(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<CommunityStory>> GetApprovedStoriesAsync(int page = 1, int pageSize = 10)
    {
        return await _context.Set<CommunityStory>()
            .Where(s => s.ContentStatus == Domain.Enums.ContentStatus.Published && !s.IsDeleted)
            .OrderByDescending(s => s.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Podcast?> GetPodcastWithDetailsAsync(Guid id)
    {
        return await _context.Set<Podcast>()
            .Include(p => p.Comments.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.Replies.Where(r => !r.IsDeleted))
            .Include(p => p.Interactions.Where(i => !i.IsDeleted))
            .Include(p => p.Ratings.Where(r => !r.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
    }

    public async Task<CommunityStory?> GetCommunityStoryWithDetailsAsync(Guid id)
    {
        return await _context.Set<CommunityStory>()
            .Include(s => s.Comments.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.Replies.Where(r => !r.IsDeleted))
            .Include(s => s.Interactions.Where(i => !i.IsDeleted))
            .Include(s => s.Ratings.Where(r => !r.IsDeleted))
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
    }

    public async Task<bool> IsUserOwnerAsync(Guid contentId, Guid userId)
    {
        return await _context.Set<Content>()
            .AnyAsync(c => c.Id == contentId && c.CreatedBy == userId && !c.IsDeleted);
    }

    public async Task<Podcast?> GetPodcastByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Podcast>()
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }

    public async Task<Podcast> CreatePodcastAsync(Podcast podcast, CancellationToken cancellationToken = default)
    {
        await _context.Set<Podcast>().AddAsync(podcast, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return podcast;
    }

    public async Task<Podcast> UpdatePodcastAsync(Podcast podcast, CancellationToken cancellationToken = default)
    {
        _context.Set<Podcast>().Update(podcast);
        await _context.SaveChangesAsync(cancellationToken);
        return podcast;
    }

    public async Task<bool> DeletePodcastAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var podcast = await _context.Set<Podcast>()
            .FirstOrDefaultAsync(p => p.Id == id && p.CreatedBy == userId && !p.IsDeleted, cancellationToken);

        if (podcast == null)
            return false;

        podcast.MarkAsDeleted();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class CommentRepository : GenericRepository<Comment>, ICommentRepository
{
    private readonly ContentDbContext _context;

    public CommentRepository(ContentDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Comment>> GetCommentsByContentAsync(Guid contentId, int page = 1, int pageSize = 10)
    {
        return await _context.Set<Comment>()
            .Where(c => c.ContentId == contentId && c.ParentCommentId == null && !c.IsDeleted && c.IsApproved)
            .Include(c => c.Replies.Where(r => !r.IsDeleted && r.IsApproved))
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Comment>> GetRepliesAsync(Guid parentCommentId)
    {
        return await _context.Set<Comment>()
            .Where(c => c.ParentCommentId == parentCommentId && !c.IsDeleted && c.IsApproved)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Comment?> GetCommentWithRepliesAsync(Guid commentId)
    {
        return await _context.Set<Comment>()
            .Include(c => c.Replies.Where(r => !r.IsDeleted && r.IsApproved))
            .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);
    }
}