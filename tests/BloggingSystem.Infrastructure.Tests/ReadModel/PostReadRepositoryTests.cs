using BloggingSystem.Application.ReadModels;
using BloggingSystem.Infrastructure.Persistence.ReadModel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BloggingSystem.Infrastructure.Tests.ReadModel;

public sealed class PostReadRepositoryTests : IDisposable
{
    private readonly BloggingDbContext _context;
    private readonly PostReadRepository _repo;

    public PostReadRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BloggingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new BloggingDbContext(options);
        _repo = new PostReadRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task UpsertAsync_NewPost_CanBeRetrieved()
    {
        var post = new PostReadModel
        {
            Id = Guid.NewGuid(), AuthorId = Guid.NewGuid(),
            Title = "T", Description = "D", Content = "C"
        };

        await _repo.UpsertAsync(post);
        var result = await _repo.GetByIdAsync(post.Id, false);

        result.Should().NotBeNull();
        result!.Title.Should().Be("T");
    }

    [Fact]
    public async Task UpsertAsync_ExistingPost_UpdatesFields()
    {
        var id = Guid.NewGuid();
        var post = new PostReadModel { Id = id, AuthorId = Guid.NewGuid(), Title = "Old", Description = "D", Content = "C" };
        await _repo.UpsertAsync(post);

        var updated = new PostReadModel { Id = id, AuthorId = post.AuthorId, Title = "New", Description = "D", Content = "C" };
        await _repo.UpsertAsync(updated);

        var result = await _repo.GetByIdAsync(id, false);
        result!.Title.Should().Be("New");
    }

    [Fact]
    public async Task GetByIdAsync_WithIncludeAuthor_ReturnsAuthorNavigation()
    {
        var authorId = Guid.NewGuid();
        _context.Authors.Add(new AuthorReadModel { Id = authorId, Name = "Jane", Surname = "Doe" });
        var post = new PostReadModel { Id = Guid.NewGuid(), AuthorId = authorId, Title = "T", Description = "D", Content = "C" };
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        var result = await _repo.GetByIdAsync(post.Id, includeAuthor: true);

        result!.Author.Should().NotBeNull();
        result.Author!.Name.Should().Be("Jane");
    }

    [Fact]
    public async Task GetByIdAsync_WithoutIncludeAuthor_DoesNotLoadAuthor()
    {
        var authorId = Guid.NewGuid();
        _context.Authors.Add(new AuthorReadModel { Id = authorId, Name = "Jane", Surname = "Doe" });
        var post = new PostReadModel { Id = Guid.NewGuid(), AuthorId = authorId, Title = "T", Description = "D", Content = "C" };
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        var result = await _repo.GetByIdAsync(post.Id, includeAuthor: false);

        result.Should().NotBeNull();
        result!.Author.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var result = await _repo.GetByIdAsync(Guid.NewGuid(), false);
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpsertAsync_SameEventTwice_DoesNotCreateDuplicate()
    {
        var id = Guid.NewGuid();
        var post = new PostReadModel { Id = id, AuthorId = Guid.NewGuid(), Title = "T", Description = "D", Content = "C" };

        await _repo.UpsertAsync(post);
        await _repo.UpsertAsync(post);

        var count = await _context.Posts.CountAsync(p => p.Id == id);
        count.Should().Be(1);
    }
}
