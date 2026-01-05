using System.Collections.Generic;

namespace XuchFramework.Extensions.ECS
{
    public class WorldContext
    {
        private int entityCounter = 0;

        private List<ISparseSet> _componentPools = new();

        public int CreateEntity()
        {
            return entityCounter++;
        }

        public SparseSet<T> GetSparseSet<T>() where T : IComponent
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
    }
}