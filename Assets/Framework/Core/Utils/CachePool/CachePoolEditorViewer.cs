using System;
using UnityEngine;

namespace XuchFramework.Core
{
    public class CachePoolEditorViewer : MonoBehaviour { }

    public readonly struct CacheCollectionInfo
    {
        public readonly Type CacheType;

        public readonly int UnusedCount;

        public readonly int UsingCount;

        public readonly int SpawnedCount;

        public readonly int UnspawnedCount;

        public readonly int CreatedCount;

        public readonly int DiscardedCount;

        public CacheCollectionInfo(
            Type cacheType, int unusedCount, int usingCount, int spawnedCount, int unspawnedCount, int createdCount, int discardedCount)
        {
            CacheType = cacheType;
            UnusedCount = unusedCount;
            UsingCount = usingCount;
            SpawnedCount = spawnedCount;
            UnspawnedCount = unspawnedCount;
            CreatedCount = createdCount;
            DiscardedCount = discardedCount;
        }
    }
}