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

public sealed class EventBus
{
    #region Listeners
    private static Dictionary<System.Type, SimplePriorityQueue<IEventListener, Events.Priority>> listeners = new Dictionary<System.Type, SimplePriorityQueue<IEventListener, Events.Priority>>();

    public static void AddListener(IEventListener listener, System.Type eventType, Events.Priority priority)
    {
        SimplePriorityQueue<IEventListener, Events.Priority> pq = listeners.GetOrCreate(eventType);
        if(!pq.Contains(listener)) pq.Enqueue(listener, priority);
    }
    public static void RemoveListener(IEventListener listener, System.Type eventType) => listeners[eventType].TryRemove(listener);
    public static void RemoveListenerFromAll(IEventListener listener)
    {
        foreach(SimplePriorityQueue<IEventListener, Events.Priority> i in listeners.Values) i.TryRemove(listener);
    }

    #endregion

    public static T DispatchEvent<T>(T e) where T : Event
    {
        for(System.Type t = e.GetType(); t != typeof(object); t = t.BaseType)
        {
            if(listeners.ContainsKey(t)) foreach(IEventListener i in listeners[t])
            {
                try
                {
                    i.OnRecieveEvent(e);
                }
                catch(System.Exception exc)
                {
                    Debug.LogException(exc);
                }
            }
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
        while(types.MoveNext()) EventBus.AddListener(this, types.Current, Events.Priority.Normal);
    }

    protected virtual void OnDisable()
    {
        EventBus.RemoveListenerFromAll(this);
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