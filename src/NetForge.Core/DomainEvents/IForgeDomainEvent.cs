namespace NetForge.Core.DomainEvents;

// TODO(domain-events-001): Flesh out domain event interface contract (correlation, timestamp, metadata).
public interface IForgeDomainEvent { DateTime OccurredOnUtc { get; } }

// TODO(domain-events-002): Add base abstract class implementing OccurredOnUtc.
public abstract record ForgeDomainEventBase(DateTime OccurredOnUtc) : IForgeDomainEvent;
