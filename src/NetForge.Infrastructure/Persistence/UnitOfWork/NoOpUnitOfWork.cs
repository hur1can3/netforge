using NetForge.Core.UnitOfWork;

namespace NetForge.Infrastructure.Persistence.UnitOfWork;

// TODO(infra-uow-001): Replace with real transactional unit of work (EF Core, multiple db adapters, etc.).
public sealed class NoOpUnitOfWork : IForgeUnitOfWork
{
    public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
}
