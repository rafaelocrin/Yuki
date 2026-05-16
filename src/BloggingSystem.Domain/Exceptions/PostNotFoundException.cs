using BloggingSystem.Domain.ValueObjects;

namespace BloggingSystem.Domain.Exceptions;

public sealed class PostNotFoundException : DomainException
{
    public PostNotFoundException(PostId postId)
        : base($"Post with id '{postId.Value}' was not found.") { }
}
