namespace Authors.Infrastructure.Idempotency;

internal sealed class ProcessedCommand
{
    public string IdempotencyKey { get; set; } = "";
    public string SerializedResult { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
