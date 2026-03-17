using System;
using System.Collections.Generic;

namespace Framework.Core
{
    public static class EventBus
    {
        private interface IEventBinding { }

        private sealed class EventBinding<T> : IEventBinding
        {
            public Action<T> OnDispatch = delegate { };
        }

        private static readonly Dictionary<Type, IEventBinding> _eventBindings = new();

        public static void AddListener<TEvent>(Action<TEvent> listener) where TEvent : struct
        {
            var type = typeof(TEvent);
            if (!_eventBindings.TryGetValue(type, out var binding))
            {
                binding = new EventBinding<TEvent>();
                _eventBindings.Add(type, binding);
            }
            // '+=' operator will create a new delegate instance, so there is a bit of GC here. But it's acceptable for initialization
            ((EventBinding<TEvent>)binding).OnDispatch += listener;
        }

        public static void RemoveListener<TEvent>(Action<TEvent> listener) where TEvent : struct
        {
            var type = typeof(TEvent);
            if (_eventBindings.TryGetValue(type, out var binding))
            {
                ((EventBinding<TEvent>)binding).OnDispatch -= listener;
            }
        }

        public static void Dispatch<TEvent>(TEvent evt) where TEvent : struct
        {
            var type = typeof(TEvent);
            if (_eventBindings.TryGetValue(type, out var binding))
            {
                ((EventBinding<TEvent>)binding).OnDispatch?.Invoke(evt);
            }
        }

        public static void Clear()
        {
            _eventBindings.Clear();
        }
    }
}