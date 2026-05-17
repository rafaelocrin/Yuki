using Posts.Application.Ports;
using Posts.Application.Queries.GetPostById;
using Posts.Application.ReadModels;
using Posts.Domain.Exceptions;
using Posts.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace BloggingSystem.Application.Tests.Handlers;

public sealed class GetPostByIdQueryHandlerTests
{
    private readonly IPostReadRepository _postRepo;
    private readonly GetPostByIdQueryHandler _handler;

    public GetPostByIdQueryHandlerTests()
    {
        _postRepo = Substitute.For<IPostReadRepository>();
        _handler = new GetPostByIdQueryHandler(_postRepo);
    }

    [Fact]
    public async Task Handle_WhenPostExists_ReturnsPostDto()
    {
        var postId = Guid.NewGuid();
        _postRepo.GetByIdAsync(postId, Arg.Any<CancellationToken>())
            .Returns(new PostReadModel
            {
                Id = postId, AuthorId = Guid.NewGuid(),
                AuthorName = string.Empty, AuthorSurname = string.Empty,
                Title = "T", Description = "D", Content = "C"
            });

        var result = await _handler.Handle(new GetPostByIdQuery(new PostId(postId), false), CancellationToken.None);

        result.Id.Should().Be(postId);
        result.Title.Should().Be("T");
        result.Author.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenPostExistsWithIncludeAuthor_ReturnsAuthorDto()
    {
        var postId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        _postRepo.GetByIdAsync(postId, Arg.Any<CancellationToken>())
            .Returns(new PostReadModel
            {
                Id = postId, AuthorId = authorId,
                AuthorName = "Jane", AuthorSurname = "Doe",
                Title = "T", Description = "D", Content = "C"
            });

        var result = await _handler.Handle(new GetPostByIdQuery(new PostId(postId), true), CancellationToken.None);

        result.Author.Should().NotBeNull();
        result.Author!.Name.Should().Be("Jane");
        result.Author.Surname.Should().Be("Doe");
    }

    [Fact]
    public async Task Handle_WhenIncludeAuthorButNoAuthorLoaded_ReturnsNullAuthor()
    {
        var postId = Guid.NewGuid();
        _postRepo.GetByIdAsync(postId, Arg.Any<CancellationToken>())
            .Returns(new PostReadModel
            {
                Id = postId, AuthorId = Guid.NewGuid(),
                AuthorName = string.Empty, AuthorSurname = string.Empty,
                Title = "T", Description = "D", Content = "C"
            });

        // With the new denormalized model, includeAuthor=true always returns an AuthorDto
        // (even if name is empty), so this test now verifies non-null author
        var result = await _handler.Handle(new GetPostByIdQuery(new PostId(postId), true), CancellationToken.None);

        result.Author.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenPostNotFound_ThrowsPostNotFoundException()
    {
        var postId = Guid.NewGuid();
        _postRepo.GetByIdAsync(postId, Arg.Any<CancellationToken>())
            .Returns((PostReadModel?)null);

        var act = async () => await _handler.Handle(new GetPostByIdQuery(new PostId(postId), false), CancellationToken.None);

        await act.Should().ThrowAsync<PostNotFoundException>();
    }
}
