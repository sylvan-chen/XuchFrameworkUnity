using UnityEngine;

namespace Framework.Core
{
    public class CachePoolDebugger : MonoBehaviour
    {
        public float CacheExpireTime;

        private float _lastCacheExpireTime;

        private void Awake()
        {
            CacheExpireTime = CachePool.CacheExpireTime;
        }

        private void Update()
        {
            if (Mathf.Abs(_lastCacheExpireTime - CacheExpireTime) > Mathf.Epsilon)
            {
                _lastCacheExpireTime = CacheExpireTime;
                CachePool.CacheExpireTime = CacheExpireTime;
            }
        }
    }
}