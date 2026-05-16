using BloggingSystem.Application.Ports;
using BloggingSystem.Application.Projections;
using BloggingSystem.Application.ReadModels;
using BloggingSystem.Domain.Events;
using BloggingSystem.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace BloggingSystem.Application.Tests.Projections;

public sealed class AuthorProjectionTests
{
    private readonly IAuthorReadRepository _authorRepo;
    private readonly AuthorProjection _projection;

    public AuthorProjectionTests()
    {
        _authorRepo = Substitute.For<IAuthorReadRepository>();
        _projection = new AuthorProjection(_authorRepo);
    }

    [Fact]
    public async Task ProjectAsync_WithAuthorCreatedEvent_CallsUpsert()
    {
        var authorId = new AuthorId(Guid.NewGuid());
        var evt = new AuthorCreatedEvent(Guid.NewGuid(), DateTime.UtcNow, authorId, "Alice", "Jones");

        await _projection.ProjectAsync(new[] { evt });

        await _authorRepo.Received(1).UpsertAsync(
            Arg.Is<AuthorReadModel>(r =>
                r.Id == authorId.Value &&
                r.Name == "Alice" &&
                r.Surname == "Jones"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProjectAsync_WithUnknownEvent_DoesNotCallUpsert()
    {
        var unknownEvent = Substitute.For<IDomainEvent>();
        await _projection.ProjectAsync(new[] { unknownEvent });

        await _authorRepo.DidNotReceive().UpsertAsync(Arg.Any<AuthorReadModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProjectAsync_WithEmptyEvents_DoesNothing()
    {
        await _projection.ProjectAsync(Enumerable.Empty<IDomainEvent>());
        await _authorRepo.DidNotReceive().UpsertAsync(Arg.Any<AuthorReadModel>(), Arg.Any<CancellationToken>());
    }
}
