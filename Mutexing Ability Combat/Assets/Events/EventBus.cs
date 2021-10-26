using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;
using System.Linq;

namespace Events
{
    public enum Priority
    {
        Highest = 0,
        High = 1,
        Normal = 2,
        Low = 3,
        Lowest = 4,

        Final = 1000
    }
}

public sealed class EventBus
{
    #region Listeners
    private static Dictionary<System.Type, SimplePriorityQueue<IEventListener, Events.Priority>> buses = new Dictionary<System.Type, SimplePriorityQueue<IEventListener, Events.Priority>>();

    public static void AddListener(IEventListener listener, System.Type eventType, Events.Priority priority)
    {
        SimplePriorityQueue<IEventListener, Events.Priority> pq = buses.GetOrCreate(eventType);
        if(!pq.Contains(listener)) pq.Enqueue(listener, priority);
    }
    public static void RemoveListener(IEventListener listener, System.Type eventType) => buses[eventType].TryRemove(listener);
    public static void RemoveListenerFromAll(IEventListener listener)
    {
        foreach(SimplePriorityQueue<IEventListener, Events.Priority> bus in buses.Values) bus.TryRemove(listener);
    }

    #endregion

    public static T DispatchEvent<T>(T @event) where T : Event
    {
        @event.OnPreDispatch();
        int processCount = 0;

        foreach(KeyValuePair<System.Type, SimplePriorityQueue<IEventListener, Events.Priority>> pair in buses)
        {
            if(pair.Key.IsAssignableFrom(typeof(T)))
            {
                foreach (IEventListener i in pair.Value)
                {
                    try
                    {
                        i.OnRecieveEvent(@event);
                        processCount++;
                    }
                    catch (System.Exception exc)
                    {
                        Debug.LogException(exc);
                    }
                }
            }
        }

        @event.OnPostDispatch();
        return @event;
    }
}


public interface IEventListener
{
    public void OnRecieveEvent(Event e);
}

public abstract class ScopedEventListener : MonoBehaviour, IEventListener
{
    protected virtual void OnEnable() => DoEventRegistration();

    protected virtual void OnDisable() => EventBus.RemoveListenerFromAll(this);

    protected abstract void DoEventRegistration();

    public abstract void OnRecieveEvent(Event e);
}


public abstract class Event
{
    public bool isCancelled;

    protected Event()
    {
        isCancelled = false;
    }

    public virtual void OnPreDispatch() { }
    public virtual void OnPostDispatch() { }
}