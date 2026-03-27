namespace MtgEngine.Domain.Interfaces;

public interface IEventBus
{
    void Publish(IGameEvent gameEvent);
    void Subscribe<T>(Action<T> handler) where T : IGameEvent;
}
