using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ProductAuthMicroservice.Commons.Entities;

namespace ProductAuthMicroservice.Commons.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class, IEntityLike
{
    private readonly DbContext _context;
    private readonly DbSet<T> _dbSet;

    public GenericRepository(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<T>();
    }

    private void DetachEntity(T entity)
    {
        var entityType = typeof(T);
        // Look for Id property (int) or any property ending with "Id" that's a Guid
        var keyProperty = entityType.GetProperties()
            .FirstOrDefault(p => (p.Name == "Id" && (p.PropertyType == typeof(int) || p.PropertyType == typeof(Guid))) || 
                                (p.Name.EndsWith("Id") && p.PropertyType == typeof(Guid)));

        if (keyProperty != null)
        {
            var entityKeyValue = keyProperty.GetValue(entity);

            // Only detach if the key has a valid value (not 0 for int, not empty Guid)
            bool hasValidKey = false;
            if (keyProperty.PropertyType == typeof(int))
            {
                hasValidKey = entityKeyValue != null && (int)entityKeyValue != 0;
            }
            else if (keyProperty.PropertyType == typeof(Guid))
            {
                hasValidKey = entityKeyValue != null && (Guid)entityKeyValue != Guid.Empty;
            }

            if (hasValidKey)
            {
                //Find and detach any existing entity with the same key
                var existingEntry = _context.ChangeTracker.Entries<T>()
                    .FirstOrDefault(e => e.Entity != entity && 
                    keyProperty.GetValue(e.Entity)?.Equals(entityKeyValue) == true);

                if (existingEntry != null)
                {
                    _context.Entry(existingEntry.Entity).State = EntityState.Detached;
                }
            }
        }
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        var query = GetQueryable();
        return await query.AnyAsync(predicate);
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        var query = GetQueryable();
        if (predicate != null)
        {
            query = query.Where(predicate);
        }
        return await query.CountAsync();
    }

    public virtual void Delete(T entity)
    {
        // Kiểm tra entity đã được track chưa
        var entry = _context.ChangeTracker.Entries<T>().FirstOrDefault(e => e.Entity == entity);
        if (entry == null)
        {
            // Nếu chưa track thì detach entity và attach vào context
            DetachEntity(entity);
            _dbSet.Attach(entity);
        }
        else
        {
            // Nếu đã track thì chỉ cần set state là deleted
            entry.State = EntityState.Deleted;
        }
        // Remove entity
        _dbSet.Remove(entity);
    }

    public virtual void DeleteRange(IEnumerable<T> entities)
    {
       
        // Batch detach existing entities with same keys
        foreach (var entity in entities)
        {
            DetachEntity(entity);
        }
        
        // Batch attach all entities
        _dbSet.AttachRange(entities);
        
        // Batch remove all entities
        _dbSet.RemoveRange(entities);
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>>? predicate = null, params Expression<Func<T, object>>[] includes)
    {
        var query = GetQueryable();
        if (predicate != null)
        {
            query = query.Where(predicate);
        }
        if (includes?.Any() == true)
        {
            query = includes.Aggregate(query, (current, include) => current.Include(include));
        }
        return await query.ToListAsync();
    }

    public async Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes)
    {
        var query = GetQueryable();
        if (includes?.Any() == true)
        {
            query = includes.Aggregate(query, (current, include) => current.Include(include));
        }
        return await query.ToListAsync();
    }

    public async Task<T> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
    {
        var query = GetQueryable();
        if (includes?.Any() == true)
        {
            query = includes.Aggregate(query, (current, include) => current.Include(include));
        }
        return await query.FirstOrDefaultAsync(predicate);
    }

    public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate = null, Expression<Func<T, object>>? orderBy = null, bool isAscending = true, params Expression<Func<T, object>>[] includes)
    {
        var query = GetQueryable();
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        if (includes?.Any() == true)
        {
            query = includes.Aggregate(query, (current, include) => current.Include(include));
        }

        if (orderBy != null)
        {
            query = isAscending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
        }
        var totalCount = await query.CountAsync();
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        // var hasPrevious = pageNumber > 1;
        // var hasNext = (pageNumber * pageSize) < totalCount;
        return (items, totalCount);
    }

    public IQueryable<T> GetQueryable()
    {
        return _dbSet.AsNoTracking();
    }
    
    public virtual void Update(T entity)
    {
        // Kiểm tra entity đã được track chưa
        var entry = _context.ChangeTracker.Entries<T>().FirstOrDefault(e => e.Entity == entity);
        if (entry != null)
        {
            // Nếu đã track thì chỉ cần set state là modified
            entry.State = EntityState.Modified;
        }
        else
        {
            // Detach entity nếu đã tồn tại trong context
            DetachEntity(entity);
            // Attach entity vào context
            _dbSet.Attach(entity);
            // Set state là modified
            _context.Entry(entity).State = EntityState.Modified;
        }
    }

    public virtual void UpdateRange(IEnumerable<T> entities)
    {
        // Batch detach existing entities with same keys
        foreach (var entity in entities)
        {
            DetachEntity(entity);
        }
        
        // Batch attach all entities
        _dbSet.AttachRange(entities);
        
        // Batch set state for all entities
        foreach (var entity in entities)
        {
            _context.Entry(entity).State = EntityState.Modified;
        }
    }
}