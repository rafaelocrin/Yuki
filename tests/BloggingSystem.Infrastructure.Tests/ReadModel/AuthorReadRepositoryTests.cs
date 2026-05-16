using BloggingSystem.Application.ReadModels;
using BloggingSystem.Infrastructure.Persistence.ReadModel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BloggingSystem.Infrastructure.Tests.ReadModel;

public sealed class AuthorReadRepositoryTests : IDisposable
{
    private readonly BloggingDbContext _context;
    private readonly AuthorReadRepository _repo;

    public AuthorReadRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BloggingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new BloggingDbContext(options);
        _repo = new AuthorReadRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task UpsertAsync_NewAuthor_CanBeRetrieved()
    {
        var author = new AuthorReadModel { Id = Guid.NewGuid(), Name = "Jane", Surname = "Doe" };
        await _repo.UpsertAsync(author);

        var result = await _repo.GetByIdAsync(author.Id);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Jane");
    }

    [Fact]
    public async Task UpsertAsync_ExistingAuthor_UpdatesFields()
    {
        var id = Guid.NewGuid();
        await _repo.UpsertAsync(new AuthorReadModel { Id = id, Name = "Old", Surname = "X" });
        await _repo.UpsertAsync(new AuthorReadModel { Id = id, Name = "New", Surname = "Y" });

        var result = await _repo.GetByIdAsync(id);
        result!.Name.Should().Be("New");
        result.Surname.Should().Be("Y");
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var result = await _repo.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }
}
