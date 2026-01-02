using System;
using System.Collections.Generic;
using XuchFramework.Core.Utils;

namespace XuchFramework.Core.ECS
{
    public class GameContext : Singleton<GameContext>
    {
        private readonly Dictionary<Type, IEntityDataPool> _pools = new();

        public EntityDataPool<T> GetPool<T>() where T : struct
        {
            Type dataType = typeof(T);
            if (!_pools.TryGetValue(dataType, out IEntityDataPool pool))
            {
                var newPool = new EntityDataPool<T>();
                _pools.Add(dataType, newPool);
                return newPool;
            }
            return pool as EntityDataPool<T>;
        }
    }

    public interface IEntityDataPool
    {
        bool Has(int entityId);
        void Remove(int entityId);
        void Clear();
    }

    public class EntityDataPool<T> : IEntityDataPool where T : struct
    {
        public T[] Datas;

        public int Count { get; private set; } = 0;

        private readonly Dictionary<int, int> _entityIdToIndex;
        private readonly Dictionary<int, int> _indexToEntityId;

        public EntityDataPool(int capacity = 1000)
        {
            Datas = new T[capacity];
            _entityIdToIndex = new Dictionary<int, int>(capacity);
            _indexToEntityId = new Dictionary<int, int>(capacity);
        }

        public void Add(int entityId, T data)
        {
            if (_entityIdToIndex.ContainsKey(entityId)) return;

            if (Count >= Datas.Length)
            {
                Array.Resize(ref Datas, Datas.Length * 2);
            }

            Datas[Count] = data;
            _entityIdToIndex[entityId] = Count;
            _indexToEntityId[Count] = entityId;
            Count++;
        }

        public ref T Get(int entityId) => ref Datas[_entityIdToIndex[entityId]];

        public bool Has(int entityId) => _entityIdToIndex.ContainsKey(entityId);

        public void Remove(int entityId)
        {
            if (!_entityIdToIndex.TryGetValue(entityId, out int indexToRemove)) return;

            int lastIndex = Count - 1;
            if (indexToRemove != lastIndex)
            {
                // Swap end
                Datas[indexToRemove] = Datas[lastIndex];
                // Update the entityId - index mapping
                int lastEntityId = _indexToEntityId[lastIndex];
                _entityIdToIndex[lastEntityId] = indexToRemove;
                _indexToEntityId[indexToRemove] = lastEntityId;
            }

            // It's necessary to set default if the struct contains fields with reference type
            Datas[lastIndex] = default;

            _entityIdToIndex.Remove(entityId);
            _indexToEntityId.Remove(lastIndex);
            Count--;
        }

        public void Clear()
        {
            _entityIdToIndex.Clear();
            _indexToEntityId.Clear();
            Count = 0;
        }
    }
}