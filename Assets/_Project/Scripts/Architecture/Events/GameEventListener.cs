// GameEventListener.cs
// This is the receiver — a MonoBehaviour you attach to any GameObject that needs to react to an event. It wires a GameEvent asset to a UnityEvent response, which means designers can hook up responses directly in the Inspector with zero code.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameEventListener : MonoBehaviour
{
    [Tooltip("The SO event channel this listener subscribes to.")]
    [SerializeField] private GameEvent _event;

    [Tooltip("The response to invoke when the event is raised.")]
    [SerializeField] private UnityEvent _response;

    private void OnEnable()
    {
        if (_event == null)
        {
            Debug.LogWarning($"[GameEventListener] on '{gameObject.name}' " +
                             $"has no GameEvent assigned.", this);
            return;
        }
        _event.RegisterListener(this);
    }

    private void OnDisable()
    {
        if (_event != null)
            _event.UnregisterListener(this);
    }

    public void OnEventRaised()
    {
        _response?.Invoke();
    }
}
