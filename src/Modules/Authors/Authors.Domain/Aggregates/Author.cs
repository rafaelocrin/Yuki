using Authors.Contracts;
using Shared.Domain.Events;
using Shared.Domain.Exceptions;

namespace Authors.Domain.Aggregates;

public sealed class Author
{
    private readonly List<IDomainEvent> _uncommittedEvents = new();

    public AuthorId Id { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public string Surname { get; private set; } = string.Empty;

    public IReadOnlyList<IDomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    private Author() { }

    public static Author Create(string name, string surname, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name cannot be empty.");
        if (string.IsNullOrWhiteSpace(surname))
            throw new DomainException("Surname cannot be empty.");

        var author = new Author();
        author.RaiseEvent(new AuthorCreatedEvent(
            EventId:   Guid.NewGuid(),
            OccurredOn: now,
            AuthorId:  new AuthorId(Guid.NewGuid()),
            Name:      name,
            Surname:   surname));
        return author;
    }

    public static Author Rehydrate(IEnumerable<IDomainEvent> events)
    {
        var author = new Author();
        foreach (var @event in events)
        {
            if (@event is AuthorCreatedEvent created)
                author.Apply(created);
        }
        return author;
    }

    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();

    private void RaiseEvent(AuthorCreatedEvent @event)
    {
        Apply(@event);
        _uncommittedEvents.Add(@event);
    }

    private void Apply(AuthorCreatedEvent @event)
    {
        Id = @event.AuthorId;
        Name = @event.Name;
        Surname = @event.Surname;
    }
}
