namespace MtgClient.Models;

/// <summary>
/// Local user session data.
/// </summary>
public sealed class UserSession
{
    public string PlayerName { get; set; } = string.Empty;
    public string? Token { get; set; }
    public bool IsConnected { get; set; }
}
