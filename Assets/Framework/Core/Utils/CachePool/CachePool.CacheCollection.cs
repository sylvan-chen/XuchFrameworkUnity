using System;
using System.Collections.Generic;

namespace XuchFramework.Core
{
    public static partial class CachePool
    {
        private class CacheCollection
        {
            private readonly Queue<ICache> _cache = new();

            public CacheCollection(Type cacheType)
            {
                CacheType = cacheType;
                UsingCount = 0;
                SpawnedCount = 0;
                UnspawnedCount = 0;
                CreatedCount = 0;
                DiscardedCount = 0;
            }

            public Type CacheType { get; private set; }

            public int UnusedCount
            {
                get
                {
                    lock (_cache)
                    {
                        return _cache.Count;
                    }
                }
            }

            public int UsingCount { get; private set; }

            public int SpawnedCount { get; private set; }

            public int UnspawnedCount { get; private set; }

            public int CreatedCount { get; private set; }

            public int DiscardedCount { get; private set; }

            public ICache Acquire()
            {
                SpawnedCount++;
                UsingCount++;
                lock (_cache)
                {
                    if (_cache.Count > 0)
                    {
                        return _cache.Dequeue();
                    }
                }

                CreatedCount++;
                return Activator.CreateInstance(CacheType) as ICache;
            }

            public void Release(ICache cache)
            {
                if (cache == null)
                {
                    return;
                }

                lock (_cache)
                {
                    if (!_cache.Contains(cache))
                    {
                        _cache.Enqueue(cache);
                        UnspawnedCount++;
                        UsingCount--;
                    }
                }
            }

            public void Reserve(int count)
            {
                lock (_cache)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ICache newInstance = Activator.CreateInstance(CacheType) as ICache;
                        if (newInstance == null)
                        {
                            Log.Error($"[XFramework] [ReferencePool] Reserve reference failed. Reference type {CacheType.Name} is invalid.");
                            continue;
                        }

                        CreatedCount++;
                        _cache.Enqueue(newInstance);
                    }
                }
            }

            public void Discard(int count)
            {
                lock (_cache)
                {
                    if (count > _cache.Count)
                    {
                        count = _cache.Count;
                    }

                    for (int i = 0; i < count; i++)
                    {
                        _cache.Dequeue();
                        DiscardedCount++;
                    }
                }
            }

            public void DiscardAll()
            {
                lock (_cache)
                {
                    DiscardedCount += _cache.Count;
                    _cache.Clear();
                }
            }
        }
    }
}