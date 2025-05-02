using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beakstorm.Core.Events
{
    public abstract class AbstractEventSO : ScriptableObject
    {
        public const string ASSET_PATH = "Beakstorm/Events/";
    }

    public abstract class AbstractEventSO<T> : AbstractEventSO
    {
        public event Action<T> Action;

        protected HashSet<IEventListener<T>> _listeners = new HashSet<IEventListener<T>>();

        public IReadOnlyCollection<IEventListener<T>> Listeners => _listeners;

        public bool RegisterListener(IEventListener<T> listener) => _listeners.Add(listener);
        public bool UnregisterListener(IEventListener<T> listener) => _listeners.Remove(listener);
        
        public void Raise(T data)
        {
            foreach (var listener in _listeners)
            {
                listener.OnEventRaised(data);
            }
            Action?.Invoke(data);
        }
    }
}