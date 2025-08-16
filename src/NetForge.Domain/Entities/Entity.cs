namespace NetForge.Domain.Entities;

public abstract class Entity<TId>
{
    public TId Id { get; protected set; } = default!;
    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();
    protected void AddDomainEvent(object evt) => _domainEvents.Add(evt);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
