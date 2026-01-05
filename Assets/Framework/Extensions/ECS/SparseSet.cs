using System;

namespace XuchFramework.Extensions.ECS
{
    public interface ISparseSet
    {
        public void Remove(int entityId);
        public bool Has(int entityId);
        public int ComponentCount { get; }
    }

    public class SparseSet<T> : ISparseSet where T : IComponent
    {
        public T[] Components; // Dense array

        private int[] _entityToComponentIndex; // Sparse array
        private int[] _componentIndexToEntity;

        public int ComponentCount { get; private set; } = 0;

        private int _entityCapacity = 64;
        private int _componentCapacity = 16;

        public SparseSet()
        {
            Components = new T[_componentCapacity];
            _entityToComponentIndex = new int[_entityCapacity];
            _componentIndexToEntity = new int[_componentCapacity];

            // -1 means null
            Array.Fill(_entityToComponentIndex, -1);
        }

        public void Add(int entity, T component)
        {
            if (entity >= _entityToComponentIndex.Length)
            {
                int newSize = Math.Max(entity + 1, _entityToComponentIndex.Length * 2);
                Array.Resize(ref _entityToComponentIndex, newSize);
                for (int i = _entityCapacity; i < newSize; i++) _entityToComponentIndex[i] = -1;
                _entityCapacity = newSize;
            }

            if (ComponentCount >= Components.Length)
            {
                int newSize = Components.Length * 2;
                Array.Resize(ref Components, newSize);
                Array.Resize(ref _componentIndexToEntity, newSize);
                _componentCapacity = newSize;
            }

            // If entity already has components, replace it
            if (_entityToComponentIndex[entity] != -1)
            {
                Components[_entityToComponentIndex[entity]] = component;
                return;
            }

            _entityToComponentIndex[entity] = ComponentCount;
            _componentIndexToEntity[ComponentCount] = entity;
            Components[ComponentCount] = component;
            ComponentCount++;
        }

        public void Remove(int entity)
        {
            if (entity >= _entityToComponentIndex.Length || _entityToComponentIndex[entity] == -1) return;

            int indexToRemove = _entityToComponentIndex[entity];
            int lastIndex = ComponentCount - 1;

            T lastData = Components[lastIndex];
            int lastEntity = _componentIndexToEntity[lastIndex];

            Components[indexToRemove] = lastData;
            _entityToComponentIndex[lastEntity] = indexToRemove;
            _componentIndexToEntity[indexToRemove] = lastEntity;

            Components[lastIndex] = default;
            _entityToComponentIndex[entity] = -1;

            ComponentCount--;
        }

        public bool Has(int entity)
        {
            return entity < _entityToComponentIndex.Length && _entityToComponentIndex[entity] != -1;
        }

        public ref T Get(int entity)
        {
            return ref Components[_entityToComponentIndex[entity]];
        }
    }
}