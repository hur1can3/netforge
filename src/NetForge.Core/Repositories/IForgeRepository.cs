namespace NetForge.Core.Repositories;

// TODO(repos-001): Add specification pattern support via expression / criteria objects.
public interface IForgeRepository<TEntity, in TId>
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct = default);
    Task AddAsync(TEntity entity, CancellationToken ct = default);
    // TODO(repos-002): Add Update/Delete methods and querying with specification.
}
