namespace Posts.Domain.ValueObjects;

public sealed record PostId
{
    public Guid Value { get; }

    public PostId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("PostId cannot be empty.", nameof(value));
        Value = value;
    }

    public static implicit operator Guid(PostId id) => id.Value;
    public static implicit operator PostId(Guid id) => new(id);
    public override string ToString() => Value.ToString();
}
