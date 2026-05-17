using Posts.Application.Ports;
using Posts.Application.Queries.GetPosts;
using Posts.Application.ReadModels;
using FluentAssertions;
using NSubstitute;

namespace BloggingSystem.Application.Tests.Handlers;

public sealed class GetPostsQueryHandlerTests
{
    private readonly IPostReadRepository _postRepo;
    private readonly GetPostsQueryHandler _handler;

    public GetPostsQueryHandlerTests()
    {
        _postRepo = Substitute.For<IPostReadRepository>();
        _handler = new GetPostsQueryHandler(_postRepo);
    }

    private static PostReadModel MakePost(string title = "T") => new()
    {
        Id = Guid.NewGuid(), AuthorId = Guid.NewGuid(),
        AuthorName = "Jane", AuthorSurname = "Doe",
        Title = title, Description = "D", Content = "C",
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Handle_ReturnsMappedDtos()
    {
        var posts = new List<PostReadModel> { MakePost("A"), MakePost("B") };
        _postRepo.GetPagedAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns((posts, 2));

        var result = await _handler.Handle(new GetPostsQuery(1, 10, false), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithIncludeAuthor_MapsAuthorDto()
    {
        var authorId = Guid.NewGuid();
        var post = MakePost();
        post.AuthorName = "Jane";
        post.AuthorSurname = "Doe";
        _postRepo.GetPagedAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns((new List<PostReadModel> { post }, 1));

        var result = await _handler.Handle(new GetPostsQuery(1, 10, true), CancellationToken.None);

        result.Items[0].Author.Should().NotBeNull();
        result.Items[0].Author!.Name.Should().Be("Jane");
    }

    [Fact]
    public async Task Handle_WithoutIncludeAuthor_NullAuthorDto()
    {
        var post = MakePost();
        _postRepo.GetPagedAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns((new List<PostReadModel> { post }, 1));

        var result = await _handler.Handle(new GetPostsQuery(1, 10, false), CancellationToken.None);

        result.Items[0].Author.Should().BeNull();
    }

    [Fact]
    public async Task Handle_EmptyPage_ReturnsEmptyItems()
    {
        _postRepo.GetPagedAsync(5, 10, Arg.Any<CancellationToken>())
            .Returns((new List<PostReadModel>(), 0));

        var result = await _handler.Handle(new GetPostsQuery(5, 10, false), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_TotalPages_CalculatedCorrectly()
    {
        _postRepo.GetPagedAsync(1, 3, Arg.Any<CancellationToken>())
            .Returns((new List<PostReadModel> { MakePost(), MakePost(), MakePost() }, 7));

        var result = await _handler.Handle(new GetPostsQuery(1, 3, false), CancellationToken.None);

        result.TotalPages.Should().Be(3); // ceil(7/3) = 3
    }
}
