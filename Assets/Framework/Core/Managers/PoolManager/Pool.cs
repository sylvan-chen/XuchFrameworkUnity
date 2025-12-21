using System;
using System.Collections.Generic;

namespace XuchFramework.Core
{
    public abstract class PoolBase
    {
        public abstract Type ObjectType { get; }
        public abstract bool AllowMultiReference { get; }
        public abstract int Capacity { get; set; }
        public abstract float ObjectExpiredTime { get; set; }
        public abstract float AutoClearInterval { get; set; }
        public abstract float AutoClearTimer { get; }
        public abstract int Count { get; }

        internal abstract void Update(float deltaTime, float unscaledDeltaTime);
        internal abstract void Destroy();

        public abstract PoolObjectInfo[] GetAllPoolObjectInfos();
        public abstract void Squeeze();
        public abstract void DiscardAllUnused();
        public abstract void DiscardAllExpired();
    }

    public sealed class Pool<T> : PoolBase where T : class
    {
        private readonly bool _allowMultiReference;
        private int _capacity;
        private float _objectExpiredTime;
        private float _autoClearInterval;
        private float _autoClearTimer = 0f;
        private readonly Dictionary<T, PoolObject> _typeToPoolObjectMap = new();
        private readonly List<PoolObject> _cachedDiscardablePoolObjects = new();

        public Pool(bool allowMultiReference, int capacity, float objectExpiredTime, float autoClearInterval)
        {
            _allowMultiReference = allowMultiReference;
            _capacity = capacity;
            _objectExpiredTime = objectExpiredTime;
            _autoClearInterval = autoClearInterval;
        }

        public override Type ObjectType => typeof(T);

        public override bool AllowMultiReference => _allowMultiReference;

        public override float AutoClearInterval
        {
            get => _autoClearInterval;
            set
            {
                if (value < 0f)
                {
                    Log.Error($"[Pool<{typeof(T)}>] AutoClearInterval must be greater than or equal to 0.");
                    value = 0f;
                }

                _autoClearInterval = value;
            }
        }

        public override float AutoClearTimer => _autoClearTimer;

        public override float ObjectExpiredTime
        {
            get => _objectExpiredTime;
            set
            {
                if (value < 0f)
                {
                    Log.Error($"[Pool<{typeof(T)}>] ObjectExpiredTime must be greater than or equal to 0.");
                    value = 0f;
                }

                _objectExpiredTime = value;
            }
        }

        public override int Capacity
        {
            get => _capacity;
            set
            {
                if (value < 0)
                {
                    Log.Error($"[Pool<{typeof(T)}>] Capacity must be greater than or equal to 0.");
                    value = 0;
                }

                _capacity = value;
                Squeeze();
            }
        }

        public override int Count => _typeToPoolObjectMap.Count;

        internal override void Update(float deltaTime, float unscaledDeltaTime)
        {
            _autoClearTimer += unscaledDeltaTime;
            if (_autoClearTimer >= _autoClearInterval)
            {
                DiscardAllExpired();
                _autoClearTimer = 0f;
            }
        }

        internal override void Destroy()
        {
            foreach (PoolObject poolObject in _typeToPoolObjectMap.Values)
            {
                poolObject.Destroy();
            }
        }

        public void Register(T target, Action<T> onSpawn = null, Action<T> onUnspawn = null, Action<T> onDiscard = null)
        {
            if (target == null)
            {
                Log.Error($"[Pool<{typeof(T)}>] Register target failed, target cannot be null.");
                return;
            }

            var poolObject = PoolObject.Create(target);
            poolObject.OnAcquire = onSpawn == null ? null : () => onSpawn.Invoke(target);
            poolObject.OnRelease = onUnspawn == null ? null : () => onUnspawn.Invoke(target);
            poolObject.OnDiscard = onDiscard == null ? null : () => onDiscard.Invoke(target);
            poolObject.ReferenceCount = 1;
            _typeToPoolObjectMap.Add(target, poolObject);
        }

        public override PoolObjectInfo[] GetAllPoolObjectInfos()
        {
            PoolObjectInfo[] poolObjectInfos = new PoolObjectInfo[_typeToPoolObjectMap.Count];
            int index = 0;
            foreach (PoolObject poolObject in _typeToPoolObjectMap.Values)
            {
                poolObjectInfos[index++] = new PoolObjectInfo(
                    poolObject.Locked,
                    poolObject.IsInUse,
                    poolObject.ReferenceCount,
                    poolObject.LastUseUtcTime.ToLocalTime());
            }

            return poolObjectInfos;
        }

        public T Acquire()
        {
            foreach (PoolObject poolObject in _typeToPoolObjectMap.Values)
            {
                if (_allowMultiReference || !poolObject.IsInUse)
                {
                    return poolObject.Acquire().Target as T;
                }
            }

            return null;
        }

        public void Release(T target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target), "Target cannot be null.");
            }

            if (_typeToPoolObjectMap.TryGetValue(target, out var poolObject))
            {
                poolObject.Release();
                if (Count > Capacity && poolObject.ReferenceCount <= 0)
                {
                    Squeeze();
                }
            }
            else
            {
                Log.Error($"[XFramework] [Pool<{typeof(T).Name}>] Unspawn failed. Target not found in pool.");
            }
        }

        public void Lock(T target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target), "Target cannot be null.");
            }

            if (_typeToPoolObjectMap.TryGetValue(target, out var poolObject))
            {
                poolObject.Locked = true;
            }
            else
            {
                Log.Error($"[XFramework] [Pool<{typeof(T).Name}>] Lock failed. Target not found in pool.");
            }
        }

        public void Unlock(T target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target), "Target cannot be null.");
            }

            if (_typeToPoolObjectMap.TryGetValue(target, out PoolObject poolObject))
            {
                poolObject.Locked = false;
            }
            else
            {
                Log.Error($"[XFramework] [Pool<{typeof(T).Name}>] UnLock failed. Target not found in pool.");
            }
        }

        public bool Discard(T target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target), "Target cannot be null.");
            }

            if (_typeToPoolObjectMap.TryGetValue(target, out PoolObject poolObject))
            {
                return Discard(poolObject);
            }

            return false;
        }

        internal bool Discard(PoolObject poolObject)
        {
            if (poolObject == null)
            {
                throw new ArgumentNullException(nameof(poolObject), "PoolObject cannot be null.");
            }

            if (poolObject.IsInUse || poolObject.Locked)
            {
                return false;
            }

            if (poolObject.Target is T target)
                _typeToPoolObjectMap.Remove(target);
            poolObject.Destroy();
            return true;
        }

        public override void DiscardAllUnused()
        {
            UpdateDiscardablePoolObjectsWithoutExpiredCheck();
            foreach (var poolObject in _cachedDiscardablePoolObjects)
            {
                Discard(poolObject);
            }

            _cachedDiscardablePoolObjects.Clear();
        }

        public override void DiscardAllExpired()
        {
            UpdateDiscardablePoolObjects();
            foreach (var poolObject in _cachedDiscardablePoolObjects)
            {
                Discard(poolObject);
            }

            _cachedDiscardablePoolObjects.Clear();
        }

        public override void Squeeze()
        {
            int discardCount = Count - Capacity;
            if (discardCount <= 0)
            {
                return;
            }

            UpdateDiscardablePoolObjectsWithoutExpiredCheck();
            _cachedDiscardablePoolObjects.Sort((a, b) => b.LastUseUtcTime.CompareTo(a.LastUseUtcTime));
            foreach (var poolObject in _cachedDiscardablePoolObjects)
            {
                Discard(poolObject);
            }

            _cachedDiscardablePoolObjects.Clear();
        }

        private void UpdateDiscardablePoolObjects()
        {
            _cachedDiscardablePoolObjects.Clear();
            foreach (var poolObject in _typeToPoolObjectMap.Values)
            {
                if (poolObject.IsInUse || poolObject.Locked)
                    continue;

                double remainingTime = (poolObject.LastUseUtcTime - DateTime.MinValue).TotalSeconds + _objectExpiredTime;
                // If expired time is infinite, skip it
                if (remainingTime.CompareTo(float.MaxValue) >= 0)
                    continue;

                var expiredTime = poolObject.LastUseUtcTime.AddSeconds(_objectExpiredTime);
                if (DateTime.UtcNow > expiredTime)
                {
                    _cachedDiscardablePoolObjects.Add(poolObject);
                }
            }
        }

        private void UpdateDiscardablePoolObjectsWithoutExpiredCheck()
        {
            _cachedDiscardablePoolObjects.Clear();
            foreach (var poolObject in _typeToPoolObjectMap.Values)
            {
                if (poolObject.IsInUse || poolObject.Locked)
                    continue;

                _cachedDiscardablePoolObjects.Add(poolObject);
            }
        }
    }
}