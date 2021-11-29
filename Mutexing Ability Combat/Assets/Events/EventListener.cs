using System;
using UnityEngine;

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