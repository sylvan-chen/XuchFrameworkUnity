using System;
using System.Collections.Generic;
using UnityEngine;
using XuchFramework.Core.Utils;

namespace XuchFramework.Core
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Xuch/Pool Manager")]
    public sealed class PoolManager : ManagerBase
    {
        public const int DEFAULT_CAPACITY = int.MaxValue;
        public const float DEFAULT_OBJECT_EXPIRED_TIME = float.MaxValue;
        public const float DEFAULT_AUTO_CLEAR_INTERVAL = float.MaxValue;

        private readonly Dictionary<Type, PoolBase> _poolDict = new();

        public int PoolCount => _poolDict.Count;

        protected override void OnDispose()
        {
            foreach (var pool in _poolDict.Values)
            {
                pool.Destroy();
            }

            _poolDict.Clear();
        }

        protected override void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            foreach (var pool in _poolDict.Values)
            {
                pool.Update(deltaTime, unscaledDeltaTime);
            }
        }

        public PoolBase[] GetAllPools()
        {
            var pools = new PoolBase[_poolDict.Count];
            int index = 0;
            foreach (var pool in _poolDict.Values)
            {
                pools[index++] = pool;
            }

            return pools;
        }

        public Pool<T> GetPool<T>() where T : class
        {
            return GetPool(typeof(T)) as Pool<T>;
        }

        public PoolBase GetPool(Type objectType)
        {
            if (objectType == null)
            {
                Log.Error("[PoolManager] GetPool failed, object type cannot be null.");
                return null;
            }

            return _poolDict.GetValueOrDefault(objectType);
        }

        public bool DestroyPool<T>() where T : class
        {
            return DestroyPool(typeof(T));
        }

        public bool DestroyPool(Type objectType)
        {
            if (objectType == null)
            {
                Log.Error("[PoolManager] DestroyPool failed, object type cannot be null.");
                return false;
            }

            if (!_poolDict.TryGetValue(objectType, out var pool))
                return false;

            pool.Destroy();
            _poolDict.Remove(objectType);
            return true;
        }

        public void Squeeze()
        {
            foreach (var pool in _poolDict.Values)
            {
                pool.Squeeze();
            }
        }

        public void DiscardAllUnused()
        {
            foreach (var pool in _poolDict.Values)
            {
                pool.DiscardAllUnused();
            }
        }

        public Pool<T> CreatePool<T>() where T : class
        {
            return CreatePoolInternal<T>(false, DEFAULT_CAPACITY, DEFAULT_OBJECT_EXPIRED_TIME, DEFAULT_AUTO_CLEAR_INTERVAL);
        }

        public Pool<T> CreatePool<T>(int capacity) where T : class
        {
            return CreatePoolInternal<T>(false, capacity, DEFAULT_OBJECT_EXPIRED_TIME, DEFAULT_AUTO_CLEAR_INTERVAL);
        }

        public Pool<T> CreatePool<T>(float objectExpiredTime, float autoClearInterval) where T : class
        {
            return CreatePoolInternal<T>(false, DEFAULT_CAPACITY, objectExpiredTime, autoClearInterval);
        }

        public Pool<T> CreatePool<T>(int capacity, float objectExpiredTime, float autoClearInterval) where T : class
        {
            return CreatePoolInternal<T>(false, capacity, objectExpiredTime, autoClearInterval);
        }

        public PoolBase CreatePool(Type objectType)
        {
            return CreatePoolInternal(objectType, false, DEFAULT_CAPACITY, DEFAULT_OBJECT_EXPIRED_TIME, DEFAULT_AUTO_CLEAR_INTERVAL);
        }

        public PoolBase CreatePool(Type objectType, int capacity)
        {
            return CreatePoolInternal(objectType, false, capacity, DEFAULT_OBJECT_EXPIRED_TIME, DEFAULT_AUTO_CLEAR_INTERVAL);
        }

        public PoolBase CreatePool(Type objectType, float objectExpiredTime, float autoClearInterval)
        {
            return CreatePoolInternal(objectType, false, DEFAULT_CAPACITY, objectExpiredTime, autoClearInterval);
        }

        public PoolBase CreatePool(Type objectType, int capacity, float objectExpiredTime, float autoClearInterval)
        {
            return CreatePoolInternal(objectType, false, capacity, objectExpiredTime, autoClearInterval);
        }

        public Pool<T> CreateMultiReferencePool<T>() where T : class
        {
            return CreatePoolInternal<T>(true, DEFAULT_CAPACITY, DEFAULT_OBJECT_EXPIRED_TIME, DEFAULT_AUTO_CLEAR_INTERVAL);
        }

        public Pool<T> CreateMultiReferencePool<T>(int capacity) where T : class
        {
            return CreatePoolInternal<T>(true, capacity, DEFAULT_OBJECT_EXPIRED_TIME, DEFAULT_AUTO_CLEAR_INTERVAL);
        }

        public Pool<T> CreateMultiReferencePool<T>(float objectExpiredTime, float autoClearInterval) where T : class
        {
            return CreatePoolInternal<T>(true, DEFAULT_CAPACITY, objectExpiredTime, autoClearInterval);
        }

        public Pool<T> CreateMultiReferencePool<T>(int capacity, float objectExpiredTime, float autoClearInterval) where T : class
        {
            return CreatePoolInternal<T>(true, capacity, objectExpiredTime, autoClearInterval);
        }

        public PoolBase CreateMultiReferencePool(Type objectType)
        {
            return CreatePoolInternal(objectType, true, DEFAULT_CAPACITY, DEFAULT_OBJECT_EXPIRED_TIME, DEFAULT_AUTO_CLEAR_INTERVAL);
        }

        public PoolBase CreateMultiReferencePool(Type objectType, int capacity)
        {
            return CreatePoolInternal(objectType, true, capacity, DEFAULT_OBJECT_EXPIRED_TIME, DEFAULT_AUTO_CLEAR_INTERVAL);
        }

        public PoolBase CreateMultiReferencePool(Type objectType, float objectExpiredTime, float autoClearInterval)
        {
            return CreatePoolInternal(objectType, true, DEFAULT_CAPACITY, objectExpiredTime, autoClearInterval);
        }

        public PoolBase CreateMultiReferencePool(Type objectType, int capacity, float objectExpiredTime, float autoClearInterval)
        {
            return CreatePoolInternal(objectType, true, capacity, objectExpiredTime, autoClearInterval);
        }

        private PoolBase CreatePoolInternal(Type objectType, bool allowMultiReference, int capacity, float objectExpiredTime, float autoClearInterval)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType), "CreatePool failed. Type cannot be null.");
            }

            if (!objectType.IsClass || objectType.IsAbstract)
            {
                throw new ArgumentException($"CreatePool failed. Type {objectType.Name} is not a valid class type.");
            }

            if (_poolDict.ContainsKey(objectType))
            {
                throw new InvalidOperationException($"CreatePool failed. Pool of type {objectType.Name} already exists.");
            }

            var poolType = typeof(Pool<>).MakeGenericType(objectType);
            var pool = Activator.CreateInstance(poolType, allowMultiReference, capacity, objectExpiredTime, autoClearInterval) as PoolBase;
            _poolDict.Add(objectType, pool);
            return pool;
        }

        private Pool<T> CreatePoolInternal<T>(bool allowMultiReference, int capacity, float objectExpiredTime, float autoClearInterval)
            where T : class
        {
            if (_poolDict.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException($"Create pool failed, pool of type {typeof(T)} already exists.");
            }

            Pool<T> pool = new(allowMultiReference, capacity, objectExpiredTime, autoClearInterval);
            _poolDict.Add(typeof(T), pool);
            return pool;
        }
    }
}