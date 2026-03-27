using MtgEngine.Domain.Interfaces;

namespace MtgEngine.Application.GameEngine;

public sealed class EventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public void Publish(IGameEvent gameEvent)
    {
        Type type = gameEvent.GetType();
        if (_handlers.TryGetValue(type, out List<Delegate>? handlers))
        {
            foreach (Delegate? handler in handlers.ToList())
            {
                _ = handler.DynamicInvoke(gameEvent);
            }
        }
    }

    public void Subscribe<T>(Action<T> handler) where T : IGameEvent
    {
        Type type = typeof(T);
        if (!_handlers.ContainsKey(type))
            _handlers[type] = [];

        _handlers[type].Add(handler);
    }
}
