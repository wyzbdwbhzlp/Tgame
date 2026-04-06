using System;
using System.Collections.Generic;

// 事件接口约束，所有事件必须继承此接口
public interface IEvent { }

// 强类型事件总线
public static class EventBus
{
    private static readonly Dictionary<Type, Delegate> _events = new Dictionary<Type, Delegate>();

    public static void Subscribe<T>(Action<T> onEvent) where T : IEvent
    {
        Type eventType = typeof(T);
        if (_events.TryGetValue(eventType, out Delegate existingDelegate))
        {
            _events[eventType] = Delegate.Combine(existingDelegate, onEvent);
        }
        else
        {
            _events[eventType] = onEvent;
        }
    }

    public static void Unsubscribe<T>(Action<T> onEvent) where T : IEvent
    {
        Type eventType = typeof(T);
        if (_events.TryGetValue(eventType, out Delegate existingDelegate))
        {
            Delegate currentDelegate = Delegate.Remove(existingDelegate, onEvent);
            if (currentDelegate == null)
            {
                _events.Remove(eventType);
            }
            else
            {
                _events[eventType] = currentDelegate;
            }
        }
    }

    public static void Publish<T>(T eventArgs) where T : IEvent
    {
        Type eventType = typeof(T);
        if (_events.TryGetValue(eventType, out Delegate eventDelegate))
        {
            Action<T> callback = eventDelegate as Action<T>;
            callback?.Invoke(eventArgs);
        }
    }

    public static void ClearAll()
    {
        _events.Clear();
    }
}