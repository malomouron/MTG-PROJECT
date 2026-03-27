namespace MtgEngine.Domain.Interfaces;

public interface IGameEvent
{
    string EventType { get; }
    string GameId { get; }
}
