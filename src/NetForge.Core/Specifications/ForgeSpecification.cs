using System.Linq.Expressions;

namespace NetForge.Core.Specifications;

// TODO(spec-001): Add include, order, paging metadata containers.
public abstract class ForgeSpecification<TEntity>
{
    public abstract Expression<Func<TEntity, bool>> Criteria { get; }
    // TODO(spec-002): Add And/Or/Not composition helpers.
}
