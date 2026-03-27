namespace MtgClient.Models;

/// <summary>
/// Chat message model.
/// </summary>
public sealed class ChatMessage
{
    public required string Sender { get; init; }
    public required string Text { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}
