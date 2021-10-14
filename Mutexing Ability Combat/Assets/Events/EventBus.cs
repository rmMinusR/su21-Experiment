using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;

namespace Events
{
    public enum Priority
    {
        Highest = 0,
        High = 1,
        Normal = 2,
        Low = 3,
        Lowest = 4
    }
}

public sealed class EventBus : MonoBehaviour
{
    #region Singleton

    private static EventBus __Instance = null;
    public static bool HasInstance => __Instance != null;
    public static EventBus Instance
    {
        get
        {
            //Have we already captured Instance? And if so, is it still good? (Scene changes etc)
            if (__Instance != null) return __Instance;
            //Search for existing Instance
            if ((__Instance = FindObjectOfType<EventBus>()) != null) return __Instance;
            //None exists, create new Instance
            return __Instance = new GameObject().AddComponent<EventBus>();
        }
    }

    #endregion

    #region Listeners

    private Dictionary<System.Type, SimplePriorityQueue<IEventListener, Events.Priority>> _listeners;
    private Dictionary<System.Type, SimplePriorityQueue<IEventListener, Events.Priority>> listeners => _listeners != null ? _listeners : (_listeners = new Dictionary<System.Type, SimplePriorityQueue<IEventListener, Events.Priority>>());

    public void AddListener(IEventListener listener, System.Type eventType, Events.Priority priority)
    {
        SimplePriorityQueue<IEventListener, Events.Priority> pq = listeners.GetOrCreate(eventType);
        if(!pq.Contains(listener)) pq.Enqueue(listener, priority);
    }
    public void RemoveListener(IEventListener listener, System.Type eventType) => listeners[eventType].TryRemove(listener);
    public void RemoveListenerFromAll(IEventListener listener)
    {
        foreach(SimplePriorityQueue<IEventListener, Events.Priority> i in listeners.Values) i.TryRemove(listener);
    }

    #endregion

    public T DispatchEvent<T>(T e) where T : Event
    {
        for(System.Type t = e.GetType(); t != typeof(object); t = t.BaseType)
        {
            if(listeners.ContainsKey(t)) foreach(IEventListener i in listeners[t]) i.OnRecieveEvent(e);
        }

        return e;
    }
}


public interface IEventListener
{
    public void OnRecieveEvent(Event e);
}

public abstract class ScopedEventListener : MonoBehaviour, IEventListener
{
    protected virtual void OnEnable()
    {
        IEnumerator<System.Type> types = GetListenedEventTypes();
        while(types.MoveNext()) EventBus.Instance.AddListener(this, types.Current, Events.Priority.Normal);
    }

    protected virtual void OnDisable()
    {
        if(EventBus.HasInstance) EventBus.Instance.RemoveListenerFromAll(this);
    }

    protected abstract IEnumerator<System.Type> GetListenedEventTypes();

    public abstract void OnRecieveEvent(Event e);
}


public abstract class Event
{
    public bool isCancelled;

    protected Event()
    {
        isCancelled = false;
    }
}