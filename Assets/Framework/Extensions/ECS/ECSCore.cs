using System;

namespace XuchFramework.Extensions.ECS
{
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
        public void Remove(int entityId);
        public bool Has(int entityId);
        public int Count { get; }
        public int[] Entities { get; }
    }

    public class SparseSet<T> : ISparseSet
    {
        public T[] Components;               // Dense array
        public int[] EntityToComponentIndex; // Sparse array

        public int[] ComponentIndexToEntity;

        public int Count { get; private set; } = 0;

        private int _entityCapacity = 64;
        private int _componentCapacity = 16;

        public int[] Entities => ComponentIndexToEntity;

        public SparseSet()
        {
            Components = new T[_componentCapacity];
            EntityToComponentIndex = new int[_entityCapacity];
            ComponentIndexToEntity = new int[_componentCapacity];

            // -1 means null
            Array.Fill(EntityToComponentIndex, -1);
        }

        public void Add(int entity, T component)
        {
            if (entity >= EntityToComponentIndex.Length)
            {
                int newSize = Math.Max(entity + 1, EntityToComponentIndex.Length * 2);
                Array.Resize(ref EntityToComponentIndex, newSize);
                for (int i = _entityCapacity; i < newSize; i++) EntityToComponentIndex[i] = -1;
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
            if (EntityToComponentIndex[entity] != -1)
            {
                Components[EntityToComponentIndex[entity]] = component;
                return;
            }

            EntityToComponentIndex[entity] = Count;
            ComponentIndexToEntity[Count] = entity;
            Components[Count] = component;
            Count++;
        }

        public void Remove(int entity)
        {
            if (entity >= EntityToComponentIndex.Length || EntityToComponentIndex[entity] == -1) return;

            int indexToRemove = EntityToComponentIndex[entity];
            int lastIndex = Count - 1;

            T lastData = Components[lastIndex];
            int lastEntity = ComponentIndexToEntity[lastIndex];

            Components[indexToRemove] = lastData;
            EntityToComponentIndex[lastEntity] = indexToRemove;
            ComponentIndexToEntity[indexToRemove] = lastEntity;

            Components[lastIndex] = default;
            EntityToComponentIndex[entity] = -1;

            Count--;
        }

        public bool Has(int entity)
        {
            return entity < EntityToComponentIndex.Length && EntityToComponentIndex[entity] != -1;
        }

        public ref T Get(int entity)
        {
            return ref Components[EntityToComponentIndex[entity]];
        }
    }

    public readonly struct Selector<T>
    {
        private readonly SparseSet<T> _pool;

        public Selector(WorldContext world)
        {
            _pool = world.GetPool<T>();
        }

        public delegate void ComponentAction(int entity, ref T component);

        public void ForEach(ComponentAction action)
        {
            int count = _pool.Count;
            int[] entities = _pool.Entities;

            for (int i = 0; i < count; i++)
            {
                int entity = entities[i];
                action(entity, ref _pool.Get(entity));
            }
        }
    }

    public readonly struct Selector<T1, T2>
    {
        private readonly SparseSet<T1> _pool1;
        private readonly SparseSet<T2> _pool2;

        private readonly bool _pool1Smaller;

        public Selector(WorldContext world)
        {
            _pool1 = world.GetPool<T1>();
            _pool2 = world.GetPool<T2>();
            _pool1Smaller = _pool1.Count < _pool2.Count;
        }

        public delegate void ComponentAction(int entity, ref T1 c1, ref T2 c2);

        public void ForEach(ComponentAction action)
        {
            ISparseSet smallerSet = _pool1Smaller ? _pool1 : _pool2;
            ISparseSet otherSet = _pool1Smaller ? _pool2 : _pool1;

            int count = smallerSet.Count;
            int[] entities = smallerSet.Entities;

            for (int i = 0; i < count; i++)
            {
                int entity = entities[i];
                if (otherSet.Has(entity))
                {
                    ref var component1 = ref _pool1.Get(entity);
                    ref var component2 = ref _pool2.Get(entity);
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

        public delegate void ComponentAction(int entity, ref T1 c1, ref T2 c2, ref T3 c3);

        public void ForEach(ComponentAction action)
        {
            int count = smallestPool.Count;
            int[] entities = smallestPool.Entities;

            for (int i = 0; i < count; i++)
            {
                int entity = entities[i];

                if (otherA.Has(entity) && otherB.Has(entity))
                {
                    ref var c1 = ref _pool1.Get(entity);
                    ref var c2 = ref _pool2.Get(entity);
                    ref var c3 = ref _pool3.Get(entity);

                    action(entity, ref c1, ref c2, ref c3);
                }
            }
        }
    }

    public readonly struct Selector<T1, T2, T3, T4>
    {
        private readonly SparseSet<T1> _pool1;
        private readonly SparseSet<T2> _pool2;
        private readonly SparseSet<T3> _pool3;
        private readonly SparseSet<T4> _pool4;

        private readonly ISparseSet smallestPool;
        private readonly ISparseSet otherA;
        private readonly ISparseSet otherB;
        private readonly ISparseSet otherC;

        // public Selector(WorldContext world)
        // {
        //     _pool1 = world.GetPool<T1>();
        //     _pool2 = world.GetPool<T2>();
        //     _pool3 = world.GetPool<T3>();
        //     _pool4 = world.GetPool<T4>();
        // }
    }

    #endregion
}