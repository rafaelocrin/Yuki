using Shared.Domain.Exceptions;

namespace Posts.Domain.Exceptions;

public sealed class KnownAuthorNotFoundException : DomainException
{
    public KnownAuthorNotFoundException(Guid authorId)
        : base($"Author '{authorId}' is not known to the Posts module.") { }
}
