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

        public SparseSet<T> GetPool<T>() where T : IComponent
        {
            int typeId = ComponentType<T>.Id;

            while (_componentPools.Count <= typeId)
            {
                _componentPools.Add(null);
            }

            if (_componentPools[typeId] == null)
            {
                _componentPools[typeId] = new SparseSet<T>();
            }

            return _componentPools[typeId] as SparseSet<T>;
        }

        public void AddComponent<T>(int entityId, T component) where T : IComponent
        {
            GetPool<T>().Add(entityId, component);
        }

        public void RemoveComponent<T>(int entity) where T : IComponent
        {
            GetPool<T>().Remove(entity);
        }

        public T GetComponent<T>(int entity) where T : IComponent
        {
            return GetPool<T>().Get(entity);
        }

        public bool HasComponent<T>(int entity) where T : IComponent
        {
            return GetPool<T>().Has(entity);
        }
    }
}