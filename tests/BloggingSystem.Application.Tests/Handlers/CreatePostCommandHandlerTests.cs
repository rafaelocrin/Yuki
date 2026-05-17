using Authors.Contracts;
using Posts.Application.Commands.CreatePost;
using Posts.Application.Ports;
using Posts.Application.Projections;
using Posts.Application.ReadModels;
using Posts.Domain.Events;
using Posts.Domain.Exceptions;
using Shared.Application.Ports;
using Shared.Domain.Events;
using FluentAssertions;
using NSubstitute;

namespace BloggingSystem.Application.Tests.Handlers;

public sealed class CreatePostCommandHandlerTests
{
    private readonly IEventStore _eventStore;
    private readonly IKnownAuthorRepository _knownAuthorRepo;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IPostReadRepository _postRepo;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly PostProjection _projection;
    private readonly CreatePostCommandHandler _handler;

    public CreatePostCommandHandlerTests()
    {
        _eventStore = Substitute.For<IEventStore>();
        _knownAuthorRepo = Substitute.For<IKnownAuthorRepository>();
        _outboxWriter = Substitute.For<IOutboxWriter>();
        _postRepo = Substitute.For<IPostReadRepository>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
        _projection = new PostProjection(_postRepo, _knownAuthorRepo);
        _handler = new CreatePostCommandHandler(_eventStore, _knownAuthorRepo, _outboxWriter, _projection, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenAuthorExists_ReturnsNewPostId()
    {
        var authorId = Guid.NewGuid();
        _knownAuthorRepo.GetByIdAsync(authorId, Arg.Any<CancellationToken>())
            .Returns(new KnownAuthorReadModel { Id = authorId, Name = "Jane", Surname = "Doe" });

        var command = new CreatePostCommand(authorId, "Title", "Desc", "Content");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_WhenAuthorExists_AppendsDomainEvents()
    {
        var authorId = Guid.NewGuid();
        _knownAuthorRepo.GetByIdAsync(authorId, Arg.Any<CancellationToken>())
            .Returns(new KnownAuthorReadModel { Id = authorId, Name = "Jane", Surname = "Doe" });

        var command = new CreatePostCommand(authorId, "Title", "Desc", "Content");
        await _handler.Handle(command, CancellationToken.None);

        await _eventStore.Received(1).AppendEventsAsync(
            Arg.Any<Guid>(),
            Arg.Any<IEnumerable<IDomainEvent>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAuthorExists_UpsertsThroughProjection()
    {
        var authorId = Guid.NewGuid();
        _knownAuthorRepo.GetByIdAsync(authorId, Arg.Any<CancellationToken>())
            .Returns(new KnownAuthorReadModel { Id = authorId, Name = "Jane", Surname = "Doe" });

        var command = new CreatePostCommand(authorId, "Title", "Desc", "Content");
        await _handler.Handle(command, CancellationToken.None);

        await _postRepo.Received(1).UpsertAsync(
            Arg.Any<PostReadModel>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAuthorExists_UsesDateTimeProviderForOccurredOn()
    {
        var fixedTime = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        _dateTimeProvider.UtcNow.Returns(fixedTime);
        var authorId = Guid.NewGuid();
        _knownAuthorRepo.GetByIdAsync(authorId, Arg.Any<CancellationToken>())
            .Returns(new KnownAuthorReadModel { Id = authorId, Name = "Jane", Surname = "Doe" });

        PostReadModel? captured = null;
        await _postRepo.UpsertAsync(Arg.Do<PostReadModel>(m => captured = m), Arg.Any<CancellationToken>());

        var command = new CreatePostCommand(authorId, "Title", "Desc", "Content");
        await _handler.Handle(command, CancellationToken.None);

        _ = _dateTimeProvider.Received(1).UtcNow;
    }

    [Fact]
    public async Task Handle_WhenAuthorNotFound_ThrowsAuthorNotFoundException()
    {
        var authorId = Guid.NewGuid();
        _knownAuthorRepo.GetByIdAsync(authorId, Arg.Any<CancellationToken>())
            .Returns((KnownAuthorReadModel?)null);

        var command = new CreatePostCommand(authorId, "Title", "Desc", "Content");
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<KnownAuthorNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAuthorNotFound_DoesNotAppendEvents()
    {
        var authorId = Guid.NewGuid();
        _knownAuthorRepo.GetByIdAsync(authorId, Arg.Any<CancellationToken>())
            .Returns((KnownAuthorReadModel?)null);

        var command = new CreatePostCommand(authorId, "Title", "Desc", "Content");
        try { await _handler.Handle(command, CancellationToken.None); } catch { }

        await _eventStore.DidNotReceive().AppendEventsAsync(
            Arg.Any<Guid>(), Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAuthorExists_WritesEventsToOutbox()
    {
        var authorId = Guid.NewGuid();
        _knownAuthorRepo.GetByIdAsync(authorId, Arg.Any<CancellationToken>())
            .Returns(new KnownAuthorReadModel { Id = authorId, Name = "Jane", Surname = "Doe" });

        var command = new CreatePostCommand(authorId, "Title", "Desc", "Content");
        await _handler.Handle(command, CancellationToken.None);

        await _outboxWriter.Received(1).WriteAsync(
            Arg.Any<IEnumerable<IDomainEvent>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAuthorNotFound_DoesNotWriteToOutbox()
    {
        var authorId = Guid.NewGuid();
        _knownAuthorRepo.GetByIdAsync(authorId, Arg.Any<CancellationToken>())
            .Returns((KnownAuthorReadModel?)null);

        var command = new CreatePostCommand(authorId, "Title", "Desc", "Content");
        try { await _handler.Handle(command, CancellationToken.None); } catch { }

        await _outboxWriter.DidNotReceive().WriteAsync(
            Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }
}
