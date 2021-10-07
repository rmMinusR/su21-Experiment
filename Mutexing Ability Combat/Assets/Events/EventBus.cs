using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class EventBus : MonoBehaviour
{
    #region Singleton

    private static EventBus __Instance = null;
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

    private Dictionary<System.Type, HashSet<IEventListener>> listeners;

    public void AddListener(IEventListener listener, System.Type eventType) => listeners.GetOrCreate(eventType).Add(listener);
    public void RemoveListener(IEventListener listener, System.Type eventType) => listeners[eventType].Remove(listener);
    public void RemoveListenerFromAll(IEventListener listener, System.Type eventType)
    {
        foreach(HashSet<IEventListener> i in listeners.Values) i.Remove(listener);
    }

    #endregion

    public void DispatchEvent(ref Event e)
    {
        for(System.Type t = e.GetType(); t != typeof(object); t = t.BaseType)
        {
            if(listeners.ContainsKey(t)) foreach(IEventListener i in listeners[t]) i.OnRecieveEvent(ref e);
        }
    }
}

public interface IEventListener
{
    public void OnRecieveEvent(ref Event e);
}

public abstract class Event
{
    public bool isCancelled;
}

public class UpdateStatsEvent : Event
{
    public float moveSpeedMultiplier;
}

public class ActionWindupEvent : Event
{

}

public class ActionCastEvent : Event
{

}