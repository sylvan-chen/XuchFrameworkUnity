using System;
using System.Collections.Generic;
using UnityEngine;
using Xuch.Framework.Utils;

namespace Xuch.Framework
{
    [DisallowMultipleComponent]
    [AddComponentMenu("DigiEden/Event Manager")]
    public sealed partial class EventManager : ManagerBase
    {
        // 事件字典
        private readonly Dictionary<int,Action<IEvent>> _listenerMap = new();
        private readonly Dictionary<int, EventListenerChain> _listenerChainMap = new();
        // 已注册的监听器缓存 listenerId -> (eventId, listener)
        private readonly Dictionary<int, (int eventId, Action<IEvent> listener)> _cachedListeners = new();
        // listenerId 池
        private readonly Queue<int> _listenerIdPool = new();

        private int _currentListenerId = -1;

        public int EventCount => _listenerChainMap.Count;

        protected override void OnDispose()
        {
            base.OnDispose();
            foreach (var listenerChain in _listenerChainMap.Values)
            {
                listenerChain.Destroy();
            }

            _listenerChainMap.Clear();
            _cachedListeners.Clear();
            _listenerIdPool.Clear();
            _listenerMap.Clear();
            _currentListenerId = -1;
        }

        public int AddListenerOnlyOne(int eventId, Action<IEvent> listener)
        {
            if (listener == null)
            {
                Log.Error("[EventManager] AddListener failed, listener cannot be null.");
                return -1;
            }

            if (_listenerMap.ContainsKey(eventId))
            {
                return -1;
            }
            _listenerMap[eventId] = listener;
            return 0;
        }

        /// <summary>
        /// 为事件添加监听器
        /// </summary>
        /// <param name="eventId">事件 Id</param>
        /// <param name="listener">监听器</param>
        /// <returns>监听器 Id</returns>
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

        /// <summary>
        /// 移除监听器（通过监听器实例）
        /// </summary>
        /// <param name="eventId">事件 Id</param>
        /// <param name="listener">监听器</param>
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

        public void RemoveListenerOnlyOne(int eventId)
        {
            _listenerMap.Remove(eventId);
        }

        /// <summary>
        /// 移除监听器（通过监听器 Id）
        /// </summary>
        /// <param name="listenerId">监听器 Id</param>
        public void RemoveListener(int listenerId)
        {
            if (_cachedListeners.TryGetValue(listenerId, out var listenerInfo))
            {
                RemoveListener(listenerInfo.eventId, listenerInfo.listener);
                listenerInfo.listener = null;
            }
        }

        /// <summary>
        /// 分发事件
        /// </summary>
        /// <param name="eventId">事件 Id</param>
        /// <param name="args">可变参数</param>
        public void Dispatch(int eventId, params object[] args)
        {
            var gameEvent = GameEvent.Create(args);

            if (_listenerChainMap.TryGetValue(eventId, out var listenerChain))
            {
                listenerChain.Invoke(gameEvent);
            }
            if (_listenerMap.TryGetValue(eventId, out var listener))
            {
                listener?.Invoke(gameEvent);
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
            _listenerMap.Clear();
            _currentListenerId = -1;
        }
    }
}