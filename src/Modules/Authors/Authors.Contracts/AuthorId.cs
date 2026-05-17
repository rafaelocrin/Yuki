namespace Authors.Contracts;

public sealed record AuthorId
{
    public Guid Value { get; }

    public AuthorId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("AuthorId cannot be empty.", nameof(value));
        Value = value;
    }

    public static implicit operator Guid(AuthorId id) => id.Value;
    public static implicit operator AuthorId(Guid id) => new(id);
}
