namespace NetForge.Core.UnitOfWork;

// TODO(uow-001): Provide concrete implementations (EF Core, TransactionScope, Composite) in Infrastructure layer.
public interface IForgeUnitOfWork
{
    Task CommitAsync(CancellationToken ct = default);
}
