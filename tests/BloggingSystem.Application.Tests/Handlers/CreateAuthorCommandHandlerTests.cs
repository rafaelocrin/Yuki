using BloggingSystem.Application.Commands.CreateAuthor;
using BloggingSystem.Application.Ports;
using BloggingSystem.Application.Projections;
using BloggingSystem.Domain.Events;
using FluentAssertions;
using NSubstitute;

namespace BloggingSystem.Application.Tests.Handlers;

public sealed class CreateAuthorCommandHandlerTests
{
    private readonly IEventStore _eventStore;
    private readonly IAuthorReadRepository _authorRepo;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly AuthorProjection _projection;
    private readonly CreateAuthorCommandHandler _handler;

    public CreateAuthorCommandHandlerTests()
    {
        _eventStore = Substitute.For<IEventStore>();
        _authorRepo = Substitute.For<IAuthorReadRepository>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
        _projection = new AuthorProjection(_authorRepo);
        _handler = new CreateAuthorCommandHandler(_eventStore, _projection, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_ReturnsNewAuthorId()
    {
        var command = new CreateAuthorCommand("Alice", "Jones");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_AppendsDomainEvents()
    {
        var command = new CreateAuthorCommand("Alice", "Jones");
        await _handler.Handle(command, CancellationToken.None);

        await _eventStore.Received(1).AppendEventsAsync(
            Arg.Any<Guid>(),
            Arg.Any<IEnumerable<IDomainEvent>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpsertsThroughProjection()
    {
        var command = new CreateAuthorCommand("Alice", "Jones");
        await _handler.Handle(command, CancellationToken.None);

        await _authorRepo.Received(1).UpsertAsync(
            Arg.Any<Application.ReadModels.AuthorReadModel>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UsesDateTimeProviderForOccurredOn()
    {
        var fixedTime = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        _dateTimeProvider.UtcNow.Returns(fixedTime);

        var command = new CreateAuthorCommand("Alice", "Jones");
        await _handler.Handle(command, CancellationToken.None);

        _ = _dateTimeProvider.Received(1).UtcNow;
    }

    [Fact]
    public async Task Handle_TwoCalls_ReturnDistinctIds()
    {
        var command = new CreateAuthorCommand("Alice", "Jones");
        var id1 = await _handler.Handle(command, CancellationToken.None);
        var id2 = await _handler.Handle(command, CancellationToken.None);

        id1.Should().NotBe(id2);
    }
}
