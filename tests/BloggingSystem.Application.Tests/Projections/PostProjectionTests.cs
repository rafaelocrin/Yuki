using BloggingSystem.Application.Ports;
using BloggingSystem.Application.Projections;
using BloggingSystem.Application.ReadModels;
using BloggingSystem.Domain.Events;
using BloggingSystem.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace BloggingSystem.Application.Tests.Projections;

public sealed class PostProjectionTests
{
    private readonly IPostReadRepository _postRepo;
    private readonly PostProjection _projection;

    public PostProjectionTests()
    {
        _postRepo = Substitute.For<IPostReadRepository>();
        _projection = new PostProjection(_postRepo);
    }

    [Fact]
    public async Task ProjectAsync_WithPostCreatedEvent_CallsUpsert()
    {
        var postId = new PostId(Guid.NewGuid());
        var authorId = new AuthorId(Guid.NewGuid());
        var evt = new PostCreatedEvent(Guid.NewGuid(), DateTime.UtcNow, postId, authorId,
            "Title", "Desc", "Content");

        await _projection.ProjectAsync(new[] { evt });

        await _postRepo.Received(1).UpsertAsync(
            Arg.Is<PostReadModel>(r =>
                r.Id == postId.Value &&
                r.Title == "Title" &&
                r.Content == "Content"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProjectAsync_WithUnknownEvent_DoesNotCallUpsert()
    {
        var unknownEvent = Substitute.For<IDomainEvent>();
        await _projection.ProjectAsync(new[] { unknownEvent });

        await _postRepo.DidNotReceive().UpsertAsync(Arg.Any<PostReadModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProjectAsync_WithEmptyEvents_DoesNothing()
    {
        await _projection.ProjectAsync(Enumerable.Empty<IDomainEvent>());
        await _postRepo.DidNotReceive().UpsertAsync(Arg.Any<PostReadModel>(), Arg.Any<CancellationToken>());
    }
}
