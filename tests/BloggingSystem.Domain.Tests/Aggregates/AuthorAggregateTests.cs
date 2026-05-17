using Authors.Contracts;
using Authors.Domain.Aggregates;
using Shared.Domain.Events;
using Shared.Domain.Exceptions;
using FluentAssertions;

namespace BloggingSystem.Domain.Tests.Aggregates;

public sealed class AuthorAggregateTests
{
    private static readonly DateTime FixedNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidInputs_RaisesAuthorCreatedEvent()
    {
        var author = Author.Create("Jane", "Doe", FixedNow);

        author.UncommittedEvents.Should().HaveCount(1);
        author.UncommittedEvents[0].Should().BeOfType<AuthorCreatedEvent>();
    }

    [Fact]
    public void Create_WithValidInputs_SetsProperties()
    {
        var author = Author.Create("Jane", "Doe", FixedNow);

        author.Id.Value.Should().NotBe(Guid.Empty);
        author.Name.Should().Be("Jane");
        author.Surname.Should().Be("Doe");
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsDomainException()
    {
        var act = () => Author.Create("  ", "Doe", FixedNow);
        act.Should().Throw<DomainException>().WithMessage("*Name*");
    }

    [Fact]
    public void Create_WithEmptySurname_ThrowsDomainException()
    {
        var act = () => Author.Create("Jane", "   ", FixedNow);
        act.Should().Throw<DomainException>().WithMessage("*Surname*");
    }

    [Fact]
    public void ClearUncommittedEvents_EmptiesTheList()
    {
        var author = Author.Create("Jane", "Doe", FixedNow);
        author.ClearUncommittedEvents();

        author.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void Rehydrate_FromAuthorCreatedEvent_RestoresState()
    {
        var original = Author.Create("John", "Smith", FixedNow);
        var events = original.UncommittedEvents.ToList();

        var rehydrated = Author.Rehydrate(events);

        rehydrated.Id.Should().Be(original.Id);
        rehydrated.Name.Should().Be("John");
        rehydrated.Surname.Should().Be("Smith");
    }

    [Fact]
    public void Rehydrate_FromEmptyEvents_ReturnsBlankAuthor()
    {
        var author = Author.Rehydrate(Enumerable.Empty<IDomainEvent>());
        author.Id.Should().BeNull();
    }
}
