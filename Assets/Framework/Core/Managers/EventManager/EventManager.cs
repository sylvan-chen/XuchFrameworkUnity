using System;
using System.Collections.Generic;
using UnityEngine;

namespace XuchFramework.Core
{
    [DisallowMultipleComponent]
    [AddComponentMenu("XuchFramework/Event Manager")]
    public sealed class EventManager : ManagerBase
    {
        private interface IEventBinding { }

        private sealed class EventBinding<T> : IEventBinding
        {
            public Action<T> OnDispatch = delegate { };
        }

        private readonly Dictionary<Type, IEventBinding> _eventBindings = new();

        protected override void OnDispose() { }

        public void AddListener<TEvent>(Action<TEvent> listener) where TEvent : struct
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

        public void RemoveListener<TEvent>(Action<TEvent> listener) where TEvent : struct
        {
            var type = typeof(TEvent);
            if (_eventBindings.TryGetValue(type, out var binding))
            {
                ((EventBinding<TEvent>)binding).OnDispatch -= listener;
            }
        }

        public void Dispatch<TEvent>(TEvent evt) where TEvent : struct
        {
            var type = typeof(TEvent);
            if (_eventBindings.TryGetValue(type, out var binding))
            {
                ((EventBinding<TEvent>)binding).OnDispatch?.Invoke(evt);
            }
        }

        public void Clear()
        {
            _eventBindings.Clear();
        }
    }
}