using System;
using System.Collections.Generic;
using System.Linq;
using Xuch.Framework.Utils;

namespace Xuch.Framework
{
    public sealed partial class EventManager
    {
        /// <summary>
        /// 事件委托链
        /// </summary>
        private class EventListenerChain : ICache
        {
            private readonly LinkedListPro<Action<IEvent>> _listeners = new();
            private readonly Dictionary<Action<IEvent>, int> _listenerIds = new();

            public int Count => _listeners.Count;

            public static EventListenerChain Create()
            {
                return CachePool.Spawn<EventListenerChain>();
            }

            public void Destroy()
            {
                Clear();
                CachePool.Unspawn(this);
            }

            private void Clear()
            {
                _listeners.Clear();
                _listenerIds.Clear();
            }

            public void AddListener(Action<IEvent> listener, int listenerId)
            {
                _listeners.AddLast(listener);
                _listenerIds[listener] = listenerId;
            }

            public int RemoveListener(Action<IEvent> listener)
            {
                int id = _listenerIds.GetValueOrDefault(listener, -1);
                _listeners.Remove(listener);
                _listenerIds.Remove(listener);
                return id;
            }

            public bool HasListener(Action<IEvent> listener)
            {
                return _listeners.Any(x => x == listener);
            }

            public void Invoke(IEvent evt)
            {
                foreach (var listener in _listeners)
                {
                    listener?.Invoke(evt);
                }
            }
        }
    }
}