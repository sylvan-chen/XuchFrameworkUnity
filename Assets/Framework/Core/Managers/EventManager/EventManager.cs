using System;
using System.Collections.Generic;
using UnityEngine;

namespace XuchFramework.Core
{
    [DisallowMultipleComponent]
    [AddComponentMenu("XuchFramework/Event Manager")]
    public sealed partial class EventManager : ManagerBase
    {
        private readonly Dictionary<int, EventListenerChain> _listenerChainMap = new();
        private readonly Dictionary<int, (int eventId, Action<IEvent> listener)> _cachedListeners = new();

        private readonly Queue<int> _listenerIdPool = new();

        private int _currentListenerId = -1;

        public int EventCount => _listenerChainMap.Count;

        protected override void OnDispose()
        {
            foreach (var listenerChain in _listenerChainMap.Values)
            {
                listenerChain.Destroy();
            }

            _listenerChainMap.Clear();
            _cachedListeners.Clear();
            _listenerIdPool.Clear();
            _currentListenerId = -1;
        }

        public int AddListener(int eventId, Action<IEvent> listener)
        {
            if (listener == null)
            {
                Log.Error("[EventManager] AddListener failed, listener cannot be null.");
                return -1;
            }

            if (!_listenerChainMap.ContainsKey(eventId))
            {
                _listenerChainMap[eventId] = EventListenerChain.Create();
            }

            int nextListenerId = GetNextListenerId();
            if (nextListenerId < 0)
            {
                Log.Error($"[EventManager] AddListener failed, no listener id available.");
                return -1;
            }

            _listenerChainMap[eventId].AddListener(listener, nextListenerId);
            _cachedListeners[nextListenerId] = (eventId, listener);
            return nextListenerId;
        }

        private int GetNextListenerId()
        {
            if (_listenerIdPool.Count > 0)
                return _listenerIdPool.Dequeue();

            if (_currentListenerId != int.MinValue)
                return ++_currentListenerId;

            Log.Error("[EventManager] GetNextListenerId failed, listenerId overflow and no recyclable ids available.");
            return -1;
        }

        public void RemoveListener(int eventId, Action<IEvent> listener)
        {
            if (!_listenerChainMap.TryGetValue(eventId, out var listenerChain))
                return;

            int removeListenerId = listenerChain.RemoveListener(listener);
            if (removeListenerId >= 0)
            {
                _cachedListeners.Remove(removeListenerId);
                _listenerIdPool.Enqueue(removeListenerId);
            }

            if (listenerChain.Count == 0)
            {
                listenerChain.Destroy();
                _listenerChainMap.Remove(eventId);
            }
        }

        public void RemoveListener(int listenerId)
        {
            if (_cachedListeners.TryGetValue(listenerId, out var listenerInfo))
            {
                RemoveListener(listenerInfo.eventId, listenerInfo.listener);
                listenerInfo.listener = null;
            }
        }

        public void Dispatch(int eventId, params object[] args)
        {
            var gameEvent = GameEvent.Create(args);

            if (_listenerChainMap.TryGetValue(eventId, out var listenerChain))
            {
                listenerChain.Invoke(gameEvent);
            }
        }

        public void ClearAllListeners()
        {
            foreach (var handlerChain in _listenerChainMap.Values)
            {
                handlerChain.Destroy();
            }

            _listenerChainMap.Clear();
            _cachedListeners.Clear();
            _listenerIdPool.Clear();
            _currentListenerId = -1;
        }
    }
}