using MtgEngine.Domain.Interfaces;

namespace MtgEngine.Application.GameEngine;

public sealed class EventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public void Publish(IGameEvent gameEvent)
    {
        var type = gameEvent.GetType();
        if (_handlers.TryGetValue(type, out var handlers))
        {
            foreach (var handler in handlers.ToList())
            {
                handler.DynamicInvoke(gameEvent);
            }
        }
    }

    public void Subscribe<T>(Action<T> handler) where T : IGameEvent
    {
        var type = typeof(T);
        if (!_handlers.ContainsKey(type))
            _handlers[type] = [];

        _handlers[type].Add(handler);
    }
}
