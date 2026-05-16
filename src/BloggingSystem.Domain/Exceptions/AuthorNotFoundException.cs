using BloggingSystem.Domain.ValueObjects;

namespace BloggingSystem.Domain.Exceptions;

public sealed class AuthorNotFoundException : DomainException
{
    public AuthorNotFoundException(AuthorId authorId)
        : base($"Author with id '{authorId.Value}' was not found.") { }
}
