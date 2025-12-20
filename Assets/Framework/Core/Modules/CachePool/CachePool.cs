using System;
using System.Collections.Generic;
using UnityEngine;

namespace XuchFramework.Core
{
    public interface ICache { }

    [DisallowMultipleComponent]
    [AddComponentMenu("XuchFramework/Modules/Cache Pool")]
    public class CachePool : ModuleBase
    {
        [SerializeField]
        private float _cacheExpireTime = 60f;

        private readonly Dictionary<Type, ICacheCollection> _cacheCollections = new();

        public int CacheCollectionCount => _cacheCollections.Count;

        public CacheCollectionInfo[] GetAllCacheCollectionInfos()
        {
            CacheCollectionInfo[] infos = new CacheCollectionInfo[_cacheCollections.Count];
            int index = 0;
            foreach (var cacheCollection in _cacheCollections.Values)
            {
                infos[index++] = new CacheCollectionInfo(
                    cacheCollection.CacheType,
                    cacheCollection.UnusedCount,
                    cacheCollection.UsingCount,
                    cacheCollection.AcquiredCount,
                    cacheCollection.ReleasedCount,
                    cacheCollection.CreatedCount,
                    cacheCollection.DiscardedCount,
                    cacheCollection.IdleTime);
            }

            return infos;
        }

        protected override void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            foreach (var collection in _cacheCollections.Values)
            {
                collection.IdleTime += Time.deltaTime;
                if (collection.IdleTime >= _cacheExpireTime)
                {
                    collection.DiscardAll();
                }
            }
        }

        public T Acquire<T>() where T : class, ICache, new()
        {
            return GetCacheableCollection<T>().Acquire();
        }

        public List<T> Acquire<T>(int count) where T : class, ICache, new()
        {
            return GetCacheableCollection<T>().Acquire(count);
        }

        public void Release<T>(T cache) where T : class, ICache, new()
        {
            GetCacheableCollection<T>().Release(cache);
        }

        public void Release<T>(IEnumerable<T> caches) where T : class, ICache, new()
        {
            GetCacheableCollection<T>().Release(caches);
        }

        public void Reserve<T>(int count) where T : class, ICache, new()
        {
            GetCacheableCollection<T>().Reserve(count);
        }

        public void Squeeze<T>(int reserveCount = 0) where T : class, ICache, new()
        {
            GetCacheableCollection<T>().Squeeze(reserveCount);
        }

        public void Discard<T>(int count) where T : class, ICache, new()
        {
            GetCacheableCollection<T>().Discard(count);
        }

        public void DiscardAll<T>() where T : class, ICache, new()
        {
            GetCacheableCollection<T>().DiscardAll();
        }

        private CacheCollection<T> GetCacheableCollection<T>() where T : class, ICache, new()
        {
            if (!_cacheCollections.TryGetValue(typeof(T), out ICacheCollection collection))
            {
                collection = new CacheCollection<T>();
                _cacheCollections.Add(typeof(T), collection);
            }

            return collection as CacheCollection<T>;
        }
    }

    public interface ICacheCollection
    {
        public Type CacheType { get; }
        public int UnusedCount { get; }
        public int UsingCount { get; }
        public int AcquiredCount { get; }
        public int ReleasedCount { get; }
        public int CreatedCount { get; }
        public int DiscardedCount { get; }

        public float IdleTime { get; set; }

        public void Squeeze(int reserve);

        public void Discard(int count);

        public void DiscardAll();
    }

    public class CacheCollection<T> : ICacheCollection where T : class, ICache, new()
    {
        private readonly Queue<T> _caches = new();

        public Type CacheType { get; private set; } = typeof(T);
        public int UsingCount { get; private set; } = 0;
        public int AcquiredCount { get; private set; } = 0;
        public int ReleasedCount { get; private set; } = 0;
        public int CreatedCount { get; private set; } = 0;
        public int DiscardedCount { get; private set; } = 0;

        public float IdleTime { get; set; } = 0f;

        public int UnusedCount => _caches.Count;

        public T Acquire()
        {
            IdleTime = 0;
            AcquiredCount++;
            UsingCount++;
            if (_caches.Count > 0)
            {
                return _caches.Dequeue();
            }

            CreatedCount++;
            return new T();
        }

        public List<T> Acquire(int count)
        {
            var result = new List<T>();

            if (count <= 0)
                return result;

            for (int i = 0; i < count; i++)
            {
                result.Add(Acquire());
            }

            return result;
        }

        public void Release(T cache)
        {
            if (cache == null)
                return;

            IdleTime = 0;
            _caches.Enqueue(cache);
            ReleasedCount++;
            UsingCount--;
        }

        public void Release(IEnumerable<T> caches)
        {
            if (caches == null)
                return;

            foreach (var cache in caches)
            {
                Release(cache);
            }
        }

        public void Reserve(int count)
        {
            IdleTime = 0;
            for (int i = 0; i < count; i++)
            {
                _caches.Enqueue(new T());
                CreatedCount++;
            }
        }

        public void Squeeze(int reserve = 0)
        {
            IdleTime = 0;
            var discardCount = _caches.Count - reserve;
            Discard(discardCount);
        }

        public void Discard(int count)
        {
            IdleTime = 0;

            if (count > _caches.Count)
                count = _caches.Count;

            for (int i = 0; i < count; i++)
            {
                _caches.Dequeue();
                DiscardedCount++;
            }
        }

        public void DiscardAll()
        {
            IdleTime = 0;
            DiscardedCount += _caches.Count;
            _caches.Clear();
        }
    }

    public readonly struct CacheCollectionInfo
    {
        public readonly Type CacheType;

        public readonly int UnusedCount;

        public readonly int UsingCount;

        public readonly int AcquiredCount;

        public readonly int ReleasedCount;

        public readonly int CreatedCount;

        public readonly int DiscardedCount;

        public readonly float IdleTime;

        public CacheCollectionInfo(
            Type cacheType, int unusedCount, int usingCount, int acquiredCount, int releasedCount, int createdCount, int discardedCount,
            float idleTime)
        {
            CacheType = cacheType;
            UnusedCount = unusedCount;
            UsingCount = usingCount;
            AcquiredCount = acquiredCount;
            ReleasedCount = releasedCount;
            CreatedCount = createdCount;
            DiscardedCount = discardedCount;
            IdleTime = idleTime;
        }
    }
}