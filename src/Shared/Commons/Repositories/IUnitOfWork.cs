using ProductAuthMicroservice.Commons.Entities;

namespace ProductAuthMicroservice.Commons.Repositories;

public interface IUnitOfWork
{
    IGenericRepository<T> Repository<T>() where T : class, IEntityLike;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    void Dispose();
}

