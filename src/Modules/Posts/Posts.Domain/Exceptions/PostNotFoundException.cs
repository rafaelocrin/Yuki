using Posts.Domain.ValueObjects;
using Shared.Domain.Exceptions;

namespace Posts.Domain.Exceptions;

public sealed class PostNotFoundException : DomainException
{
    public PostNotFoundException(PostId postId)
        : base($"Post with id '{postId.Value}' was not found.") { }
}
