using System.Collections.Generic;

namespace XuchFramework.Extensions.ECS
{
    public class WorldContext
    {
        private int entityCounter = 0;

        private readonly List<ISparseSet> _componentPools = new();

        public int CreateEntity()
        {
            return entityCounter++;
        }

        public SparseSet<T> GetPool<T>()
        {
            int typeId = ComponentType<T>.Id;
            while (_componentPools.Count <= typeId) _componentPools.Add(null);
            if (_componentPools[typeId] == null) _componentPools[typeId] = new SparseSet<T>();
            return _componentPools[typeId] as SparseSet<T>;
        }

        public void AddComponent<T>(int entity, T component) => GetPool<T>().Add(entity, component);

        public void RemoveComponent<T>(int entity) => GetPool<T>().Remove(entity);

        public bool HasComponent<T>(int entity) => GetPool<T>().Has(entity);

        public ref T GetComponent<T>(int entity) => ref GetPool<T>().Get(entity);

        public Selector<T> Query<T>()
        {
            return new Selector<T>(this);
        }
    }
}