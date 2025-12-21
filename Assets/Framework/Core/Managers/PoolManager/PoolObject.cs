using System;

namespace XuchFramework.Core
{
    /// <summary>
    /// Pool do not manager actual object directly, but manager PoolObject which contains actual object.
    /// </summary>
    public sealed class PoolObject : ICache
    {
        internal Action OnAcquire;
        internal Action OnRelease;
        internal Action OnDiscard;

        /// <summary>
        /// Actual managed object
        /// </summary>
        public object Target { get; private set; }

        /// <summary>
        /// Locked object will not be released by any automatic discard mechanism even if its reference count is 0, but will be kept in the object pool until manually unlocked
        /// </summary>
        public bool Locked { get; internal set; }

        public DateTime LastUseUtcTime { get; internal set; }

        public int ReferenceCount { get; internal set; }

        public bool IsInUse
        {
            get => ReferenceCount > 0;
        }

        internal static PoolObject Create(object target, bool locked = false)
        {
            PoolObject poolObject = GameModule<CachePool>.Instance.Acquire<PoolObject>();
            poolObject.Target = target;
            poolObject.Locked = locked;
            poolObject.LastUseUtcTime = DateTime.UtcNow;
            poolObject.ReferenceCount = 0;
            return poolObject;
        }

        internal PoolObject Acquire()
        {
            ReferenceCount++;
            LastUseUtcTime = DateTime.UtcNow;
            OnAcquire?.Invoke();
            return this;
        }

        internal void Release()
        {
            OnRelease?.Invoke();
            LastUseUtcTime = DateTime.UtcNow;
            ReferenceCount--;
            if (ReferenceCount < 0)
            {
                throw new InvalidOperationException("SpawnCount can not be negative.");
            }
        }

        internal void Destroy()
        {
            var destroyable = Target as UnityEngine.Object;
            if (destroyable != null)
                UnityEngine.Object.Destroy(destroyable);
            OnDiscard?.Invoke();
            Clear();
            GameModule<CachePool>.Instance.Release(this);
        }

        private void Clear()
        {
            OnAcquire = null;
            OnRelease = null;
            OnDiscard = null;
            Target = null;
            Locked = false;
            LastUseUtcTime = default;
            ReferenceCount = 0;
        }
    }
}