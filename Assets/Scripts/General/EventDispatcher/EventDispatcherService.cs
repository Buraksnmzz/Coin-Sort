using System;
using System.Collections.Generic;
using UnityEngine;

namespace General.EventDispatcher
{
    public class EventDispatcherService : IEventDispatcherService
    {
        private readonly Dictionary<Type, List<Delegate>> _eventDictionary = new();
        
        public void AddListener<T>(Action<T> listener) where T : ISignal
        {
            var eventType = typeof(T);

            if (!_eventDictionary.TryGetValue(eventType, out var listeners))
            {
                listeners = new List<Delegate>();
                _eventDictionary[eventType] = listeners;
            }

            listeners.Add(listener);
        }

        public void RemoveListener<T>(Action<T> listener) where T : ISignal
        {
            var eventType = typeof(T);

            if (_eventDictionary.TryGetValue(eventType, out var listeners))
            {
                listeners.Remove(listener);

                if (listeners.Count == 0)
                {
                    _eventDictionary.Remove(eventType);
                }
            }
        }

        public void Dispatch(ISignal signal)
        {
            var eventType = signal.GetType();

            if (_eventDictionary.TryGetValue(eventType, out var listeners))
            {
                var listenersCopy = new List<Delegate>(listeners);
                foreach (var listener in listenersCopy)
                {
                    if (listener is Action<ISignal> genericAction)
                    {
                        genericAction.Invoke(signal);
                    }
                    else
                    {
                        listener.DynamicInvoke(signal);
                    }
                }
            }
        }

        public void Dispose()
        {
            _eventDictionary.Clear();
        }
    }
}