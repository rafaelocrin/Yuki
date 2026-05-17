using Posts.Application.ReadModels;
using Posts.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BloggingSystem.Infrastructure.Tests.ReadModel;

public sealed class PostReadRepositoryTests : IDisposable
{
    private readonly PostsDbContext _context;
    private readonly PostReadRepository _repo;

    public PostReadRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<PostsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new PostsDbContext(options);
        _repo = new PostReadRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task UpsertAsync_NewPost_CanBeRetrieved()
    {
        var post = new PostReadModel
        {
            Id = Guid.NewGuid(), AuthorId = Guid.NewGuid(),
            AuthorName = "Jane", AuthorSurname = "Doe",
            Title = "T", Description = "D", Content = "C"
        };

        await _repo.UpsertAsync(post);
        var result = await _repo.GetByIdAsync(post.Id);

        result.Should().NotBeNull();
        result!.Title.Should().Be("T");
    }

    [Fact]
    public async Task UpsertAsync_ExistingPost_UpdatesFields()
    {
        var id = Guid.NewGuid();
        var post = new PostReadModel { Id = id, AuthorId = Guid.NewGuid(), AuthorName = "Jane", AuthorSurname = "Doe", Title = "Old", Description = "D", Content = "C" };
        await _repo.UpsertAsync(post);

        var updated = new PostReadModel { Id = id, AuthorId = post.AuthorId, AuthorName = "Jane", AuthorSurname = "Doe", Title = "New", Description = "D", Content = "C" };
        await _repo.UpsertAsync(updated);

        var result = await _repo.GetByIdAsync(id);
        result!.Title.Should().Be("New");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDenormalizedAuthorData()
    {
        var authorId = Guid.NewGuid();
        var post = new PostReadModel { Id = Guid.NewGuid(), AuthorId = authorId, AuthorName = "Jane", AuthorSurname = "Doe", Title = "T", Description = "D", Content = "C" };
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        var result = await _repo.GetByIdAsync(post.Id);

        result.Should().NotBeNull();
        result!.AuthorName.Should().Be("Jane");
        result.AuthorSurname.Should().Be("Doe");
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var result = await _repo.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpsertAsync_SameEventTwice_DoesNotCreateDuplicate()
    {
        var id = Guid.NewGuid();
        var post = new PostReadModel { Id = id, AuthorId = Guid.NewGuid(), AuthorName = "Jane", AuthorSurname = "Doe", Title = "T", Description = "D", Content = "C" };

        await _repo.UpsertAsync(post);
        await _repo.UpsertAsync(post);

        var count = await _context.Posts.CountAsync(p => p.Id == id);
        count.Should().Be(1);
    }
}
