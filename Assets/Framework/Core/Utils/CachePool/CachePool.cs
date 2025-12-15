using System;
using System.Collections.Generic;

namespace XuchFramework.Core
{
    public interface ICache { }

    public static class CachePool
    {
        private static readonly Dictionary<Type, ICacheCollection> _cacheCollections = new();

        public static int CacheCollectionCount => _cacheCollections.Count;

        public static CacheCollectionInfo[] GetAllCacheCollectionInfos()
        {
            CacheCollectionInfo[] infos = new CacheCollectionInfo[_cacheCollections.Count];
            int index = 0;
            foreach (var cacheCollection in _cacheCollections.Values)
            {
                infos[index++] = new CacheCollectionInfo(
                    cacheCollection.CacheType,
                    cacheCollection.UnusedCount,
                    cacheCollection.UsingCount,
                    cacheCollection.SpawnedCount,
                    cacheCollection.UnspawnedCount,
                    cacheCollection.CreatedCount,
                    cacheCollection.DiscardedCount);
            }

            return infos;
        }

        public static T Acquire<T>() where T : class, ICache, new()
        {
            return GetCacheableCollection<T>().Acquire();
        }

        public static void Release<T>(T cache) where T : class, ICache, new()
        {
            GetCacheableCollection<T>().Release(cache);
        }

        public static void Reserve<T>(int count) where T : class, ICache, new()
        {
            GetCacheableCollection<T>().Reserve(count);
        }

        public static void Discard<T>(int count) where T : class, ICache, new()
        {
            GetCacheableCollection<T>().Discard(count);
        }

        public static void DiscardAll<T>() where T : class, ICache, new()
        {
            GetCacheableCollection<T>().DiscardAll();
        }

        public static void Squeeze<T>(int reserveCount = 0) where T : class, ICache, new()
        {
            GetCacheableCollection<T>().Squeeze(reserveCount);
        }

        private static CacheCollection<T> GetCacheableCollection<T>() where T : class, ICache, new()
        {
            if (!_cacheCollections.TryGetValue(typeof(T), out ICacheCollection collection))
            {
                collection = new CacheCollection<T>();
                _cacheCollections.Add(typeof(T), collection);
            }

            return collection as CacheCollection<T>;
        }

        private interface ICacheCollection
        {
            public Type CacheType { get; }

            public int UnusedCount { get; }

            public int UsingCount { get; }

            public int SpawnedCount { get; }

            public int UnspawnedCount { get; }

            public int CreatedCount { get; }

            public int DiscardedCount { get; }
        }

        private class CacheCollection<T> : ICacheCollection where T : class, ICache, new()
        {
            private readonly Queue<T> _caches = new();

            public Type CacheType { get; private set; } = typeof(T);
            public int UsingCount { get; private set; } = 0;
            public int SpawnedCount { get; private set; } = 0;
            public int UnspawnedCount { get; private set; } = 0;
            public int CreatedCount { get; private set; } = 0;
            public int DiscardedCount { get; private set; } = 0;

            public int UnusedCount
            {
                get
                {
                    lock (_caches)
                    {
                        return _caches.Count;
                    }
                }
            }

            public T Acquire()
            {
                lock (_caches)
                {
                    SpawnedCount++;
                    UsingCount++;
                    if (_caches.Count > 0)
                    {
                        return _caches.Dequeue();
                    }
                }

                CreatedCount++;
                return new T();
            }

            public void Release(T cache)
            {
                if (cache == null)
                {
                    return;
                }

                lock (_caches)
                {
                    _caches.Enqueue(cache);
                    UnspawnedCount++;
                    UsingCount--;
                }
            }

            public void Reserve(int count)
            {
                lock (_caches)
                {
                    for (int i = 0; i < count; i++)
                    {
                        _caches.Enqueue(new T());
                        CreatedCount++;
                    }
                }
            }

            public void Discard(int count)
            {
                lock (_caches)
                {
                    if (count > _caches.Count)
                    {
                        count = _caches.Count;
                    }

                    for (int i = 0; i < count; i++)
                    {
                        _caches.Dequeue();
                        DiscardedCount++;
                    }
                }
            }

            public void DiscardAll()
            {
                lock (_caches)
                {
                    DiscardedCount += _caches.Count;
                    _caches.Clear();
                }
            }

            public void Squeeze(int reserveCount = 0)
            {
                lock (_caches)
                {
                    var discardCount = _caches.Count - reserveCount;
                    Discard(discardCount);
                }
            }
        }
    }
}