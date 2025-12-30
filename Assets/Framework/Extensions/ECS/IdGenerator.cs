using System.Collections.Generic;

namespace XuchFramework.Core.ECS
{
    public static class IdGenerator<T>
    {
        private static int _nextId = 0;

        private static readonly Queue<int> _idPool = new();

        public static int GenerateId()
        {
            if (_idPool.Count > 0)
            {
                return _idPool.Dequeue();
            }

            if (_nextId + 1 == int.MaxValue)
            {
                Log.Error($"[IdGenerator] Reached maximum ID limit for {typeof(T).Name} - {typeof(T).FullName})");
                return -1;
            }

            return _nextId++;
        }

        public static void ReturnId(int id) => _idPool.Enqueue(id);
    }
}