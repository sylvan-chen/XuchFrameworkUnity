using System.Collections.Generic;
using XuchFramework.Core;

namespace XuchFramework.Extensions.ECS
{
    public class WorldContext
    {
        private int _entityIndexCounter = 0;
        private readonly Queue<int> _entityIndexPool = new();
        private readonly List<int> _entityVersions = new List<int>();

        // _allPools stores pools with less capacity, so it's efficient for destroying entity
        // _lookUpPools stores pools using the biggest index as capacity, so it's efficient for query (just _lookUpPools[typeId], O(1))
        // Like this:
        // - _allPools: [ set1(id_1), set2(id_2), set3(id_100) ]
        // - _lookUpPools: [ set1(id_1), set2(id_2), null, ... , null, set3(id_100) ]
        private readonly List<ISparseSet> _allPools = new List<ISparseSet>();
        private readonly List<ISparseSet> _lookUpPools = new List<ISparseSet>();

        public Entity CreateEntity()
        {
            int index;
            if (_entityIndexPool.Count > 0)
            {
                index = _entityIndexPool.Dequeue();
            }
            else
            {
                index = _entityIndexCounter++;
                while (_entityVersions.Count <= index) _entityVersions.Add(0);
                _entityVersions[index] = 1;
            }

            return new Entity(index, _entityVersions[index]);
        }

        public void DestroyEntity(Entity entity)
        {
            if (!IsAlive(entity))
            {
                Log.Warning($"[WorldContext] Destroying an entity({entity}) that is not alive.");
                return;
            }

            _entityVersions[entity.Index]++;
            _entityIndexPool.Enqueue(entity.Index);

            foreach (var pool in _allPools)
            {
                pool.Remove(entity.Index);
            }
        }

        public bool IsAlive(Entity entity)
        {
            if (entity.Index < 0 || entity.Index >= _entityVersions.Count) return false;
            return entity.Version == _entityVersions[entity.Index];
        }

        public SparseSet<T> GetPool<T>()
        {
            int typeId = ComponentType<T>.Id;
            while (_lookUpPools.Count <= typeId) _lookUpPools.Add(null);

            if (_lookUpPools[typeId] == null)
            {
                var newPool = new SparseSet<T>();
                _lookUpPools[typeId] = newPool;
                _allPools.Add(newPool);
            }
            return _lookUpPools[typeId] as SparseSet<T>;
        }

        public void AddComponent<T>(Entity entity, T component)
        {
            if (!IsAlive(entity)) return;
            GetPool<T>().Add(entity, component);
        }

        public void RemoveComponent<T>(Entity entity)
        {
            if (!IsAlive(entity)) return;
            GetPool<T>().Remove(entity.Index);
        }

        public bool HasComponent<T>(Entity entity)
        {
            if (!IsAlive(entity)) return false;
            return GetPool<T>().Has(entity.Index);
        }

        public ref T GetComponent<T>(Entity entity)
        {
            // Considering performance, not check IsAlive here
            return ref GetPool<T>().Get(entity.Index);
        }

        public Selector<T> Query<T>()
        {
            return new Selector<T>(this);
        }

        public Selector<T1, T2> Query<T1, T2>()
        {
            return new Selector<T1, T2>(this);
        }

        public Selector<T1, T2, T3> Query<T1, T2, T3>()
        {
            return new Selector<T1, T2, T3>(this);
        }
    }
}