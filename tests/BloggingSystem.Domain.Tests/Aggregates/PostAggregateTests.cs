using Authors.Contracts;
using Posts.Domain.Aggregates;
using Posts.Domain.Events;
using Posts.Domain.Exceptions;
using Posts.Domain.ValueObjects;
using Shared.Domain.Events;
using Shared.Domain.Exceptions;
using FluentAssertions;

namespace BloggingSystem.Domain.Tests.Aggregates;
#pragma warning disable CS8073 // intentional null checks in tests

public sealed class PostAggregateTests
{
    private static readonly DateTime FixedNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidInputs_RaisesPostCreatedEvent()
    {
        var authorId = new AuthorId(Guid.NewGuid());
        var post = Post.Create(authorId, "Test Title", "A description", "Some content", FixedNow);

        post.UncommittedEvents.Should().HaveCount(1);
        post.UncommittedEvents[0].Should().BeOfType<PostCreatedEvent>();
    }

    [Fact]
    public void Create_WithValidInputs_SetsProperties()
    {
        var authorId = new AuthorId(Guid.NewGuid());
        var post = Post.Create(authorId, "Title", "Desc", "Body", FixedNow);

        post.Id.Value.Should().NotBe(Guid.Empty);
        post.AuthorId.Should().Be(authorId);
        post.Title.Should().Be("Title");
        post.Description.Should().Be("Desc");
        post.Content.Should().Be("Body");
    }

    [Fact]
    public void Create_WithEmptyTitle_ThrowsDomainException()
    {
        var act = () => Post.Create(new AuthorId(Guid.NewGuid()), "   ", "desc", "content", FixedNow);
        act.Should().Throw<DomainException>().WithMessage("*Title*");
    }

    [Fact]
    public void Create_WithEmptyContent_ThrowsDomainException()
    {
        var act = () => Post.Create(new AuthorId(Guid.NewGuid()), "Title", "desc", "   ", FixedNow);
        act.Should().Throw<DomainException>().WithMessage("*Content*");
    }

    [Fact]
    public void Create_WithEmptyGuidAuthorId_ThrowsArgumentException()
    {
        var act = () => new AuthorId(Guid.Empty);
        act.Should().Throw<ArgumentException>().WithMessage("*AuthorId*");
    }

    [Fact]
    public void ClearUncommittedEvents_EmptiesTheList()
    {
        var post = Post.Create(new AuthorId(Guid.NewGuid()), "Title", "desc", "content", FixedNow);
        post.ClearUncommittedEvents();

        post.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void Rehydrate_FromPostCreatedEvent_RestoresState()
    {
        var authorId = new AuthorId(Guid.NewGuid());
        var original = Post.Create(authorId, "Restored Title", "desc", "Restored Content", FixedNow);
        var events = original.UncommittedEvents.ToList();

        var rehydrated = Post.Rehydrate(events);

        rehydrated.Id.Should().Be(original.Id);
        rehydrated.AuthorId.Should().Be(original.AuthorId);
        rehydrated.Title.Should().Be("Restored Title");
        rehydrated.Content.Should().Be("Restored Content");
    }

    [Fact]
    public void Rehydrate_FromEmptyEvents_ReturnsBlankPost()
    {
        var post = Post.Rehydrate(Enumerable.Empty<IDomainEvent>());
        post.Id.Should().BeNull();
    }

    [Fact]
    public void PostCreatedEvent_HasCorrectData()
    {
        var authorId = new AuthorId(Guid.NewGuid());
        var post = Post.Create(authorId, "Title", "Desc", "Content", FixedNow);

        var evt = (PostCreatedEvent)post.UncommittedEvents[0];
        evt.PostId.Should().Be(post.Id);
        evt.AuthorId.Should().Be(authorId);
        evt.Title.Should().Be("Title");
        evt.EventId.Should().NotBe(Guid.Empty);
        evt.OccurredOn.Should().Be(FixedNow);
    }
}
