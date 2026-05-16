using BloggingSystem.Application.Commands.CreatePost;
using BloggingSystem.Application.Ports;
using BloggingSystem.Application.Projections;
using BloggingSystem.Application.ReadModels;
using BloggingSystem.Domain.Events;
using BloggingSystem.Domain.Exceptions;
using FluentAssertions;
using NSubstitute;

namespace BloggingSystem.Application.Tests.Handlers;

public sealed class CreatePostCommandHandlerTests
{
    private readonly IEventStore _eventStore;
    private readonly IAuthorReadRepository _authorRepo;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IPostReadRepository _postRepo;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly PostProjection _projection;
    private readonly CreatePostCommandHandler _handler;

    public CreatePostCommandHandlerTests()
    {
        _eventStore = Substitute.For<IEventStore>();
        _authorRepo = Substitute.For<IAuthorReadRepository>();
        _outboxWriter = Substitute.For<IOutboxWriter>();
        _postRepo = Substitute.For<IPostReadRepository>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
        _projection = new PostProjection(_postRepo);
        _handler = new CreatePostCommandHandler(_eventStore, _authorRepo, _outboxWriter, _projection, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenAuthorExists_ReturnsNewPostId()
    {
        var authorId = Guid.NewGuid();
        _authorRepo.GetByIdAsync(authorId, Arg.Any<CancellationToken>())
            .Returns(new AuthorReadModel { Id = authorId, Name = "Jane", Surname = "Doe" });

        var command = new CreatePostCommand(authorId, "Title", "Desc", "Content");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_WhenAuthorExists_AppendsDomainEvents()
    {
        var authorId = Guid.NewGuid();
        _authorRepo.GetByIdAsync(authorId, Arg.Any<CancellationToken>())
            .Returns(new AuthorReadModel { Id = authorId, Name = "Jane", Surname = "Doe" });

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
        _authorRepo.GetByIdAsync(authorId, Arg.Any<CancellationToken>())
            .Returns(new AuthorReadModel { Id = authorId, Name = "Jane", Surname = "Doe" });

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
        _authorRepo.GetByIdAsync(authorId, Arg.Any<CancellationToken>())
            .Returns(new AuthorReadModel { Id = authorId, Name = "Jane", Surname = "Doe" });

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
        _authorRepo.GetByIdAsync(authorId, Arg.Any<CancellationToken>())
            .Returns((AuthorReadModel?)null);

        var command = new CreatePostCommand(authorId, "Title", "Desc", "Content");
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<AuthorNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAuthorNotFound_DoesNotAppendEvents()
    {
        var authorId = Guid.NewGuid();
        _authorRepo.GetByIdAsync(authorId, Arg.Any<CancellationToken>())
            .Returns((AuthorReadModel?)null);

        var command = new CreatePostCommand(authorId, "Title", "Desc", "Content");
        try { await _handler.Handle(command, CancellationToken.None); } catch { }

        await _eventStore.DidNotReceive().AppendEventsAsync(
            Arg.Any<Guid>(), Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAuthorExists_WritesEventsToOutbox()
    {
        var authorId = Guid.NewGuid();
        _authorRepo.GetByIdAsync(authorId, Arg.Any<CancellationToken>())
            .Returns(new AuthorReadModel { Id = authorId, Name = "Jane", Surname = "Doe" });

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
        _authorRepo.GetByIdAsync(authorId, Arg.Any<CancellationToken>())
            .Returns((AuthorReadModel?)null);

        var command = new CreatePostCommand(authorId, "Title", "Desc", "Content");
        try { await _handler.Handle(command, CancellationToken.None); } catch { }

        await _outboxWriter.DidNotReceive().WriteAsync(
            Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }
}
