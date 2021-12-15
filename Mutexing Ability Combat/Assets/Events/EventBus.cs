using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;
using System.Linq;

public sealed class EventBus : MonoBehaviour
{
    #region Singleton

    private static EventBus __instance = null;
    public static EventBus Instance => __instance != null ? __instance : (__instance = new GameObject("EventBus").AddComponent<EventBus>()); //NOTE: Causes bad cleanup message

    #endregion

    #region Listeners
    private Dictionary<System.Type, SimplePriorityQueue<IEventListener, Events.Priority>> buses = new Dictionary<System.Type, SimplePriorityQueue<IEventListener, Events.Priority>>();

    public static void AddListener(IEventListener listener, System.Type eventType, Events.Priority priority)
    {
        SimplePriorityQueue<IEventListener, Events.Priority> pq = Instance.buses.GetOrCreate(eventType);
        if(!pq.Contains(listener)) pq.Enqueue(listener, priority);
    }
    public static void RemoveListener(IEventListener listener, System.Type eventType) => Instance.buses[eventType].TryRemove(listener);
    public static void RemoveListenerFromAll(IEventListener listener)
    {
        foreach(SimplePriorityQueue<IEventListener, Events.Priority> bus in Instance.buses.Values) bus.TryRemove(listener);
    }

    public static T DispatchImmediately<T>(T @event) where T : Event
    {
        @event.OnPreDispatch();
        int processCount = 0;

        foreach (KeyValuePair<System.Type, SimplePriorityQueue<IEventListener, Events.Priority>> pair in Instance.buses)
        {
            if (pair.Key.IsAssignableFrom(@event.GetType()))
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

    #endregion

    private void Update()
    {
        DispatchBufferedEvents();
    }

    #region Buffering system

    private Queue<Event> eventBuffer = new Queue<Event>();

    private void DispatchBufferedEvents()
    {
        while(eventBuffer.Count > 0)
        {
            DispatchImmediately(eventBuffer.Dequeue());
        }
    }

    public static void Dispatch(Event @event)
    {
        if (!Instance.eventBuffer.Contains(@event)) Instance.eventBuffer.Enqueue(@event);
    }

    #endregion
}

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
