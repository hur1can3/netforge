namespace NetForge.Core.DomainEvents;

// TODO(domain-events-003): Implement dispatcher to collect events from aggregates post UoW commit.
public interface IForgeDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IForgeDomainEvent> events, CancellationToken ct);
}
