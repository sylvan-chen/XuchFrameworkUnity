using System;

namespace XuchFramework.Extensions.ECS
{
    #region Entity

    public readonly struct Entity : IEquatable<Entity>
    {
        public readonly int Index;
        public readonly int Version;

        public Entity(int index, int version)
        {
            Index = index;
            Version = version;
        }

        public bool Equals(Entity other) => Index == other.Index && Version == other.Version;
        public override bool Equals(object obj) => obj is Entity other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Index, Version);
        public override string ToString() => $"Entity({Index}:{Version})";

        public static bool operator ==(Entity a, Entity b) => a.Index == b.Index && a.Version == b.Version;
        public static bool operator !=(Entity a, Entity b) => !(a == b);

        public static Entity Null => new Entity(-1, -1);
    }

    #endregion

    #region Component

    internal static class ComponentCounter
    {
        public static int Counter = 0;
    }

    /// <summary> Get a unique id for component T by ComponentType&lt;T&gt;.Id </summary>
    public static class ComponentType<T>
    {
        public static readonly int Id = ComponentCounter.Counter++;
    }

    #endregion

    #region System

    public abstract class SystemBase
    {
        protected WorldContext _world;

        public void Initialize(WorldContext world)
        {
            _world = world;
            OnInitialize();
        }

        public void Dispose()
        {
            OnDispose();
        }

        public abstract void Update(float deltaTime, float unscaledDeltaTime);

        protected virtual void OnInitialize() { }
        protected virtual void OnDispose() { }
    }

    #endregion

    #region Selector

    public interface ISparseSet
    {
        public void Remove(int entityIndex);
        public bool Has(int entityIndex);
        public int Count { get; }
        public Entity[] Entities { get; }
    }

    public class SparseSet<T> : ISparseSet
    {
        public T[] Components;                    // Dense array
        public int[] EntityIndexToComponentIndex; // Sparse array

        public Entity[] ComponentIndexToEntity;

        public int Count { get; private set; } = 0;

        private int _entityCapacity = 64;
        private int _componentCapacity = 16;

        public Entity[] Entities => ComponentIndexToEntity;

        public SparseSet()
        {
            Components = new T[_componentCapacity];
            EntityIndexToComponentIndex = new int[_entityCapacity];
            ComponentIndexToEntity = new Entity[_componentCapacity];

            // -1 means null
            Array.Fill(EntityIndexToComponentIndex, -1);
        }

        public void Add(Entity entity, T component)
        {
            int index = entity.Index;

            if (index >= EntityIndexToComponentIndex.Length)
            {
                int newSize = Math.Max(index + 1, EntityIndexToComponentIndex.Length * 2);
                Array.Resize(ref EntityIndexToComponentIndex, newSize);
                for (int i = _entityCapacity; i < newSize; i++) EntityIndexToComponentIndex[i] = -1;
                _entityCapacity = newSize;
            }

            if (Count >= Components.Length)
            {
                int newSize = Components.Length * 2;
                Array.Resize(ref Components, newSize);
                Array.Resize(ref ComponentIndexToEntity, newSize);
                _componentCapacity = newSize;
            }

            // If entity already has components, replace it
            if (EntityIndexToComponentIndex[index] != -1)
            {
                Components[EntityIndexToComponentIndex[index]] = component;
                ComponentIndexToEntity[EntityIndexToComponentIndex[index]] = entity;
                return;
            }

            EntityIndexToComponentIndex[index] = Count;
            ComponentIndexToEntity[Count] = entity;
            Components[Count] = component;
            Count++;
        }

        public void Remove(int entityIndex)
        {
            if (entityIndex >= EntityIndexToComponentIndex.Length || EntityIndexToComponentIndex[entityIndex] == -1) return;

            int indexToRemove = EntityIndexToComponentIndex[entityIndex];
            int lastIndex = Count - 1;

            T lastData = Components[lastIndex];
            Entity lastEntity = ComponentIndexToEntity[lastIndex];

            Components[indexToRemove] = lastData;
            ComponentIndexToEntity[indexToRemove] = lastEntity;

            EntityIndexToComponentIndex[lastEntity.Index] = indexToRemove;

            Components[lastIndex] = default;
            EntityIndexToComponentIndex[entityIndex] = -1;

            Count--;
        }

        public bool Has(int entityIndex)
        {
            return entityIndex < EntityIndexToComponentIndex.Length && EntityIndexToComponentIndex[entityIndex] != -1;
        }

        public ref T Get(int entityIndex)
        {
            return ref Components[EntityIndexToComponentIndex[entityIndex]];
        }
    }

    public readonly struct Selector<T>
    {
        private readonly SparseSet<T> _pool;

        public Selector(WorldContext world)
        {
            _pool = world.GetPool<T>();
        }

        public delegate void ComponentAction(Entity entity, ref T component);

        public void ForEach(ComponentAction action)
        {
            int count = _pool.Count;
            Entity[] entities = _pool.Entities;

            for (int i = 0; i < count; i++)
            {
                Entity entity = entities[i];
                action(entity, ref _pool.Get(entity.Index));
            }
        }
    }

    public readonly struct Selector<T1, T2>
    {
        private readonly SparseSet<T1> _pool1;
        private readonly SparseSet<T2> _pool2;

        private readonly ISparseSet _smallerSet;
        private readonly ISparseSet _otherSet;

        public Selector(WorldContext world)
        {
            _pool1 = world.GetPool<T1>();
            _pool2 = world.GetPool<T2>();

            if (_pool1.Count < _pool2.Count)
            {
                _smallerSet = _pool1;
                _otherSet = _pool2;
            }
            else
            {
                _smallerSet = _pool2;
                _otherSet = _pool1;
            }
        }

        public delegate void ComponentAction(Entity entity, ref T1 c1, ref T2 c2);

        public void ForEach(ComponentAction action)
        {
            int count = _smallerSet.Count;
            Entity[] entities = _smallerSet.Entities;

            for (int i = 0; i < count; i++)
            {
                Entity entity = entities[i];
                int entityIndex = entity.Index;

                if (_otherSet.Has(entityIndex))
                {
                    ref var component1 = ref _pool1.Get(entityIndex);
                    ref var component2 = ref _pool2.Get(entityIndex);
                    action(entity, ref component1, ref component2);
                }
            }
        }
    }

    public readonly struct Selector<T1, T2, T3>
    {
        private readonly SparseSet<T1> _pool1;
        private readonly SparseSet<T2> _pool2;
        private readonly SparseSet<T3> _pool3;

        private readonly ISparseSet smallestPool;
        private readonly ISparseSet otherA;
        private readonly ISparseSet otherB;

        public Selector(WorldContext world)
        {
            _pool1 = world.GetPool<T1>();
            _pool2 = world.GetPool<T2>();
            _pool3 = world.GetPool<T3>();

            int c1 = _pool1.Count;
            int c2 = _pool2.Count;
            int c3 = _pool3.Count;

            if (c1 <= c2 && c1 <= c3)
            {
                smallestPool = _pool1;
                otherA = _pool2;
                otherB = _pool3;
            }
            else if (c2 <= c1 && c2 <= c3)
            {
                smallestPool = _pool2;
                otherA = _pool1;
                otherB = _pool3;
            }
            else
            {
                smallestPool = _pool3;
                otherA = _pool1;
                otherB = _pool2;
            }
        }

        public delegate void ComponentAction(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3);

        public void ForEach(ComponentAction action)
        {
            int count = smallestPool.Count;
            Entity[] entities = smallestPool.Entities;

            for (int i = 0; i < count; i++)
            {
                Entity entity = entities[i];
                int entityIndex = entity.Index;

                if (otherA.Has(entityIndex) && otherB.Has(entityIndex))
                {
                    ref var c1 = ref _pool1.Get(entityIndex);
                    ref var c2 = ref _pool2.Get(entityIndex);
                    ref var c3 = ref _pool3.Get(entityIndex);

                    action(entity, ref c1, ref c2, ref c3);
                }
            }
        }
    }

    #endregion
}