using System;
using System.Collections.Generic;

namespace XuchFramework.Core
{
    public interface ICache { }

    public static partial class CachePool
    {
        private static readonly Dictionary<Type, CacheCollection> _cacheCollections = new();

        public static int CacheCollectionCount => _cacheCollections.Count;

        public static CacheCollectionInfo[] GetAllCacheCollectionInfos()
        {
            CacheCollectionInfo[] infos = new CacheCollectionInfo[_cacheCollections.Count];
            int index = 0;
            foreach (CacheCollection cacheCollection in _cacheCollections.Values)
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

        public static ICache Spawn(Type type)
        {
            return GetCacheableCollection(type).Acquire();
        }

        public static T Spawn<T>() where T : class, ICache
        {
            return GetCacheableCollection(typeof(T)).Acquire() as T;
        }

        public static void Unspawn(ICache cache)
        {
            GetCacheableCollection(cache.GetType()).Release(cache);
        }

        public static void Reserve(Type type, int count)
        {
            GetCacheableCollection(type).Reserve(count);
        }

        public static void Reserve<T>(int count) where T : class, ICache
        {
            GetCacheableCollection(typeof(T)).Reserve(count);
        }

        public static void Discard(Type type, int count)
        {
            GetCacheableCollection(type).Discard(count);
        }

        public static void Discard<T>(int count) where T : class, ICache
        {
            GetCacheableCollection(typeof(T)).Discard(count);
        }

        public static void DiscardAll(Type type)
        {
            GetCacheableCollection(type).DiscardAll();
        }

        public static void DiscardAll<T>() where T : class, ICache
        {
            GetCacheableCollection(typeof(T)).DiscardAll();
        }

        private static CacheCollection GetCacheableCollection(Type type)
        {
            if (type == null)
            {
                Log.Error("[CachePool] GetCacheableCollection failed, type cannot be null.");
                return null;
            }

            if (!_cacheCollections.TryGetValue(type, out CacheCollection collection))
            {
                collection = new CacheCollection(type);
                _cacheCollections.Add(type, collection);
            }

            return collection;
        }
    }
}